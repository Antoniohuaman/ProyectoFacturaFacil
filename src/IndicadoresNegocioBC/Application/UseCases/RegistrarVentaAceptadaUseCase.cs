using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IndicadoresNegocioBC.Domain.Aggregates;
using IndicadoresNegocioBC.Domain.Repositories;
using IndicadoresNegocioBC.Domain.ValueObjects;
using SharedKernel.Events;
using SharedKernel.Application.Interfaces;

namespace IndicadoresNegocioBC.Application.UseCases.IndicadorNegocio
{
    /// <summary>
    /// Caso de uso: Registrar una venta aceptada en el agregado IndicadorNegocio.
    /// - Carga o crea el agregado por clave natural (Tipo + Periodo + Segmento).
    /// - Aplica la venta (idempotente a nivel de agregado).
    /// - Persiste (Add si es nuevo; Update si existe y hubo cambios).
    /// - Publica eventos: VentaAceptadaRegistrada y, si hubo transición, IndicadorNegocioActualizado.
    /// </summary>
    public sealed class RegistrarVentaAceptadaUseCase
    {
        private readonly IIndicadorNegocioRepository _repository;
        private readonly IEventBus _eventBus;
        private readonly ITenantContext _tenant;

        public RegistrarVentaAceptadaUseCase(
            IIndicadorNegocioRepository repository,
            IEventBus eventBus,
            ITenantContext tenant)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        }

        public async Task<RegistrarVentaAceptadaOutputDto> ExecuteAsync(
            RegistrarVentaAceptadaInputDto input,
            CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));

            // 1) Cargar por clave natural dentro del scope de empresa del tenant
            var empresaId = _tenant.EmpresaId;
            Domain.Aggregates.IndicadorNegocio? agregado =
                await _repository.GetByClaveAsync(input.Tipo, input.Periodo, input.Segmento, empresaId, ct);

            bool esNuevo = agregado is null;
            if (esNuevo)
            {
                // Si no existe, lo creamos vacío para el periodo y segmento dados.
                agregado = Domain.Aggregates.IndicadorNegocio.Crear(
                    tipo: input.Tipo,
                    periodo: input.Periodo,
                    segmento: input.Segmento,
                    ahora: DateTimeOffset.UtcNow // CreadoEn a ahora
                );
            }

            // 2) Tomar snapshot de estado y versión
            var estadoAntes = agregado!.Estado;
            var versionAntes = agregado.Version;

            // 3) Mapear DTO → ComprobanteVenta (VO interno del agregado) y aplicar
            var items = input.Items.Select(i => new Domain.Aggregates.IndicadorNegocio.ComprobanteVenta.Item(
                productoId: i.ProductoId,
                cantidad: i.Cantidad,
                subtotal: i.Subtotal
            )).ToList();

            var venta = new Domain.Aggregates.IndicadorNegocio.ComprobanteVenta(
                comprobanteId: input.ComprobanteId,
                fecha: input.Fecha,
                clienteId: input.ClienteId,
                total: input.Total,
                igv: input.Igv,
                items: items,
                vendedorId: input.VendedorId,
                tipoComprobante: input.TipoComprobante,
                establecimientoId: input.EstablecimientoId
            );

            agregado.RegistrarVentaAceptada(venta);

            // 4) Detectar si hubo mutación efectiva (idempotencia): versión cambió
            var versionDespues = agregado.Version;
            var cambioEfectivo = versionDespues != versionAntes;

            // 5) Persistir
            if (esNuevo)
            {
                await _repository.AddAsync(agregado, ct);
            }
            else if (cambioEfectivo)
            {
                await _repository.UpdateAsync(agregado, ct);
            }
            // Si no hubo cambio (venta ya registrada), no persistimos ni publicamos.

            // 6) Publicar eventos si hubo cambio
            if (cambioEfectivo)
            {
                // Normalización básica (igual que el agregado) para el evento
                static string Norm(string s) => s?.Trim().ToUpperInvariant() ?? string.Empty;

                var evtVenta = new Domain.Events.IndicadorNegocioEvents.VentaAceptadaRegistrada(
                    indicadorId: agregado.IndicadorId,
                    comprobanteId: input.ComprobanteId,
                    fecha: input.Fecha,
                    clienteId: input.ClienteId,
                    total: input.Total,
                    igv: input.Igv,
                    items: input.Items
                        .Select(x => new Domain.Events.IndicadorNegocioEvents.VentaItemEventData(x.ProductoId, x.Cantidad, x.Subtotal))
                        .ToList(),
                    vendedorId: input.VendedorId,
                    tipoComprobante: Norm(input.TipoComprobante),
                    establecimientoId: input.EstablecimientoId,
                    version: agregado.Version
                );
                await _eventBus.PublishAsync(evtVenta, ct);

                // Si hubo transición de estado (CREADO → ACTUALIZADO), emite evento
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

            // 7) Salida
            return new RegistrarVentaAceptadaOutputDto(
                indicadorId: agregado.IndicadorId,
                tipo: agregado.Tipo,
                periodo: agregado.Periodo,
                segmento: agregado.Segmento,
                estado: agregado.Estado,
                totalVentas: agregado.TotalVentas,
                totalComprobantes: agregado.TotalComprobantes,
                version: agregado.Version,
                fueIdempotente: !cambioEfectivo
            );
        }
    }
}
