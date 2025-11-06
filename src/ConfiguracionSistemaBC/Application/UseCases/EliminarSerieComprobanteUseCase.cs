using System;
using System.Threading;
using System.Threading.Tasks;
using ConfiguracionSistemaBC.Domain.Repositories;        // ISerieComprobanteRepository, IUnitOfWork
using ConfiguracionSistemaBC.Domain.Aggregates;          // SerieComprobante
using SharedKernel.Application.Interfaces;               // ITenantContext
using SharedKernel.ValueObjects;                         // EmpresaId
using ConfiguracionSistemaBC.Application.Interfaces;     // IUnitOfWork

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Elimina una serie de comprobante si y solo si NUNCA fue usada.
    /// Valida pertenencia a la empresa actual (multiempresa) y aplica concurrencia optimista.
    /// </summary>
    public sealed class EliminarSerieComprobanteUseCase
    {
        private readonly ISerieComprobanteRepository _seriesRepo;
        private readonly IUnitOfWork _uow;
        private readonly ITenantContext _tenant;

        public EliminarSerieComprobanteUseCase(
            ISerieComprobanteRepository seriesRepo,
            IUnitOfWork uow,
            ITenantContext tenantContext)
        {
            _seriesRepo = seriesRepo ?? throw new ArgumentNullException(nameof(seriesRepo));
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
            _tenant = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        }

        public async Task<EliminarSerieComprobanteOutputDto> HandleAsync(
            EliminarSerieComprobanteInputDto input,
            CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            if (string.IsNullOrWhiteSpace(input.SerieComprobanteId))
                throw new ArgumentNullException(nameof(input.SerieComprobanteId));

            if (!Guid.TryParse(input.SerieComprobanteId.Trim(), out var serieId))
                throw new ArgumentOutOfRangeException(nameof(input.SerieComprobanteId), "Guid de serie inválido.");

            var empresaId = _tenant.EmpresaId ?? throw new InvalidOperationException("No hay EmpresaId en el contexto actual.");

            // 1) Cargar la serie
            var serie = await _seriesRepo.GetByIdAsync(serieId, ct);
            if (serie is null)
                throw new KeyNotFoundException("Serie no encontrada.");

            // 2) Validar pertenencia a la empresa actual
            if (!string.Equals(serie.EmpresaId.Value, empresaId.Value, StringComparison.Ordinal))
                throw new InvalidOperationException("La serie no pertenece a la empresa actual.");

            // 3) Regla de negocio: solo si nunca fue usada
            if (!serie.PuedeEliminar) // equivalente a !FueUsada
                throw new InvalidOperationException("No se puede eliminar: la serie ya fue usada en emisión.");

            // 4) Eliminar (infra aplica expectedVersion)
            await _seriesRepo.DeleteAsync(serie.Id, input.ExpectedVersion, ct);
            await _uow.CommitAsync(ct);

            // 5) Salida
            return new EliminarSerieComprobanteOutputDto
            {
                Eliminado = true,
                SerieComprobanteId = serie.Id,
                EmpresaId = serie.EmpresaId.Value,
                EstablecimientoId = serie.EstablecimientoId.Value,
                TipoComprobante = serie.Tipo.Codigo,
                Serie = serie.Serie.Codigo,
                VersionEliminada = input.ExpectedVersion
            };
        }
    }
}
