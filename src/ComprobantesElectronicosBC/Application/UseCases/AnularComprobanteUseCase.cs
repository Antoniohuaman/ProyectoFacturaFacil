using System;
using System.Threading;
using System.Threading.Tasks;
using ComprobantesElectronicosBC.Application.DTOs;
using ComprobantesElectronicosBC.Domain.Repositories;

namespace ComprobantesElectronicosBC.Application.UseCases
{
    /// <summary>
    /// Marca un comprobante como ANULADO (baja confirmada por el servicio externo).
    /// Este caso de uso solo persiste el cambio de estado en el BC de Comprobantes.
    /// </summary>
    public sealed class AnularComprobanteUseCase
    {
        private readonly IComprobanteRepository _repo;
        private readonly IUnitOfWork _uow;

        public AnularComprobanteUseCase(IComprobanteRepository repo, IUnitOfWork uow)
        {
            _repo = repo;
            _uow  = uow;
        }

        /// <summary>
        /// Cambia el estado del comprobante a "CANCELLED".
        /// Las reglas de transición se validan dentro del agregado.
        /// </summary>
        public async Task<AnularComprobanteOutput> Handle(AnularComprobanteInput input, CancellationToken ct = default)
        {
            // 1) Cargar agregado
            var agg = await _repo.GetByIdAsync(input.ComprobanteId, ct);
            if (agg is null)
                throw new InvalidOperationException("No existe el comprobante indicado.");

            // 2) Aplicar transición de dominio
            var fechaBaja = input.FechaBaja ?? DateOnly.FromDateTime(DateTime.Now);
            // Convertir DateOnly a DateTimeOffset (inicio del día local)
            var fechaBajaDateTimeOffset = new DateTimeOffset(fechaBaja.ToDateTime(TimeOnly.MinValue));
            agg.MarcarAnulado(fechaBajaDateTimeOffset);

            // 3) Persistir
            await _repo.UpdateAsync(agg, ct);
            await _uow.SaveChangesAsync(ct);

            // 4) Salida
            return new AnularComprobanteOutput(
                ComprobanteId: agg.ComprobanteId,
                Estado:        agg.EstadoCodigo, // "CANCELLED"
                FechaBaja:     fechaBaja
            );
        }
    }
}
