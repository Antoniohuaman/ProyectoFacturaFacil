using System;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.DTOs;
using ListaPreciosBC.Application.Mappers;
using ListaPreciosBC.Domain.Repositories;
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace ListaPreciosBC.Application.UseCases
{
    public sealed class ObtenerPaqueteDetalleUseCase
    {
        private readonly IProductoPaqueteRepository _paqueteRepository;
        private readonly ITenantContext _tenant;

        public ObtenerPaqueteDetalleUseCase(IProductoPaqueteRepository paqueteRepository, ITenantContext tenant)
        {
            _paqueteRepository = paqueteRepository ?? throw new ArgumentNullException(nameof(paqueteRepository));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        }

        public async Task<PaqueteDetalleDto> EjecutarAsync(
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

            return new PaqueteDetalleDto
            {
                PaqueteId = paquete.Id,
                Nombre = paquete.Nombre.Valor,
                Descripcion = paquete.Descripcion,
                DescuentoPorcentaje = paquete.Descuento.Valor,
                FechaCreacionUtc = paquete.FechaCreacionUtc,
                Productos = PaqueteApplicationMapper.ConvertirLineasDto(paquete.Productos)
            };
        }

        private EmpresaId ObtenerEmpresaId()
        {
            return _tenant.EmpresaId
                ?? throw new InvalidOperationException("El contexto de tenant no proporciona EmpresaId.");
        }
    }
}
