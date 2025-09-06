using System;
using System.Threading;
using System.Threading.Tasks;
using ConfiguracionSistemaBC.Domain.Aggregates;            // SerieComprobante
using ConfiguracionSistemaBC.Domain.Repositories;          // ISerieComprobanteRepository, IUnitOfWork
using SharedKernel.Application.Interfaces;                 // ITenantContext
using SharedKernel.ValueObjects;                           // EmpresaId

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Marca una serie como "por defecto" para su tipo (Factura/Boleta) garantizando exclusividad por tipo.
    /// Reglas:
    /// - Debe pertenecer a la empresa del contexto.
    /// - No puede estar inhabilitada (el agregado valida).
    /// - Debe quedar como única por defecto para ese tipo.
    /// </summary>
    public sealed class EstablecerSeriePorDefectoUseCase
    {
        private readonly ISerieComprobanteRepository _repo;
        private readonly IUnitOfWork _uow;
        private readonly ITenantContext _tenant;

        public EstablecerSeriePorDefectoUseCase(
            ISerieComprobanteRepository repo,
            IUnitOfWork uow,
            ITenantContext tenantContext)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _uow  = uow  ?? throw new ArgumentNullException(nameof(uow));
            _tenant = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        }

        public async Task<EstablecerSeriePorDefectoOutputDto> HandleAsync(
            EstablecerSeriePorDefectoInputDto input,
            CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            if (string.IsNullOrWhiteSpace(input.SerieComprobanteId))
                throw new ArgumentNullException(nameof(input.SerieComprobanteId));

            if (!Guid.TryParse(input.SerieComprobanteId.Trim(), out var serieId))
                throw new ArgumentOutOfRangeException(nameof(input.SerieComprobanteId), "Guid de serie inválido.");

            var empresaId = _tenant.EmpresaId ?? throw new InvalidOperationException("Empresa no disponible en el contexto.");

            // 1) Cargar serie a marcar
            var serie = await _repo.GetByIdAsync(serieId, ct)
                        ?? throw new KeyNotFoundException("Serie no encontrada.");

            // 2) Validar pertenencia
            if (!string.Equals(serie.EmpresaId.Value, empresaId.Value, StringComparison.Ordinal))
                throw new InvalidOperationException("La serie no pertenece a la empresa actual.");

            // 3) Si ya es por defecto → idempotente (no tocamos nada)
            var yaEraDefault = serie.EsPorDefecto;

            // 4) Capturar la anterior default (si la hubiera) para retorno/evento
            var anteriorDefault = await _repo.GetDefaultByTipoAsync(empresaId, serie.Tipo, ct);

            // 5) Si vamos a cambiar, primero desmarcamos cualquiera anterior (exclusividad)
            if (!yaEraDefault && anteriorDefault is not null)
            {
                await _repo.UnsetDefaultForTipoAsync(empresaId, serie.Tipo, ct);
            }

            // 6) Marcar esta serie como por defecto (el agregado valida que esté habilitada)
            if (!yaEraDefault)
            {
                serie.EstablecerPorDefecto(true);
                await _repo.UpdateAsync(serie, input.ExpectedVersion, ct);
                await _uow.SaveChangesAsync(ct);
            }

            // 7) Salida
            return new EstablecerSeriePorDefectoOutputDto
            {
                EmpresaId = empresaId.Value,
                SerieComprobanteId = serie.Id,
                EstablecimientoId = serie.EstablecimientoId.Value,
                TipoComprobante = serie.Tipo.Codigo,    // "01" | "03"
                Serie = serie.Serie.Codigo,
                YaEraPorDefecto = yaEraDefault,
                AnteriorSeriePorDefectoId = anteriorDefault is not null && anteriorDefault.Id != serie.Id
                    ? anteriorDefault.Id
                    : (Guid?)null,
                VersionAplicada = yaEraDefault ? serie.Version : input.ExpectedVersion
            };
        }
    }
}
