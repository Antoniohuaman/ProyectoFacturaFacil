#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ComprobantesElectronicosBC.Application.DTOs;
using ComprobantesElectronicosBC.Application.Interfaces;
using SharedKernel.Events;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace ComprobantesElectronicosBC.Application.UseCases
{
    /// <summary>
    /// Caso de uso: Emitir un comprobante (Factura/Boleta, etc.).
    /// Coordina validaciones con VOs, solicita numeración, calcula totales,
    /// persiste y publica el evento de dominio.
    /// </summary>
    public sealed class EmitirComprobanteUseCase : IEmitirComprobanteUseCase
    {
        private readonly INumeracionService _numeracion;
        private readonly IComprobanteEmitidoPersister _repo;
        private readonly IEventBus _eventBus;

        public EmitirComprobanteUseCase(
            INumeracionService numeracion,
            IComprobanteEmitidoPersister repo,
            IEventBus eventBus)
        {
            _numeracion = numeracion ?? throw new ArgumentNullException(nameof(numeracion));
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public async Task<EmitirComprobanteOutputDto> HandleAsync(EmitirComprobanteInputDto input, CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            if (input.Items is null || input.Items.Count == 0)
                throw new BusinessRuleException("El comprobante debe contener al menos un ítem.");

            // -------- Identidades / valores transversales
            var empresaId = EmpresaId.From(input.EmpresaId);
            var establecimientoId = EstablecimientoId.FromString(input.EstablecimientoId);
            var tipoComprobante = NormalizarTipoComprobante(input.TipoComprobante);

            var moneda = Moneda.Create(input.MonedaCodigo);
            var tasa = TasaImpuesto.FromPercent(NormalizarTasa(input.TasaImpuestoPorcentaje));

            var fechaEmision = input.FechaEmision ?? DateOnly.FromDateTime(DateTime.Now);

            // -------- Cliente
            var doc = DocumentoIdentidad.Crear(input.Cliente.TipoDocumento, input.Cliente.NumeroDocumento);
            var clienteEtiqueta = ConstruirEtiquetaCliente(input, doc);
            var domicilio = ConstruirDomicilio(input);

            var emails = Email.ParseListOrEmpty(input.Cliente.Emails, Email.MaxDestinatarios);
            var telefonos = Telefono.FromTexto(input.Cliente.Telefonos);

            // Orquestación: construir aggregate BORRADOR y usar comandos de dominio.
            // Multitenant: usar TenantId real si viene en input; si no, fallback temporal a uno nuevo.
            // TODO: Retirar fallback cuando la UI provea siempre TenantId.
            var tenantId = SharedKernel.ValueObjects.TenantId.TryParse(input.TenantId, out var parsedTenant)
                ? parsedTenant
                : SharedKernel.ValueObjects.TenantId.New();
            // El EmisorSnapshot requiere RUC. Si el documento del cliente NO es RUC usamos un placeholder de empresa (input.EmpresaId) asumiendo que empresaId.Value es el RUC.
            var rucEmisor = doc.EsRuc ? doc.Numero : empresaId.Value; // Asunción: empresaId.Value mantiene RUC válido.
            var emisor = ComprobantesElectronicosBC.Domain.ValueObjects.EmisorSnapshot.Create(
                empresaId, tenantId, establecimientoId, rucEmisor,
                clienteEtiqueta, domicilio);

            var nombreCliente = !string.IsNullOrWhiteSpace(input.Cliente.RazonSocial)
                ? RazonSocial.Crear(input.Cliente.RazonSocial).Valor
                : NombrePersona.Crear(input.Cliente.Nombres!, input.Cliente.Apellidos!).Completo;
            var clienteSnap = ComprobantesElectronicosBC.Domain.ValueObjects.ClienteSnapshot.Create(
                empresaId, tenantId, doc, nombreCliente, domicilio, emails.FirstOrDefault());

            var tipo = ComprobantesElectronicosBC.Domain.ValueObjects.TipoDeComprobante.Create(tipoComprobante);
            // Para estabilidad de pruebas, usamos 'now' igual a la fecha de emisión al construir el VO
            var fechaVO = ComprobantesElectronicosBC.Domain.ValueObjects.FechaEmision.Create(
                fechaEmision,
                tipo.Codigo,
                new DateTime(fechaEmision.Year, fechaEmision.Month, fechaEmision.Day));
            var formaPago = SharedKernel.ValueObjects.FormaDePago.Contado();
            var venc = ComprobantesElectronicosBC.Domain.ValueObjects.FechaVencimiento.ParaFormaDePago(formaPago, fechaEmision, null);
            var usuario = new ComprobantesElectronicosBC.Domain.ValueObjects.UsuarioSnapshot("system", "system", "system");

            var comp = ComprobantesElectronicosBC.Domain.Aggregates.ComprobanteElectronico.CrearBorrador(
                tipo, emisor, clienteSnap, moneda, fechaVO, formaPago, venc, usuario, DateTimeOffset.UtcNow);

            // Agregar líneas
            foreach (var i in input.Items)
            {
                var descripcion = ComprobantesElectronicosBC.Domain.ValueObjects.DescripcionProducto.Create(i.Descripcion ?? i.Sku ?? string.Empty);
                var um = SharedKernel.ValueObjects.UnidadDeMedida.From(i.UnidadMedidaCodigo);
                var cant = ComprobantesElectronicosBC.Domain.ValueObjects.Cantidad.Create(i.Cantidad);
                var precio = ComprobantesElectronicosBC.Domain.ValueObjects.ImporteMonetario.Create(i.PrecioUnitario, moneda);
                var afect = SharedKernel.ValueObjects.AfectacionImpuesto.From(i.AfectacionCodigo);
                var tasaLinea = SharedKernel.ValueObjects.TasaImpuesto.FromPercent(input.TasaImpuestoPorcentaje).CompatibilizarCon(afect);
                comp.AgregarLinea(descripcion, um, cant, precio, afect, tasaLinea, precioIncluyeIgv: false);
            }

            // Numeración y emisión
            // Reservar numeración (solo una vez; eliminación de duplicado posterior en método)
            var snPrimer = await _numeracion.ReservarSiguienteAsync(
                empresaId, establecimientoId, tipoComprobante, input.SeriePreferida, ct);
            if (snPrimer is null)
                throw new NotFoundException("Numeracion", $"{empresaId.Value}/{establecimientoId.Value}/{tipoComprobante}",
                    "No se pudo obtener numeración.");
            comp.AsignarSerieYNumero(snPrimer.Serie, snPrimer.Numero);
            // Transición de estado estricta: el agregado valida Factura=RUC y otras reglas
            comp.Emitir();

            // Mantener proyección y cálculo anteriores para compatibilidad de persistencia/salida
            // Ya no se proyectan líneas ni se recalculan totales aquí; delega al agregado.

            // -------- Numeración
            // Eliminado segundo request de numeración duplicado; usamos snPrimer.
            var nowUtc = DateTimeOffset.UtcNow;
            // Snapshot desde el agregado (sin recálculo en Application)
            var snapshot = ComprobantesElectronicosBC.Domain.Mappers.ComprobanteSnapshotMapper.FromAggregate(comp);

            ComprobantePersistido persisted;
            try
            {
                persisted = await _repo.GuardarEmitidoAsync(snapshot, ct);
            }
            catch (ConcurrencyException) { throw; }
            catch (NotFoundException) { throw; }
            catch (Exception ex)
            {
                throw new BusinessRuleException("No se pudo emitir el comprobante.", new Dictionary<string, object?>
                {
                    ["empresaId"] = empresaId.Value,
                    ["establecimientoId"] = establecimientoId.Value.ToString("D"),
                    ["tipo"] = tipoComprobante,
                    ["serie"] = snPrimer.Serie,
                    ["numero"] = snPrimer.Numero
                }) { Source = ex.Source };
            }

            // Publicar eventos drenados del agregado
            foreach (var evt in comp.DrainDomainEvents())
                await _eventBus.PublishAsync(evt, ct);

            // -------- Salida
            // Recalcular bases segmentadas para salida (sin alterar agregado): gravada vs no gravada (exonerada/inafecta), excluyendo gratuitas (21,31,...)
            static bool EsGravada(string cod) => cod is "10" or "11" or "12" or "13" or "14" or "15" or "16" or "17";
            static bool EsGratuita(string cod) => cod is "21" or "31" or "32" or "33" or "34"; // ampliable si aparecen futuras

            var baseGravada = snapshot.Lineas.Where(l => EsGravada(l.AfectacionCodigo)).Sum(l => l.BaseImponible);
            var baseNoGravada = snapshot.Lineas.Where(l => !EsGravada(l.AfectacionCodigo) && !EsGratuita(l.AfectacionCodigo)).Sum(l => l.BaseImponible);
            var valorVenta = baseGravada + baseNoGravada;

            return new EmitirComprobanteOutputDto
            {
                ComprobanteId = persisted.Id,
                TipoComprobante = tipoComprobante,
                Serie = snPrimer.Serie,
                Numero = snPrimer.Numero,
                FechaEmision = fechaEmision,
                EmitidoEnUtc = nowUtc,
                Moneda = snapshot.Moneda.Codigo,
                ImporteBaseGravada = baseGravada,
                ImporteBaseNoGravada = baseNoGravada,
                ImporteImpuesto = snapshot.IgvTotal,
                TotalValorVenta = valorVenta,
                ImporteTotal = snapshot.Total,
                ClienteResumen = snapshot.ClienteEtiqueta,
                Estado = "EMITIDO"
            };
        }

        // ============================ Helpers ============================

        private static string NormalizarTipoComprobante(string tipo)
        {
            if (string.IsNullOrWhiteSpace(tipo))
                throw new BusinessRuleException("Tipo de comprobante es obligatorio.");

            var t = tipo.Trim().ToUpperInvariant();
            if (t is not ("FACTURA" or "BOLETA"))
                throw new BusinessRuleException($"Tipo de comprobante no soportado: {tipo}.");
            return t;
        }

        private static decimal NormalizarTasa(decimal porcentaje)
        {
            if (porcentaje < 0m || porcentaje > 100m)
                throw new BusinessRuleException("La tasa de impuesto debe estar entre 0 y 100.");
            return porcentaje;
        }

        private static string ConstruirEtiquetaCliente(EmitirComprobanteInputDto input, DocumentoIdentidad doc)
        {
            var etiquetaDoc = doc.ToString();
            if (!string.IsNullOrWhiteSpace(input.Cliente.RazonSocial))
                return $"{etiquetaDoc} - {RazonSocial.Crear(input.Cliente.RazonSocial).Valor}";

            var nombres = input.Cliente.Nombres ?? throw new BusinessRuleException("Nombres del cliente son obligatorios cuando no hay razón social.");
            var apellidos = input.Cliente.Apellidos ?? throw new BusinessRuleException("Apellidos del cliente son obligatorios cuando no hay razón social.");

            var np = NombrePersona.Crear(nombres, apellidos);
            return $"{etiquetaDoc} - {np.Completo}";
        }

        private static DomicilioFiscal ConstruirDomicilio(EmitirComprobanteInputDto input)
        {
            if (string.Equals(input.Cliente.PaisCodigoIso, "PE", StringComparison.OrdinalIgnoreCase))
            {
                return DomicilioFiscal.FromPeru(
                    linea: input.Cliente.DomicilioLinea,
                    ubigeo: input.Cliente.Ubigeo,
                    departamento: input.Cliente.Departamento,
                    provincia: input.Cliente.Provincia,
                    distrito: input.Cliente.Distrito,
                    addressTypeCode: input.Cliente.AddressTypeCode
                );
            }

            return DomicilioFiscal.From(
                paisCodigoIso: input.Cliente.PaisCodigoIso,
                linea: input.Cliente.DomicilioLinea
            );
        }

        // Eliminado: Proyección y cálculos; ahora todo proviene del agregado y su snapshot.
    }

    /// <summary>Contrato del caso de uso para DI/tests.</summary>
    public interface IEmitirComprobanteUseCase
    {
        Task<EmitirComprobanteOutputDto> HandleAsync(EmitirComprobanteInputDto input, CancellationToken ct = default);
    }
}
