using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConfiguracionSistemaBC.Domain.Aggregates;
using ConfiguracionSistemaBC.Domain.Repositories;
using ConfiguracionSistemaBC.Application.Interfaces;   // IUnitOfWork
using SharedKernel.Application.Interfaces;      // ITenantContext
using SharedKernel.ValueObjects;               // EmpresaId
using ConfiguracionSistemaBC.Domain.ValueObjects; // FormaDePago

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Registra una o más formas de pago PERSONALIZADAS en la empresa.
    /// - Respeta las reglas del aggregate: no se pueden tocar/eliminar las del sistema.
    /// - Unicidad por (PaymentMeansCode|Metodo|Nombre canonizado).
    /// - Si varias del input vienen como default, se rechaza (una sola permitida).
    /// </summary>
    public sealed class RegistrarFormasDePagoUseCase
    {
        private readonly IConfiguracionEmpresaRepository _repo;
        private readonly IUnitOfWork _uow;
        private readonly ITenantContext _tenant;

        public RegistrarFormasDePagoUseCase(
            IConfiguracionEmpresaRepository repo,
            IUnitOfWork uow,
            ITenantContext tenantContext)
        {
            _repo   = repo   ?? throw new ArgumentNullException(nameof(repo));
            _uow    = uow    ?? throw new ArgumentNullException(nameof(uow));
            _tenant = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        }

        public async Task<RegistrarFormasDePagoOutputDto> HandleAsync(
            RegistrarFormasDePagoInputDto input,
            CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            if (input.Items is null || input.Items.Count == 0)
                throw new ArgumentException("Debes enviar al menos una forma de pago.", nameof(input.Items));

            // Empresa destino (input o contexto)
            var empresaId = !string.IsNullOrWhiteSpace(input.EmpresaId)
                ? EmpresaId.From(input.EmpresaId!.Trim())
                : _tenant.EmpresaId ?? throw new InvalidOperationException("No hay EmpresaId en el contexto.");

            var empresa = await _repo.GetByEmpresaIdAsync(empresaId, ct)
                ?? throw new KeyNotFoundException("No se encontró la configuración de la empresa.");

            // Validaciones de conjunto
            var defaultsMarcados = input.Items.Count(i => i.EsPorDefecto);
            if (defaultsMarcados > 1)
                throw new InvalidOperationException("Solo puedes marcar una forma de pago como 'por defecto'.");

            foreach (var i in input.Items)
            {
                if (string.IsNullOrWhiteSpace(i.Tipo))
                    throw new ArgumentNullException(nameof(i.Tipo), "Tipo es obligatorio (CONTADO/CREDITO).");
                if (string.IsNullOrWhiteSpace(i.Nombre))
                    throw new ArgumentNullException(nameof(i.Nombre), "Nombre es obligatorio.");
                if (i.EsPorDefecto && !i.Visible)
                    throw new InvalidOperationException("No puedes establecer por defecto una forma de pago oculta.");
            }

            var versionOriginal = empresa.Version;
            var creadas = new List<RegistrarFormasDePagoOutputDto.FormaPagoCreada>();

            // Procesar altas
            foreach (var item in input.Items)
            {
                var tipo = item.Tipo.Trim().ToUpperInvariant();
                var valor = MapFormaDePago(tipo, item.Metodo);

                // Lanza si ya existe una con misma firma (valor+metodo+nombre canonizado).
                var id = empresa.AgregarFormaDePagoPersonalizada(
                    valor,
                    item.Nombre.Trim(),
                    visible: item.Visible,
                    orden: item.Orden,
                    esPorDefecto: item.EsPorDefecto
                );

                // Leer el estado actual del agregado para armar salida de esa creada
                var st = empresa.ListarFormasDePago().First(fp => fp.Id == id);
                creadas.Add(new RegistrarFormasDePagoOutputDto.FormaPagoCreada
                {
                    Id = st.Id,
                    PaymentMeansCode = st.Valor.PaymentMeansCode,
                    MetodoCodigo = st.Valor.MetodoCodigo,
                    Nombre = st.Nombre,
                    Visible = st.Visible,
                    EsPorDefecto = st.EsPorDefecto,
                    EsSistema = st.EsSistema,
                    Orden = st.Orden
                });
            }

            await _repo.UpdateAsync(empresa, ct);
            await _uow.CommitAsync(ct);

            var defaultActual = empresa.ObtenerFormaDePagoPorDefecto();
            var total = empresa.ListarFormasDePago().Count;

            return new RegistrarFormasDePagoOutputDto
            {
                EmpresaId = empresa.EmpresaId.Value,
                Creadas = creadas,
                FormaPagoDefaultId = defaultActual?.Id,
                TotalFormasDePago = total
            };
        }

        // -------------------- Helpers --------------------

        private static FormaDePago MapFormaDePago(string tipo, string? metodoRaw)
        {
            if (tipo == "CREDITO")
            {
                // Método no aplica; nombre distingue el plazo (“Crédito 30 días”, etc.)
                return FormaDePago.Credito();
            }

            if (tipo != "CONTADO")
                throw new InvalidOperationException("Tipo inválido. Usa CONTADO o CREDITO.");

            var metodo = (metodoRaw ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(metodo))
            {
                // Contado genérico
                return FormaDePago.Contado();
            }

            return metodo switch
            {
                "EFECTIVO"      => FormaDePago.ContadoEfectivo(),
                "TARJETA"       => FormaDePago.ContadoTarjeta(),
                "TRANSFERENCIA" => FormaDePago.ContadoTransferencia(),
                "YAPE"          => FormaDePago.ContadoYape(),
                "PLIN"          => FormaDePago.ContadoPlin(),
                "DEPOSITO"      => FormaDePago.ContadoDeposito(),
                _               => throw new InvalidOperationException("Método de CONTADO inválido.")
            };
        }
    }
}
