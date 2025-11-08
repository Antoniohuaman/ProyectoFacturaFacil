#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ComprobantesElectronicosBC.Application.DTOs;
using ComprobantesElectronicosBC.Application.Interfaces; // Ajusta si tus puertos tienen otro nombre/ubicación
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

            // -------- Ítems y cálculos
            var lineas = ProyectarLineas(input.Items, moneda);
            if (lineas.Count == 0)
                throw new BusinessRuleException("No hay líneas válidas para emitir.");

            CalcularTotales(
                lineas, tasa, moneda,
                out var baseGravada, out var baseNoGravada,
                out var impuesto, out var totalValorVenta, out var total);

            if (total.Monto < 0m)
                throw new BusinessRuleException("El total no puede ser negativo.");

            // -------- Numeración
            var sn = await _numeracion.ReservarSiguienteAsync(
                empresaId, establecimientoId, tipoComprobante, input.SeriePreferida, ct);

            if (sn is null)
                throw new NotFoundException("Numeracion", $"{empresaId.Value}/{establecimientoId.Value}/{tipoComprobante}",
                    "No se pudo obtener numeración.");

            // -------- Persistencia
            var nowUtc = DateTimeOffset.UtcNow;

            var data = new ComprobanteParaEmitir(
                empresaId,
                establecimientoId,
                tipoComprobante,
                sn.Serie,
                sn.Numero,
                fechaEmision,
                moneda,
                tasa,
                doc,
                clienteEtiqueta,
                domicilio,
                emails,
                telefonos,
                lineas,
                baseGravada,
                baseNoGravada,
                impuesto,
                totalValorVenta,
                total,
                input.Observaciones,
                nowUtc
            );

            ComprobantesElectronicosBC.Application.Interfaces.ComprobantePersistido persisted;
            try
            {
                persisted = await _repo.GuardarEmitidoAsync(data, ct);
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
                    ["serie"] = sn.Serie,
                    ["numero"] = sn.Numero
                }) { Source = ex.Source };
            }

            // -------- Evento de dominio
            // Evento se migrará a dominio; aquí solo se publica el drenado del agregado (pendiente de refactor completo).
            // Por ahora reutilizamos el record de dominio para mantener compatibilidad de pruebas.
            var evt = new ComprobantesElectronicosBC.Domain.Events.ComprobanteEmitidoDomainEvent(
                empresaId,
                establecimientoId,
                persisted.Id,
                tipoComprobante,
                sn.Serie,
                sn.Numero,
                moneda.Codigo,
                total.Monto,
                nowUtc.UtcDateTime);
            await _eventBus.PublishAsync(evt, ct);

            // -------- Salida
            return new EmitirComprobanteOutputDto
            {
                ComprobanteId = persisted.Id,
                TipoComprobante = tipoComprobante,
                Serie = sn.Serie,
                Numero = sn.Numero,
                FechaEmision = fechaEmision,
                EmitidoEnUtc = nowUtc,
                Moneda = moneda.Codigo,
                ImporteBaseGravada = baseGravada.Monto,
                ImporteBaseNoGravada = baseNoGravada.Monto,
                ImporteImpuesto = impuesto.Monto,
                TotalValorVenta = totalValorVenta.Monto,
                ImporteTotal = total.Monto,
                ClienteResumen = clienteEtiqueta,
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

        private static List<LineaCalculada> ProyectarLineas(IEnumerable<EmitirComprobanteInputDto.ItemDto> items, Moneda moneda)
        {
            var list = new List<LineaCalculada>();

            foreach (var i in items)
            {
                if (i.Cantidad <= 0m) throw new BusinessRuleException($"Cantidad inválida para SKU '{i.Sku}'.");
                if (i.PrecioUnitario < 0m) throw new BusinessRuleException($"Precio unitario inválido para SKU '{i.Sku}'.");

                var sku = (i.Sku ?? string.Empty).Trim();
                var uom = UnidadDeMedida.From(i.UnidadMedidaCodigo);
                var afect = AfectacionImpuesto.From(i.AfectacionCodigo);

                var pu = Dinero.Create(i.PrecioUnitario, moneda);
                var baseLinea = pu.Multiplicar(i.Cantidad);

                // Gratuita (21) no suma a bases (base contributiva = 0)
                var baseContributiva = afect.EsGratuita ? Dinero.Cero(moneda) : baseLinea;

                list.Add(new LineaCalculada(
                    sku: sku,
                    descripcion: i.Descripcion?.Trim() ?? sku,
                    unidadMedida: uom,
                    cantidad: i.Cantidad,
                    precioUnitario: pu,
                    BaseLinea: baseContributiva,
                    Afectacion: afect
                ));
            }

            return list;
        }

        private static void CalcularTotales(
            IReadOnlyList<LineaCalculada> lineas,
            TasaImpuesto tasaGeneral,
            Moneda moneda,
            out Dinero baseGravada,
            out Dinero baseNoGravada,
            out Dinero impuesto,
            out Dinero totalValorVenta,
            out Dinero total)
        {
            baseGravada = Dinero.Cero(moneda);
            baseNoGravada = Dinero.Cero(moneda);
            impuesto = Dinero.Cero(moneda);

            foreach (var l in lineas)
            {
                if (l.Afectacion.GravaImpuesto && !l.Afectacion.EsGratuita)
                {
                    baseGravada = baseGravada + l.BaseLinea;
                    var tasaAplicada = tasaGeneral.CompatibilizarCon(l.Afectacion);
                    var impLinea = Dinero.Create(l.BaseLinea.Monto * tasaAplicada.Fraccion, moneda);
                    impuesto = impuesto + impLinea;
                }
                else
                {
                    baseNoGravada = baseNoGravada + l.BaseLinea; // en gratuita BaseLinea ya es 0
                }
            }

            totalValorVenta = baseGravada + baseNoGravada;
            total = totalValorVenta + impuesto;
        }

        // ============================ Contratos internos/records ============================

        /// <summary>Representa una línea lista para el cálculo/persistencia.</summary>
        public sealed record LineaCalculada(
            string sku,
            string descripcion,
            UnidadDeMedida unidadMedida,
            decimal cantidad,
            Dinero precioUnitario,
            Dinero BaseLinea,
            AfectacionImpuesto Afectacion)
        {
            public string Sku => sku;
            public string Descripcion => descripcion;
            public UnidadDeMedida UnidadDeMedida => unidadMedida;
            public decimal Cantidad => cantidad;
            public Dinero PrecioUnitario => precioUnitario;
        }

        /// <summary>Paquete de datos que el repositorio almacenará (adapta en tu adapter).</summary>
        public sealed record ComprobanteParaEmitir(
            EmpresaId empresaId,
            EstablecimientoId establecimientoId,
            string tipoComprobante,
            string serie,
            int numero,
            DateOnly fechaEmision,
            Moneda moneda,
            TasaImpuesto tasaImpuesto,
            DocumentoIdentidad docReceptor,
            string receptorEtiqueta,
            DomicilioFiscal domicilioFiscal,
            IReadOnlyList<Email> emails,
            Telefono telefonos,
            IReadOnlyList<LineaCalculada> lineas,
            Dinero baseGravada,
            Dinero baseNoGravada,
            Dinero impuesto,
            Dinero totalValorVenta,
            Dinero total,
            string? observaciones,
            DateTimeOffset emitidoEnUtc
        );

    // Eliminado: public sealed record ComprobantePersistido(Guid Id, int Version);

        // Eliminado: nested ComprobanteEmitidoDomainEvent (migrado a Domain/Events como record inmutable).
    }

    /// <summary>Contrato del caso de uso para DI/tests.</summary>
    public interface IEmitirComprobanteUseCase
    {
        Task<EmitirComprobanteOutputDto> HandleAsync(EmitirComprobanteInputDto input, CancellationToken ct = default);
    }
}
