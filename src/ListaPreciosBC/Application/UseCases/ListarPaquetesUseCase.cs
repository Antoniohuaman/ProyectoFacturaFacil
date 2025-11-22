using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.DTOs;
using ListaPreciosBC.Domain.Repositories;
using SharedKernel.Application.Interfaces;
using SharedKernel.ValueObjects;

namespace ListaPreciosBC.Application.UseCases
{
    public sealed class ListarPaquetesUseCase
    {
        private readonly IProductoPaqueteRepository _paqueteRepository;
        private readonly ITenantContext _tenant;

        public ListarPaquetesUseCase(IProductoPaqueteRepository paqueteRepository, ITenantContext tenant)
        {
            _paqueteRepository = paqueteRepository ?? throw new ArgumentNullException(nameof(paqueteRepository));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        }

        public async Task<IReadOnlyList<PaqueteResumenDto>> EjecutarAsync(CancellationToken cancellationToken)
        {
            var empresaId = ObtenerEmpresaId();
            var paquetes = await _paqueteRepository.ListarPorEmpresaAsync(
                    empresaId,
                    cancellationToken)
                .ConfigureAwait(false);

            var resultados = paquetes
                .Select(p => new PaqueteResumenDto
                {
                    PaqueteId = p.Id,
                    Nombre = p.Nombre.Valor,
                    Descripcion = p.Descripcion,
                    DescuentoPorcentaje = p.Descuento.Valor,
                    Subtotal = p.Subtotal,
                    Total = p.Total,
                    FechaCreacionUtc = p.FechaCreacionUtc
                })
                .ToList();

            return resultados;
        }

        private EmpresaId ObtenerEmpresaId()
        {
            return _tenant.EmpresaId
                ?? throw new InvalidOperationException("El contexto de tenant no proporciona EmpresaId.");
        }
    }
}
