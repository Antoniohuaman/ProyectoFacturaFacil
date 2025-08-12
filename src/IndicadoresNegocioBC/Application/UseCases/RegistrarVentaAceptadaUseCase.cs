using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IndicadoresNegocioBC.Application.Contracts.Inbound;
using IndicadoresNegocioBC.Domain.Aggregates;
using IndicadoresNegocioBC.Domain.Repositories;
using IndicadoresNegocioBC.Domain.ValueObjects;

namespace IndicadoresNegocioBC.Application.UseCases
{
    /// <summary>
    /// Caso de uso: aplica una venta ACEPTADA al agregado IndicadorNegocio
    /// para Día, Semana, Mes y Año, y para: Ventas Diarias, Ranking Productos,
    /// Ranking Clientes y Ticket Promedio.
    ///
    /// Idempotente a nivel de dominio (ComprobanteId).
    /// </summary>
    public sealed class RegistrarVentaAceptadaUseCase
    {
        private readonly IIndicadorNegocioRepository _repo;

        public RegistrarVentaAceptadaUseCase(IIndicadorNegocioRepository repo)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public async Task ExecuteAsync(ComprobanteEmitidoAceptado evt, CancellationToken ct = default)
        {
            if (evt is null) throw new ArgumentNullException(nameof(evt));

            // --- 1) Mapear contrato -> Value Objects ---
            var moneda = new Moneda(evt.Moneda);

            var segmento = evt.EstablecimientoId.HasValue
                ? SegmentoIndicador.ParaEstablecimiento(
                    Establecimiento.Crear(evt.EmpresaId, evt.EstablecimientoId.Value),
                    moneda)
                : SegmentoIndicador.ParaEmpresa(evt.EmpresaId, moneda);

            var fechaVenta = DateOnly.FromDateTime(evt.FechaEmisionUtc.UtcDateTime);

            var items = (evt.Items ?? Array.Empty<ComprobanteEmitidoAceptadoItem>())
                .Select(i => new IndicadorNegocio.ComprobanteVenta.Item(
                    i.ProductoId,
                    i.Cantidad,
                    Dinero.Crear(i.Subtotal, moneda)));

            var venta = new IndicadorNegocio.ComprobanteVenta(
                evt.ComprobanteId,
                fechaVenta,
                evt.ClienteId,
                Dinero.Crear(evt.Total, moneda),
                Dinero.Crear(evt.Igv, moneda),
                items);

            // --- 2) Periodos (granularidades) ---
            var periodos = new[]
            {
                Periodo.PorDia(fechaVenta),
                Periodo.PorSemana(fechaVenta),
                Periodo.PorMes(fechaVenta.Year, fechaVenta.Month),
                Periodo.PorAnio(fechaVenta.Year)
            };

            // --- 3) Tipos de indicador ---
            var tipos = new[]
            {
                IndicadorNegocio.TipoIndicador.VentaDiaria,
                IndicadorNegocio.TipoIndicador.RankingProductos,
                IndicadorNegocio.TipoIndicador.RankingClientes,
                IndicadorNegocio.TipoIndicador.TicketPromedio
            };

            // --- 4) Orquestación por (tipo, periodo) ---
            foreach (var periodo in periodos)
            {
                foreach (var tipo in tipos)
                {
                    // cargar o crear por clave natural
                    var agregado = await _repo.GetByClaveAsync(tipo, periodo, segmento, ct);
                    var esNuevo = agregado is null;

                    agregado ??= IndicadorNegocio.Crear(tipo, periodo, segmento);

                    // mutar (idempotente dentro del AR)
                    agregado.RegistrarVentaAceptada(venta);

                    // persistir
                    if (esNuevo)
                        await _repo.AddAsync(agregado, ct);
                    else
                        await _repo.UpdateAsync(agregado, ct);
                }
            }
        }
    }
}