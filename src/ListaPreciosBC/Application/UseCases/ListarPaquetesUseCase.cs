using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.DTOs;
using ListaPreciosBC.Domain.Repositories;
using SharedKernel.ValueObjects;

namespace ListaPreciosBC.Application.UseCases
{
    public sealed class ListarPaquetesUseCase
    {
        private readonly IProductoPaqueteRepository _paqueteRepository;

        public ListarPaquetesUseCase(IProductoPaqueteRepository paqueteRepository)
        {
            _paqueteRepository = paqueteRepository;
        }

        public async Task<IReadOnlyList<PaqueteResumenDto>> EjecutarAsync(
            EmpresaId empresaId,
            CancellationToken cancellationToken)
        {
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
    }
}
