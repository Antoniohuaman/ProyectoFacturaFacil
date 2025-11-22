using System;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Domain.Repositories;
using ListaPreciosBC.Application.Interfaces;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace ListaPreciosBC.Application.UseCases
{
    public sealed class EliminarPaqueteUseCase
    {
        private readonly IProductoPaqueteRepository _paqueteRepository;
        private readonly IUnitOfWork _unitOfWork;

        public EliminarPaqueteUseCase(
            IProductoPaqueteRepository paqueteRepository,
            IUnitOfWork unitOfWork)
        {
            _paqueteRepository = paqueteRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task EjecutarAsync(
            EmpresaId empresaId,
            Guid paqueteId,
            CancellationToken cancellationToken)
        {
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

            await _paqueteRepository.EliminarAsync(paquete, cancellationToken)
                .ConfigureAwait(false);

            await _unitOfWork.CommitAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
