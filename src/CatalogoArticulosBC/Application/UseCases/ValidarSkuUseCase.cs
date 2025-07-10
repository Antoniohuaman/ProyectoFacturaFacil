// src/CatalogoArticulosBC/Application/UseCases/ValidarSkuUseCase.cs
using System.Threading.Tasks;
using CatalogoArticulosBC.Application.DTOs;
using CatalogoArticulosBC.Application.Interfaces;
using CatalogoArticulosBC.Domain.ValueObjects;
using CatalogoArticulosBC.Domain.Entities;

namespace CatalogoArticulosBC.Application.UseCases
{
    /// <summary>
    /// Valida la existencia y estado de un SKU, retornando sus datos clave.
    /// </summary>
    public class ValidarSkuUseCase
    {
        private readonly ICatalogoArticulosRepository _repo;

        public ValidarSkuUseCase(ICatalogoArticulosRepository repo)
            => _repo = repo;

        public async Task<ProductoDetalleDto> HandleAsync(string sku)
        {
            var item = await _repo.GetBySkuAsync(sku)
                       ?? throw new KeyNotFoundException($"SKU {sku} inválido.");  // :contentReference[oaicite:8]{index=8}

            return new ProductoDetalleDto(
                item.ProductoId,
                item.Sku.Value,
                item.Nombre,
                item.Descripcion,
                item.Tipo.ToString(),
                item.Peso.Valor);
        }
    }
}
