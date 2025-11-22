using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.DTOs;
using ListaPreciosBC.Domain.Repositories;
using ListaPreciosBC.Domain.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;
using ListaPreciosBC.Application.Interfaces;
namespace ListaPreciosBC.Application.UseCases
{
    public sealed class ActualizarPaqueteUseCase
    {
        private readonly IProductoPaqueteRepository _paqueteRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ActualizarPaqueteUseCase(
            IProductoPaqueteRepository paqueteRepository,
            IUnitOfWork unitOfWork)
        {
            _paqueteRepository = paqueteRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task EjecutarAsync(
            EmpresaId empresaId,
            ActualizarPaqueteDto comando,
            CancellationToken cancellationToken)
        {
            if (comando is null)
            {
                throw new ArgumentNullException(nameof(comando));
            }

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

            paquete.ActualizarDatos(
                nuevoNombre,
                comando.Descripcion,
                nuevoDescuento,
                comando.Productos,
                DateTime.UtcNow);

            await _paqueteRepository.GuardarAsync(paquete, cancellationToken)
                .ConfigureAwait(false);

            await _unitOfWork.CommitAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
