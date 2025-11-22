using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.DTOs;
using ListaPreciosBC.Application.Interfaces;
using ListaPreciosBC.Application.Mappers;
using ListaPreciosBC.Domain.Aggregates;
using ListaPreciosBC.Domain.Repositories;
using ListaPreciosBC.Domain.ValueObjects;
using SharedKernel.Application.Interfaces;
using SharedKernel.Events;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace ListaPreciosBC.Application.UseCases
{
    public sealed class CrearPaqueteUseCase
    {
        private readonly IProductoPaqueteRepository _paqueteRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantContext _tenant;
        private readonly IEventBus? _eventBus;

        public CrearPaqueteUseCase(
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

        public async Task<Guid> EjecutarAsync(
            CrearPaqueteDto comando,
            CancellationToken cancellationToken)
        {
            if (comando is null)
            {
                throw new ArgumentNullException(nameof(comando));
            }

            if (comando.Productos is null || !comando.Productos.Any())
            {
                throw new BusinessRuleException(
                    "Un paquete debe contener al menos un producto.");
            }

            var empresaId = ObtenerEmpresaId();
            var nombre = NombrePaquete.Crear(comando.Nombre);
            var descuento = PorcentajeDescuentoPaquete.Crear(comando.DescuentoPorcentaje);
            var lineas = PaqueteApplicationMapper.ConvertirLineas(comando.Productos);

            var paqueteId = Guid.NewGuid();

            var paquete = ProductoPaquete.Crear(
                empresaId,
                paqueteId,
                nombre,
                descuento,
                comando.Descripcion,
                lineas,
                fechaCreacionUtc: null);

            await _paqueteRepository.GuardarAsync(paquete, cancellationToken)
                .ConfigureAwait(false);

            await _unitOfWork.CommitAsync(cancellationToken)
                .ConfigureAwait(false);

            await PublicarEventosAsync(paquete, cancellationToken)
                .ConfigureAwait(false);

            return paquete.Id;
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
