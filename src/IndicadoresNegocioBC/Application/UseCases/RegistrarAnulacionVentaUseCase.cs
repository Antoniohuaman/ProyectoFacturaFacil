using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IndicadoresNegocioBC.Domain.Repositories;
using SharedKernel.Events;
using SharedKernel.Exceptions;
using SharedKernel.Application.Interfaces;

namespace IndicadoresNegocioBC.Application.UseCases.IndicadorNegocio
{
    /// <summary>
    /// Caso de uso: Registrar la anulación de una venta previamente aplicada.
    /// Flujo:
    /// 1) Carga el agregado por clave natural (Tipo + Periodo + Segmento; opcional Empresa).
    /// 2) Busca la venta en el rango del periodo para construir el evento (si existe y no estaba anulada).
    /// 3) Llama a RegistrarAnulacion(comprobanteId) en el agregado (idempotente).
    /// 4) Si hubo cambio (versión incrementó), persiste y publica evento de anulación.
    /// 5) Devuelve resumen del estado/totales.
    /// </summary>
    public sealed class RegistrarAnulacionVentaUseCase
    {
    private readonly Domain.Repositories.IIndicadorNegocioRepository _repository;
        private readonly IEventBus _eventBus;
    private readonly ITenantContext _tenant;

        public RegistrarAnulacionVentaUseCase(
            Domain.Repositories.IIndicadorNegocioRepository repository,
            IEventBus eventBus,
            ITenantContext tenant)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        }

        public async Task<RegistrarAnulacionVentaOutputDto> ExecuteAsync(
            RegistrarAnulacionVentaInputDto input,
            CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));

            // 1) Cargar agregado por clave natural dentro del scope de empresa del tenant
            var empresaId = _tenant.EmpresaId;
            Domain.Aggregates.IndicadorNegocio? agregado =
                await _repository.GetByClaveAsync(input.Tipo, input.Periodo, input.Segmento, empresaId, ct);

            if (agregado is null)
            {
                var key = $"Tipo={input.Tipo}, Periodo=[{input.Periodo.Inicio:yyyy-MM-dd}..{input.Periodo.FinInclusive:yyyy-MM-dd}], Segmento={input.Segmento}";
                throw new NotFoundException("IndicadorNegocio", key, "IndicadorNegocio no encontrado para la clave natural especificada.");
            }

            // 2) Snapshot pre-anulación: estado y versión
            var estadoAntes = agregado.Estado;
            var versionAntes = agregado.Version;

            // Para payload del evento: buscar la venta aún NO anulada dentro del periodo
            var ventaPre = agregado
                .ObtenerVentasPorRango(input.Periodo.Inicio, input.Periodo.FinInclusive)
                .FirstOrDefault(v => v.ComprobanteId == input.ComprobanteId);

            // 3) Aplicar anulación (idempotente). Si no existe o ya anulada, no muta.
            agregado.RegistrarAnulacion(input.ComprobanteId);

            // 4) ¿Hubo cambio?
            var huboCambio = agregado.Version != versionAntes;

            if (huboCambio)
            {
                // Persistir
                await _repository.UpdateAsync(agregado, ct);

                // Publicar evento de anulación (payload desde la vista pre-anulación)
                if (ventaPre is not null)
                {
                    var evt = new Domain.Events.IndicadorNegocioEvents.AnulacionRegistrada(
                        indicadorId: agregado.IndicadorId,
                        comprobanteId: ventaPre.ComprobanteId,
                        fecha: ventaPre.Fecha,
                        clienteId: ventaPre.ClienteId,
                        total: ventaPre.Total,
                        igv: ventaPre.Igv,
                        items: ventaPre.Items
                            .Select(x => new Domain.Events.IndicadorNegocioEvents.VentaItemEventData(x.ProductoId, x.Cantidad, x.Subtotal))
                            .ToList(),
                        vendedorId: ventaPre.VendedorId,
                        tipoComprobante: ventaPre.TipoComprobante,
                        establecimientoId: ventaPre.EstablecimientoId,
                        version: agregado.Version
                    );
                    await _eventBus.PublishAsync(evt, ct);
                }

                // Si el estado cambiara (poco probable en anulación), publicar transición
                if (!ReferenceEquals(estadoAntes, agregado.Estado) && !Equals(estadoAntes, agregado.Estado))
                {
                    var evtEstado = new Domain.Events.IndicadorNegocioEvents.IndicadorNegocioActualizado(
                        indicadorId: agregado.IndicadorId,
                        estadoAnterior: estadoAntes,
                        estadoNuevo: agregado.Estado,
                        version: agregado.Version
                    );
                    await _eventBus.PublishAsync(evtEstado, ct);
                }
            }

            // 5) Salida
            return new RegistrarAnulacionVentaOutputDto(
                indicadorId: agregado.IndicadorId,
                tipo: agregado.Tipo,
                periodo: agregado.Periodo,
                segmento: agregado.Segmento,
                estado: agregado.Estado,
                totalVentas: agregado.TotalVentas,
                totalComprobantes: agregado.TotalComprobantes,
                version: agregado.Version,
                fueIdempotente: !huboCambio
            );
        }
    }
}
