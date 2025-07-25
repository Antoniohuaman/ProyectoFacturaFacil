// src/CatalogoArticulosBC/Application/UseCases/ConsultarProductoPorSkuUseCase.cs
using System;
using System.Threading.Tasks;
using CatalogoArticulosBC.Application.DTOs;
using CatalogoArticulosBC.Application.Interfaces;
using CatalogoArticulosBC.Domain.ValueObjects;


namespace CatalogoArticulosBC.Application.UseCases
{
    /// <summary>
    /// Obtiene la ficha detallada de un producto por su SKU.
    /// </summary>
    public class ConsultarProductoPorSkuUseCase
    {
        private readonly ICatalogoArticulosRepository _repo;

        public ConsultarProductoPorSkuUseCase(ICatalogoArticulosRepository repo)
            => _repo = repo;

        public async Task<ProductoDetalleDto> HandleAsync(string sku)
        {
            var item = await _repo.GetBySkuAsync(sku)
                       ?? throw new InvalidOperationException($"SKU {sku} no encontrado.");  // :contentReference[oaicite:6]{index=6}

            return new ProductoDetalleDto(
                item.ProductoId,
                item.Sku.Value,
                item.Nombre,
                item.Descripcion,
                item.Tipo,
                item.Peso?.Valor ?? 0
                );}
    }
}
