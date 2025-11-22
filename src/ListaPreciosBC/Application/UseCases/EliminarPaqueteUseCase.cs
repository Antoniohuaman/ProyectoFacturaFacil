using System;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.Interfaces;
using ListaPreciosBC.Domain.Aggregates;
using ListaPreciosBC.Domain.Repositories;
using SharedKernel.Application.Interfaces;
using SharedKernel.Events;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace ListaPreciosBC.Application.UseCases
{
    public sealed class EliminarPaqueteUseCase
    {
        private readonly IProductoPaqueteRepository _paqueteRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantContext _tenant;
        private readonly IEventBus? _eventBus;

        public EliminarPaqueteUseCase(
            IProductoPaqueteRepository paqueteRepository,
            IUnitOfWork unitOfWork,
            ITenantContext tenant,
            IEventBus? eventBus = null)
        {
            _paqueteRepository = paqueteRepository ?? throw new ArgumentNullException(nameof(paqueteRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
            _eventBus = eventBus;
        }

        public async Task EjecutarAsync(
            Guid paqueteId,
            CancellationToken cancellationToken)
        {
            var empresaId = ObtenerEmpresaId();

            var paquete = await _paqueteRepository.ObtenerPorIdAsync(
                    empresaId,
                    paqueteId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (paquete is null)
            {
                throw new NotFoundException(
                    $"No se encontró el paquete con Id {paqueteId} para la empresa {empresaId}.");
            }

            paquete.MarcarComoEliminado(DateTime.UtcNow);

            await _paqueteRepository.EliminarAsync(paquete, cancellationToken)
                .ConfigureAwait(false);

            await _unitOfWork.CommitAsync(cancellationToken)
                .ConfigureAwait(false);

            await PublicarEventosAsync(paquete, cancellationToken)
                .ConfigureAwait(false);
        }

        private EmpresaId ObtenerEmpresaId()
        {
            return _tenant.EmpresaId
                ?? throw new InvalidOperationException("El contexto de tenant no proporciona EmpresaId.");
        }

        private Task PublicarEventosAsync(ProductoPaquete paquete, CancellationToken cancellationToken)
        {
            if (_eventBus is null || paquete.DomainEvents.Count == 0)
            {
                return Task.CompletedTask;
            }

            return _eventBus.PublishAsync(paquete.DomainEvents, cancellationToken);
        }
    }
}
