using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.DTOs;
using ListaPreciosBC.Domain.Repositories;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;
using System;

namespace ListaPreciosBC.Application.UseCases
{
    public sealed class ObtenerPaqueteDetalleUseCase
    {
        private readonly IProductoPaqueteRepository _paqueteRepository;

        public ObtenerPaqueteDetalleUseCase(IProductoPaqueteRepository paqueteRepository)
        {
            _paqueteRepository = paqueteRepository;
        }

        public async Task<PaqueteDetalleDto> EjecutarAsync(
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

            return new PaqueteDetalleDto
            {
                PaqueteId = paquete.Id,
                Nombre = paquete.Nombre.Valor,
                Descripcion = paquete.Descripcion,
                DescuentoPorcentaje = paquete.Descuento.Valor,
                FechaCreacionUtc = paquete.FechaCreacionUtc,
                Productos = paquete.Productos
            };
        }
    }
}
