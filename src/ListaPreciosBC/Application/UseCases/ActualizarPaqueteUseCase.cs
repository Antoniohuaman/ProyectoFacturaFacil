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
    public sealed class ActualizarPaqueteUseCase
    {
        private readonly IProductoPaqueteRepository _paqueteRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantContext _tenant;
        private readonly IEventBus? _eventBus;

        public ActualizarPaqueteUseCase(
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
            ActualizarPaqueteDto comando,
            CancellationToken cancellationToken)
        {
            if (comando is null)
            {
                throw new ArgumentNullException(nameof(comando));
            }

            var empresaId = ObtenerEmpresaId();

            var paquete = await _paqueteRepository.ObtenerPorIdAsync(
                    empresaId,
                    comando.PaqueteId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (paquete is null)
            {
                throw new NotFoundException(
                    $"No se encontró el paquete con Id {comando.PaqueteId} para la empresa {empresaId}.");
            }

            if (comando.Productos is null || !comando.Productos.Any())
            {
                throw new BusinessRuleException(
                    "Un paquete debe contener al menos un producto.");
            }

            var nuevoNombre = NombrePaquete.Crear(comando.Nombre);
            var nuevoDescuento = PorcentajeDescuentoPaquete.Crear(comando.DescuentoPorcentaje);
            var nuevasLineas = PaqueteApplicationMapper.ConvertirLineas(comando.Productos);

            paquete.ActualizarDatos(
                nuevoNombre,
                comando.Descripcion,
                nuevoDescuento,
                nuevasLineas,
                DateTime.UtcNow);

            await _paqueteRepository.GuardarAsync(paquete, cancellationToken)
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
