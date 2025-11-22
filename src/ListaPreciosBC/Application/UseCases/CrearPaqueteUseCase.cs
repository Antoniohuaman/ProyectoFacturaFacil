using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.DTOs;
using ListaPreciosBC.Domain.Aggregates;
using ListaPreciosBC.Domain.Repositories;
using ListaPreciosBC.Application.Interfaces;
using ListaPreciosBC.Domain.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace ListaPreciosBC.Application.UseCases
{
    public sealed class CrearPaqueteUseCase
    {
        private readonly IProductoPaqueteRepository _paqueteRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CrearPaqueteUseCase(
            IProductoPaqueteRepository paqueteRepository,
            IUnitOfWork unitOfWork)
        {
            _paqueteRepository = paqueteRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> EjecutarAsync(
            EmpresaId empresaId,
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

            var nombre = NombrePaquete.Crear(comando.Nombre);
            var descuento = PorcentajeDescuentoPaquete.Crear(comando.DescuentoPorcentaje);

            var paqueteId = Guid.NewGuid();

            var paquete = ProductoPaquete.Crear(
                empresaId,
                paqueteId,
                nombre,
                descuento,
                comando.Descripcion,
                comando.Productos,
                fechaCreacionUtc: null);

            await _paqueteRepository.GuardarAsync(paquete, cancellationToken)
                .ConfigureAwait(false);

            await _unitOfWork.CommitAsync(cancellationToken)
                .ConfigureAwait(false);

            return paquete.Id;
        }
    }
}
