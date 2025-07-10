// src/CatalogoArticulosBC/Application/UseCases/ListarProductosUseCase.cs
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CatalogoArticulosBC.Application.DTOs;
using CatalogoArticulosBC.Application.Interfaces;
using CatalogoArticulosBC.Domain.Entities;

namespace CatalogoArticulosBC.Application.UseCases
{
    /// <summary>
    /// Devuelve una lista paginada de productos según filtros.
    /// </summary>
    public class ListarProductosUseCase
    {
        private readonly ICatalogoArticulosRepository _repo;

        public ListarProductosUseCase(ICatalogoArticulosRepository repo)
            => _repo = repo;

        public async Task<IReadOnlyCollection<ProductoDetalleDto>> HandleAsync(
            int pagina, int tamano, string filtrosJson)
        {
            var items = await _repo.ListarAsync(pagina, tamano, filtrosJson);
            return items
                .Select(item => new ProductoDetalleDto(
                    item.ProductoId,
                    item.Sku.Value,
                    item.Nombre,
                    item.Descripcion,
                    item.Tipo.ToString(),
                    item.Peso.Valor))
                .ToList()
                .AsReadOnly();  // :contentReference[oaicite:7]{index=7}
        }
    }
}
