using IUnitOfWork = ComprobantesElectronicosBC.Application.Interfaces.IUnitOfWork;
using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ComprobantesElectronicosBC.Domain.Aggregates;
using ComprobantesElectronicosBC.Domain.Repositories;
using SharedKernel.Exceptions;
using ComprobantesElectronicosBC.Application.Interfaces; // IUnitOfWork

namespace ComprobantesElectronicosBC.Application.UseCases.DuplicarComprobante
{
    /// <summary>
    /// Puerto de aplicación para clonar un comprobante del dominio.
    /// La implementación (Adapters) debe:
    ///  - Crear un nuevo agregado a partir del original.
    ///  - Aplicar overrides (serie/número/fecha) si vienen.
    ///  - Dejar el estado en "Borrador" (o equivalente en el agregado).
    ///  - Devolver datos básicos para el OutputDto.
    /// </summary>
    public interface IComprobanteDuplicator
    {
        Task<(ComprobanteElectronico Nuevo, DuplicarComprobanteOutputDto Datos)>
            DuplicarAsync(ComprobanteElectronico original,
                          DuplicarComprobanteInputDto input,
                          CancellationToken ct);
    }

    /// <summary>
    /// Caso de uso: duplicar un comprobante existente para generar un nuevo borrador.
    /// Reglas en este orquestador:
    ///  - Verifica existencia del origen.
    ///  - Si Serie–Número vienen fijos por input, valida formato y unicidad antes de clonar.
    ///  - Persiste el nuevo agregado y retorna datos del duplicado.
    /// </summary>
    public sealed class DuplicarComprobanteUseCase
    {
        private readonly IComprobanteRepository _repo;
        private readonly IUnitOfWork _uow;
        private readonly IComprobanteDuplicator _duplicator;

        public DuplicarComprobanteUseCase(
            IComprobanteRepository repo,
            IUnitOfWork uow,
            IComprobanteDuplicator duplicator)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
            _duplicator = duplicator ?? throw new ArgumentNullException(nameof(duplicator));
        }

        public async Task<DuplicarComprobanteOutputDto> ExecuteAsync(
            DuplicarComprobanteInputDto input,
            CancellationToken ct = default)
        {
            if (input.SourceId == Guid.Empty)
                throw new ArgumentException("SourceId es obligatorio.", nameof(input));

            // Validación básica de par Serie–Número si se pasan (deben venir ambos)
            if (input.Serie is not null || input.Numero is not null)
            {
                if (string.IsNullOrWhiteSpace(input.Serie) || !input.Numero.HasValue)
                    throw new ArgumentException("Si se fija la Serie/Número, deben indicarse ambos.");

                ValidarSerie(input.Serie!);
                ValidarNumero(input.Numero!.Value);

                // Colisión de Serie–Número (no depender del duplicador para esto)
                var existe = await _repo.ExistsSerieNumeroAsync(input.Serie!, input.Numero!.Value, ct);
                if (existe)
                    throw new BusinessRuleException("Ya existe un comprobante con la misma Serie–Número.");
            }

            // Cargar origen
            var original = await _repo.GetByIdAsync(input.SourceId, ct);
            if (original is null)
                throw NotFoundException.For<ComprobanteElectronico>(input.SourceId);

            // Delega el clonado a la infraestructura/adapters
            var (nuevo, datos) = await _duplicator.DuplicarAsync(original, input, ct);

            // Persistir
            await _repo.AddAsync(nuevo, ct);
            await _uow.CommitAsync(ct);

            // Devuelve los datos que el duplicador garantizó (incluye Id/Serie/Número/Tipo/Estado)
            return datos;
        }

        // ======= Validaciones locales alineadas a VO SerieYNumero =======
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
