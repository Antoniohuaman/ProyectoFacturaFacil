using IUnitOfWork = ComprobantesElectronicosBC.Application.Interfaces.IUnitOfWork;
using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ComprobantesElectronicosBC.Domain.Aggregates;
using ComprobantesElectronicosBC.Domain.Repositories;
using SharedKernel.Exceptions;
using ComprobantesElectronicosBC.Application.Interfaces; // IUnitOfWork

namespace ComprobantesElectronicosBC.Application.UseCases.CorregirComprobante
{
    /// <summary>
    /// Puerto de aplicación que encapsula cómo aplicar las correcciones en el agregado.
    /// Su implementación concreta vive en Adapters/Infrastructure.
    /// Debe:
    ///   - Cargar los datos necesarios del agregado (si hiciera falta).
    ///   - Aplicar overrides (serie/número/fechas/observaciones/cliente/etc.) respetando el dominio.
    ///   - Devolver el agregado listo para persistir y el DTO resultado.
    /// </summary>
    public interface IComprobanteCorrector
    {
        Task<(ComprobanteElectronico Actualizado, CorregirComprobanteOutputDto Datos)>
            CorregirAsync(ComprobanteElectronico original,
                          CorregirComprobanteInputDto input,
                          CancellationToken ct);
    }

    /// <summary>
    /// Caso de uso: corregir un comprobante (actualiza cabecera/otros campos permitidos).
    /// Orquesta:
    ///  - Verifica existencia del comprobante.
    ///  - Si Serie–Número vienen fijos, valida formato y unicidad antes de tocar el agregado.
    ///  - Delega la lógica de corrección a <see cref="IComprobanteCorrector"/>.
    ///  - Persiste cambios.
    /// </summary>
    public sealed class CorregirComprobanteUseCase
    {
        private readonly IComprobanteRepository _repo;
        private readonly IUnitOfWork _uow;
        private readonly IComprobanteCorrector _corrector;

        public CorregirComprobanteUseCase(
            IComprobanteRepository repo,
            IUnitOfWork uow,
            IComprobanteCorrector corrector)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
            _corrector = corrector ?? throw new ArgumentNullException(nameof(corrector));
        }

        public async Task<CorregirComprobanteOutputDto> ExecuteAsync(
            CorregirComprobanteInputDto input,
            CancellationToken ct = default)
        {
            if (input.ComprobanteId == Guid.Empty)
                throw new ArgumentException("ComprobanteId es obligatorio.", nameof(input));

            // Si el llamador fija Serie/Número, deben venir ambos y con formato válido
            if (input.Serie is not null || input.Numero is not null)
            {
                if (string.IsNullOrWhiteSpace(input.Serie) || !input.Numero.HasValue)
                    throw new ArgumentException("Si se fija la Serie/Número, deben indicarse ambos.");

                ValidarSerie(input.Serie!);
                ValidarNumero(input.Numero!.Value);

                // Evitar colisiones antes de tocar el agregado
                var existe = await _repo.ExistsSerieNumeroAsync(input.Serie!, input.Numero!.Value, ct);
                if (existe)
                    throw new BusinessRuleException("Ya existe un comprobante con la misma Serie–Número.");
            }

            // Cargar el agregado a corregir
            var original = await _repo.GetByIdAsync(input.ComprobanteId, ct);
            if (original is null)
                throw NotFoundException.For<ComprobanteElectronico>(input.ComprobanteId);

            // Delegar al corrector (infraestructura adapta DTO → VOs y aplica reglas del agregado)
            var (actualizado, datos) = await _corrector.CorregirAsync(original, input, ct);

            // Persistir
            await _repo.UpdateAsync(actualizado, ct);
            await _uow.CommitAsync(ct);

            return datos;
        }

        // ===== Validaciones locales (alineadas a VO SerieYNumero) =====
        private static readonly Regex SerieRegex = new(@"^[A-Z0-9]{1,4}$", RegexOptions.Compiled);

        private static void ValidarSerie(string serie)
        {
            var s = serie.Trim().ToUpperInvariant();
            if (!SerieRegex.IsMatch(s))
                throw new ArgumentException("La serie debe ser 1..4 caracteres alfanuméricos A–Z/0–9.", nameof(serie));
        }

        private static void ValidarNumero(int numero)
        {
            if (numero < 1 || numero > 99_999_999)
                throw new ArgumentOutOfRangeException(nameof(numero), "El número debe estar entre 1 y 99,999,999.");
        }
    }
}
