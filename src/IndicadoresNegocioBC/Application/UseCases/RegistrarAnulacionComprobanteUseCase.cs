using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IndicadoresNegocioBC.Application.Contracts.Inbound;
using IndicadoresNegocioBC.Domain.Aggregates;
using IndicadoresNegocioBC.Domain.Repositories;
using IndicadoresNegocioBC.Domain.ValueObjects;

namespace IndicadoresNegocioBC.Application.UseCases
{
    /// <summary>
    /// Caso de uso: revierte una venta previamente aplicada.
    /// - Idempotente: si el comprobante ya fue revertido/no existe en el AR, el AR lo ignora.
    /// - Tolerante a falta de fecha de emisión: intenta en periodos candidatos
    ///   (día/semana/mes/año del día de anulación y sus periodos previos).
    /// </summary>
    public sealed class RegistrarAnulacionComprobanteUseCase
    {
        private readonly IIndicadorNegocioRepository _repo;

        public RegistrarAnulacionComprobanteUseCase(IIndicadorNegocioRepository repo)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        /// <summary>
        /// Ejecuta la anulación: busca los agregados por (tipo, periodo, segmento)
        /// y llama a <see cref="IndicadorNegocio.RegistrarAnulacion(Guid)"/>.
        /// </summary>
        public async Task ExecuteAsync(ComprobanteAnulado evt, CancellationToken ct = default)
        {
            if (evt is null) throw new ArgumentNullException(nameof(evt));

            // --- 1) Mapear contrato → Value Objects ---
            var moneda = new Moneda(evt.Moneda);

            var segmento = evt.EstablecimientoId.HasValue
                ? SegmentoIndicador.ParaEstablecimiento(
                    Establecimiento.Crear(evt.EmpresaId, evt.EstablecimientoId.Value),
                    moneda)
                : SegmentoIndicador.ParaEmpresa(evt.EmpresaId, moneda);

            var fechaBase = DateOnly.FromDateTime(evt.FechaAnulacionUtc.UtcDateTime);

            // --- 2) Periodos candidatos (actual y anterior por granularidad) ---
            var periodos = GenerarPeriodosCandidatos(fechaBase);

            // --- 3) Tipos de indicador mantenidos ---
            var tipos = new[]
            {
                IndicadorNegocio.TipoIndicador.VentaDiaria,
                IndicadorNegocio.TipoIndicador.RankingProductos,
                IndicadorNegocio.TipoIndicador.RankingClientes,
                IndicadorNegocio.TipoIndicador.TicketPromedio
            };

            // --- 4) Orquestación: si el agregado existe, aplicar anulación y persistir ---
            foreach (var periodo in periodos)
            {
                foreach (var tipo in tipos)
                {
                    var agg = await _repo.GetByClaveAsync(tipo, periodo, segmento, ct);
                    if (agg is null) continue; // no hay snapshot para ese periodo/tipo/segmento

                    agg.RegistrarAnulacion(evt.ComprobanteId); // idempotente dentro del AR
                    await _repo.UpdateAsync(agg, ct);
                }
            }
        }

        // -------- Helpers --------

        /// <summary>
        /// Genera periodos candidatos donde pudo haber caído la venta original:
        /// actual y previo para cada granularidad.
        /// </summary>
        private static IEnumerable<Periodo> GenerarPeriodosCandidatos(DateOnly fecha)
        {
            // Día actual y día anterior
            yield return Periodo.PorDia(fecha);
            yield return Periodo.PorDia(fecha.AddDays(-1));

            // Semana del día y semana previa
            yield return Periodo.PorSemana(fecha);
            yield return Periodo.PorSemana(fecha.AddDays(-7));

            // Mes del día y mes previo
            yield return Periodo.PorMes(fecha.Year, fecha.Month);
            var mesPrevio = fecha.AddMonths(-1);
            yield return Periodo.PorMes(mesPrevio.Year, mesPrevio.Month);

            // Año del día y año previo
            yield return Periodo.PorAnio(fecha.Year);
            yield return Periodo.PorAnio(fecha.AddYears(-1).Year);
        }
    }
}