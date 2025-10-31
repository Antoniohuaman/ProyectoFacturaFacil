using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.Filters;
using CatalogoArticulosBC.Domain.Repositories;
using CatalogoArticulosBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;
using SharedKernel.Application.Interfaces;

namespace CatalogoArticulosBC.Application.UseCases.ListarProductos
{
    public interface IListarProductosUseCase
    {
        Task<ListarProductosOutputDto> ExecuteAsync(ListarProductosInputDto input, CancellationToken ct = default);
    }

    /// <summary>
    /// Lista productos con filtros, ordenamiento y paginación.
    /// </summary>
    public sealed class ListarProductosUseCase : IListarProductosUseCase
    {
        private readonly IProductoRepository _repo;
        private readonly ITenantContext _tenant;

        public ListarProductosUseCase(IProductoRepository repo, ITenantContext tenant)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        }

        public async Task<ListarProductosOutputDto> ExecuteAsync(ListarProductosInputDto input, CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));

            // Normalización de paginación
            var page = input.Page < 1 ? 1 : input.Page;
            var size = input.PageSize < 1 ? 20 : input.PageSize;
            if (size > 200) size = 200;

            // Mapear a filtro de dominio
            var filtro = new FiltroProducto
            {
                EmpresaId = _tenant.EmpresaId,
                Nombre = string.IsNullOrWhiteSpace(input.Nombre) ? null : input.Nombre!.Trim(),
                CategoriaId = string.IsNullOrWhiteSpace(input.CategoriaId) ? null : CategoriaId.FromString(input.CategoriaId!),
                Habilitado = input.Habilitado,
                PrecioMin = input.PrecioMin,
                PrecioMax = input.PrecioMax
            };

            // Obtener desde repositorio (filtrado básico en origen)
            var productos = await _repo.BuscarPorFiltroAsync(filtro) ?? new List<ProductoSimple>();

            // Ordenamiento en aplicación
            bool desc = string.Equals(input.Direccion, "desc", StringComparison.OrdinalIgnoreCase);
            var ordenarPor = (input.OrdenarPor ?? "nombre").Trim().ToLowerInvariant();

            IOrderedEnumerable<ProductoSimple> ordered = ordenarPor switch
            {
                "sku" => desc
                    ? productos.OrderByDescending(p => p.Sku?.Valor ?? string.Empty)
                    : productos.OrderBy(p => p.Sku?.Valor ?? string.Empty),
                "categoria" => desc
                    ? productos.OrderByDescending(p => p.CategoriaNombreSnapshot ?? string.Empty)
                    : productos.OrderBy(p => p.CategoriaNombreSnapshot ?? string.Empty),
                "habilitado" => desc
                    ? productos.OrderByDescending(p => p.Habilitado)
                    : productos.OrderBy(p => p.Habilitado),
                // default: nombre
                _ => desc
                    ? productos.OrderByDescending(p => p.Nombre?.Valor ?? string.Empty)
                    : productos.OrderBy(p => p.Nombre?.Valor ?? string.Empty),
            };

            var total = ordered.Count();

            // Paginación
            var skip = (page - 1) * size;
            var pageItems = ordered.Skip(skip).Take(size).ToList();

            // Proyección a DTO
            var items = pageItems.Select(p => new ListarProductosOutputDto.Item
            {
                ProductoId = p.ProductoId,
                Habilitado = p.Habilitado,
                Sku = p.Sku?.Valor ?? string.Empty,
                Nombre = p.Nombre?.Valor ?? string.Empty,
                CategoriaId = p.CategoriaId?.ToString(),
                CategoriaNombre = p.CategoriaNombreSnapshot,
                CategoriaColor = p.CategoriaColorSnapshot,
                Marca = p.Marca?.Nombre ?? p.Marca?.ToString() ?? string.Empty,
                PrecioVenta = p.PrecioVenta?.Monto,
                Moneda = p.Moneda?.ToString() ?? string.Empty,
                PrecioCompraMonto = p.PrecioCompra?.Monto,
                PrecioCompraMoneda = p.PrecioCompra?.Moneda?.Codigo,
                PorcentajeGanancia = p.PorcentajeGanancia?.Valor,
                Alias = p.Alias?.Valor,
                TipoProducto = p.Tipo.ToString(),
                TipoExistencia = p.TipoExistencia.ToString(),
                ImagenPrincipalId = p.ImagenPrincipalId
            }).ToArray();

            var result = new ListarProductosOutputDto
            {
                EmpresaId = _tenant.EmpresaId.Value,
                Page = page,
                PageSize = size,
                TotalItems = total,
                TotalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)size),
                OrdenarPor = ordenarPor,
                Direccion = desc ? "desc" : "asc",
                Items = items
            };

            return result;
        }
    }
}
