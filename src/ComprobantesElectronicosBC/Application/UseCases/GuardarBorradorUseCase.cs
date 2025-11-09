using IUnitOfWork = ComprobantesElectronicosBC.Application.Interfaces.IUnitOfWork;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ComprobantesElectronicosBC.Domain.Aggregates;
using ComprobantesElectronicosBC.Domain.Repositories;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;
using ComprobantesElectronicosBC.Domain.ValueObjects;
using ComprobantesElectronicosBC.Application.Interfaces; // IUnitOfWork

namespace ComprobantesElectronicosBC.Application.UseCases.GuardarBorrador
{
    /// <summary>
    /// Contrato de factoría para construir/actualizar un Comprobante en estado borrador.
    /// La implementación concreta (en Domain o Adapters) conoce el agregado real y
    /// aplica el mapeo completo del DTO a dicho agregado.
    /// </summary>
    public interface IComprobanteDraftFactory
    {
        /// <summary>Crea un nuevo agregado en estado Borrador a partir del input.</summary>
        Task<ComprobanteElectronico> CrearAsync(GuardarBorradorInputDto input, CancellationToken ct);

        /// <summary>Aplica cambios del input sobre un agregado ya existente (borrador).</summary>
        Task<ComprobanteElectronico> AplicarAsync(ComprobanteElectronico actual, GuardarBorradorInputDto input, CancellationToken ct);
    }

    /// <summary>
    /// Caso de uso: Guardar borrador de CPE (crear o actualizar).
    /// Reglas mínimas que aplica directamente:
    ///  - Tipo válido ("01" / "03")
    ///  - Serie compatible con el tipo (F↔01, B↔03)
    ///  - Si se envía Número: valida Serie–Número con formato y unicidad
    /// El resto de reglas/invariantes las aplica la factoría o el agregado.
    /// </summary>
    public sealed class GuardarBorradorUseCase
    {
        private readonly IComprobanteRepository _repo;
        private readonly IUnitOfWork _uow;
        private readonly IComprobanteDraftFactory _factory;
        private readonly SharedKernel.Events.IEventBus? _eventBus;

        public GuardarBorradorUseCase(
            IComprobanteRepository repo,
            IUnitOfWork uow,
            IComprobanteDraftFactory factory)
        {
            _repo    = repo  ?? throw new ArgumentNullException(nameof(repo));
            _uow     = uow   ?? throw new ArgumentNullException(nameof(uow));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <summary>Constructor extendido para publicar eventos drenados del borrador creado/actualizado.</summary>
        public GuardarBorradorUseCase(
            IComprobanteRepository repo,
            IUnitOfWork uow,
            IComprobanteDraftFactory factory,
            SharedKernel.Events.IEventBus eventBus)
            : this(repo, uow, factory)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public async Task<GuardarBorradorOutputDto> ExecuteAsync(
            GuardarBorradorInputDto input,
            CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            if (string.IsNullOrWhiteSpace(input.EmpresaId))
                throw new BusinessRuleException("EMPRESA_OBLIGATORIA", "EmpresaId es obligatorio.");
            if (string.IsNullOrWhiteSpace(input.TenantId))
                throw new BusinessRuleException("TENANT_OBLIGATORIO", "TenantId es obligatorio.");
            if (string.IsNullOrWhiteSpace(input.TipoComprobante))
                throw new BusinessRuleException("TIPO_OBLIGATORIO", "Tipo de comprobante es obligatorio.");
            if (string.IsNullOrWhiteSpace(input.Serie))
                throw new BusinessRuleException("SERIE_OBLIGATORIA", "La serie es obligatoria.");

            // 1) Validar tipo y compatibilidad con serie (convención UI F*↔01, B*↔03)
            var tipo = TipoDeComprobante.Create(input.TipoComprobante);
            tipo.ValidarCompatibilidadConSerie(input.Serie);

            // 2) Si viene número, validar formato y unicidad Serie–Número
            if (input.Numero.HasValue)
            {
                var syn = SerieYNumero.Create(input.Serie, input.Numero.Value);
                var existente = await _repo.GetBySerieNumeroAsync(syn.Serie, syn.Numero, ct);
                if (existente is not null)
                {
                    // si estamos creando o si el existente no es el mismo Id => conflicto
                    var mismoId = input.Id.HasValue && TryGetId(existente, out var exId) && exId == input.Id.Value;
                    if (!mismoId)
                    {
                        throw new BusinessRuleException(
                            code: "SERIE_NUMERO_DUPLICADO",
                            message: $"Ya existe un comprobante con Serie–Número {syn.IdUbl}.");
                    }
                }
            }

            // 3) Crear o actualizar
            if (input.Id is null)
            {
                // Crear nuevo borrador
                var agregado = await _factory.CrearAsync(input, ct);
                await _repo.AddAsync(agregado, ct);
                await _uow.CommitAsync(ct);

                if (_eventBus is not null)
                {
                    var drained = agregado.DrainDomainEvents();
                    if (drained.Count > 0)
                        await _eventBus.PublishAsync(drained, ct);
                }

                var id = TryGetId(agregado, out var newId) ? newId : Guid.Empty;
                return new GuardarBorradorOutputDto(
                    id == Guid.Empty ? Guid.NewGuid() : id, // fallback defensivo
                    esNuevo: true,
                    serie: input.Serie,
                    numero: input.Numero
                );
            }
            else
            {
                // Actualizar borrador existente
                var actual = await _repo.GetByIdAsync(input.Id.Value, ct);
                if (actual is null)
                    throw NotFoundException.For<ComprobanteElectronico>(input.Id);

                var expectedVersion = actual.Version; // captura para control de concurrencia
                var actualizado = await _factory.AplicarAsync(actual, input, ct);
                await _repo.UpdateAsync(actualizado, expectedVersion, ct);
                await _uow.CommitAsync(ct);

                if (_eventBus is not null)
                {
                    var drained = actualizado.DrainDomainEvents();
                    if (drained.Count > 0)
                        await _eventBus.PublishAsync(drained, ct);
                }

                var id = TryGetId(actualizado, out var updId) ? updId : input.Id.Value;
                return new GuardarBorradorOutputDto(
                    id,
                    esNuevo: false,
                    serie: input.Serie,
                    numero: input.Numero
                );
            }
        }

        /// <summary>
        /// Intenta leer la propiedad pública 'Id' (Guid) del agregado sin
        /// acoplarse a su implementación concreta.
        /// </summary>
        private static bool TryGetId(ComprobanteElectronico agg, out Guid id)
        {
            id = Guid.Empty;
            var prop = agg.GetType().GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
            if (prop is null || prop.PropertyType != typeof(Guid)) return false;
            var val = prop.GetValue(agg);
            if (val is Guid g) { id = g; return true; }
            return false;
        }
    }
}
