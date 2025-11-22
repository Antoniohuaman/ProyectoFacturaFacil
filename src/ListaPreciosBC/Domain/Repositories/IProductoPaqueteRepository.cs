using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Domain.Aggregates;
using SharedKernel.ValueObjects;

namespace ListaPreciosBC.Domain.Repositories
{
    public interface IProductoPaqueteRepository
    {
        Task<ProductoPaquete?> ObtenerPorIdAsync(
            EmpresaId empresaId,
            Guid paqueteId,
            CancellationToken cancellationToken);

        Task<IReadOnlyList<ProductoPaquete>> ListarPorEmpresaAsync(
            EmpresaId empresaId,
            CancellationToken cancellationToken);

        Task GuardarAsync(
            ProductoPaquete paquete,
            CancellationToken cancellationToken);

        Task EliminarAsync(
            ProductoPaquete paquete,
            CancellationToken cancellationToken);
    }
}
