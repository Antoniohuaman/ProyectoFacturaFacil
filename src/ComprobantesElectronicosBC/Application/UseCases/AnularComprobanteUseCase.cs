using IUnitOfWork = ComprobantesElectronicosBC.Application.Interfaces.IUnitOfWork;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using ComprobantesElectronicosBC.Domain.Aggregates;
using ComprobantesElectronicosBC.Domain.Repositories;
using SharedKernel.Exceptions;
using ComprobantesElectronicosBC.Application.Interfaces; // IUnitOfWork

namespace ComprobantesElectronicosBC.Application.UseCases.AnularComprobante
{
    /// <summary>
    /// Puerto de aplicación para encapsular la lógica de anulación contra el agregado.
    /// La implementación concreta vive en Adapters/Infrastructure (p.ej., EF Core),
    /// mapea el DTO a VOs (NotaInterna, etc.) y llama a los métodos del agregado.
    /// Debe devolver el agregado actualizado y el DTO de salida.
    /// </summary>
    public interface IComprobanteAnulador
    {
        Task<(ComprobanteElectronico Anulado, AnularComprobanteOutputDto Resultado)>
            AnularAsync(ComprobanteElectronico original,
                        AnularComprobanteInputDto input,
                        CancellationToken ct);
    }

    /// <summary>
    /// Caso de uso: anular un comprobante (anulación lógica).
    /// Orquesta:
    ///  - Verifica existencia.
    ///  - Valida sintácticamente el motivo (1..1000).
    ///  - Delega la lógica de anulación al <see cref="IComprobanteAnulador"/>.
    ///  - Persiste cambios mediante repositorio + UoW.
    /// </summary>
    public sealed class AnularComprobanteUseCase
    {
        private readonly IComprobanteRepository _repo;
        private readonly IUnitOfWork _uow;
        private readonly IComprobanteAnulador _anulador;
        private readonly SharedKernel.Events.IEventBus? _eventBus; // inyección opcional para no romper firmas existentes

        // Para validación simple del motivo (no permitir solo espacios/control).
        private static readonly Regex OnlySpaces = new(@"^\s*$", RegexOptions.Compiled);

        /// <summary>Longitud máxima alineada con <c>NotaInterna</c> del dominio.</summary>
        public const int MaxMotivoLength = 1000;

        public AnularComprobanteUseCase(
            IComprobanteRepository repo,
            IUnitOfWork uow,
            IComprobanteAnulador anulador)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
            _anulador = anulador ?? throw new ArgumentNullException(nameof(anulador));
        }

        /// <summary>Nuevo constructor que permite publicar eventos drenados del agregado tras la persistencia.</summary>
        public AnularComprobanteUseCase(
            IComprobanteRepository repo,
            IUnitOfWork uow,
            IComprobanteAnulador anulador,
            SharedKernel.Events.IEventBus eventBus)
            : this(repo, uow, anulador)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public async Task<AnularComprobanteOutputDto> ExecuteAsync(
            AnularComprobanteInputDto input,
            CancellationToken ct = default)
        {
            if (input.ComprobanteId == Guid.Empty)
                throw new ArgumentException("ComprobanteId es obligatorio.", nameof(input));

            if (string.IsNullOrWhiteSpace(input.Motivo) || OnlySpaces.IsMatch(input.Motivo))
                throw new ArgumentException("El motivo de anulación es obligatorio.", nameof(input));

            if (input.Motivo.Length > MaxMotivoLength)
                throw new ArgumentException($"El motivo no debe exceder {MaxMotivoLength} caracteres.", nameof(input));

            // Cargar agregado
            var original = await _repo.GetByIdAsync(input.ComprobanteId, ct);
            if (original is null)
                throw NotFoundException.For<ComprobanteElectronico>(input.ComprobanteId);

            // Delegar la anulación al adaptador (aplica reglas del agregado y mapea VOs)
            var expectedVersion = original.Version; // captura para control de concurrencia
            var (anulado, output) = await _anulador.AnularAsync(original, input, ct);

            // Persistencia (anulación lógica: UpdateAsync con expectedVersion; no usamos RemoveAsync)
            await _repo.UpdateAsync(anulado, expectedVersion, ct);
            await _uow.CommitAsync(ct);

            // Publicación de eventos de dominio drenados (mínimo acoplamiento; sólo si se inyectó EventBus)
            if (_eventBus is not null)
            {
                var drained = anulado.DrainDomainEvents();
                if (drained.Count > 0)
                    await _eventBus.PublishAsync(drained, ct);
            }

            return output;
        }
    }
}
