using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CatalogoArticulosBC.Domain.Filters;
using CatalogoArticulosBC.Domain.Repositories;
using CatalogoArticulosBC.Domain.ValueObjects;
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;

namespace CatalogoArticulosBC.Application.UseCases.EliminarProductosPorCriterio
{
    public interface IEliminarProductosPorCriterioUseCase
    {
        Task<EliminarProductosPorCriterioOutputDto> ExecuteAsync(
            EliminarProductosPorCriterioInputDto input,
            CancellationToken ct = default);
    }

    /// <summary>
    /// Elimina todos los productos que coincidan con un criterio de búsqueda
    /// (no paginado), limitado al tenant/empresa actual (seguridad se espera en el repo real).
    /// </summary>
    public sealed class EliminarProductosPorCriterioUseCase : IEliminarProductosPorCriterioUseCase
    {
        private readonly IProductoRepository _repo;
        private readonly IUnitOfWork _uow;
        private readonly ITenantContext _tenant;

        public EliminarProductosPorCriterioUseCase(
            IProductoRepository repo,
            IUnitOfWork uow,
            ITenantContext tenant)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        }

        public async Task<EliminarProductosPorCriterioOutputDto> ExecuteAsync(
            EliminarProductosPorCriterioInputDto input,
            CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            if (!input.Confirmar)
                throw new BusinessRuleException("Debe confirmar explícitamente la eliminación por criterio.");
            if (!input.TieneAlMenosUnCriterio())
                throw new BusinessRuleException("Debe especificar al menos un criterio de búsqueda.");
            if (input.PrecioMin.HasValue && input.PrecioMin.Value < 0m)
                throw new BusinessRuleException("Precio mínimo no puede ser negativo.");
            if (input.PrecioMin.HasValue && input.PrecioMax.HasValue && input.PrecioMin > input.PrecioMax)
                throw new BusinessRuleException("Precio mínimo no puede ser mayor que el precio máximo.");

            // Construir filtro del dominio.
            var filtro = new FiltroProducto
            {
                Nombre = input.NombreContiene?.Trim(),
                Categoria = string.IsNullOrWhiteSpace(input.CategoriaNombre) ? null : new Categoria(input.CategoriaNombre!.Trim()),
                Habilitado = input.Habilitado,
                PrecioMin = input.PrecioMin,
                PrecioMax = input.PrecioMax
            };

            // Buscar candidatos a eliminar
            var encontrados = (await _repo.BuscarPorFiltroAsync(filtro)).ToList();

            // Eliminar los encontrados (ignorando si lista vacía: no es error)
            var idsEliminados = new List<Guid>(encontrados.Count);
            foreach (var p in encontrados)
            {
                await _repo.DeleteAsync(p);
                idsEliminados.Add(p.ProductoId);
            }

            await _uow.CommitAsync();

            return new EliminarProductosPorCriterioOutputDto
            {
                EmpresaId = _tenant.EmpresaId.Value,
                Criterio = new EliminarProductosPorCriterioOutputDto.CriterioEcho
                {
                    NombreContiene = input.NombreContiene?.Trim(),
                    CategoriaNombre = input.CategoriaNombre?.Trim(),
                    Habilitado = input.Habilitado,
                    PrecioMin = input.PrecioMin,
                    PrecioMax = input.PrecioMax
                },
                CantidadCoincidente = encontrados.Count,
                CantidadEliminada = idsEliminados.Count,
                IdsEliminados = idsEliminados,
                EjecutadoEnUtc = DateTimeOffset.UtcNow,
                Exitoso = true
            };
        }
    }
}
