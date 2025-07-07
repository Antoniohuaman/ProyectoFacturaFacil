// src/CatalogoArticulosBC/Application/UseCases/CrearProductoComboUseCase.cs
using System;
using System.Linq;
using System.Threading.Tasks;
using CatalogoArticulosBC.Application.DTOs;
using CatalogoArticulosBC.Application.Interfaces;
using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.ValueObjects;
using CatalogoArticulosBC.Domain.Entities;

namespace CatalogoArticulosBC.Application.UseCases
{
    /// <summary>
    /// Define un ProductoCombo agrupando varios productos existentes.
    /// </summary>
    public class CrearProductoComboUseCase
    {
        private readonly ICatalogoArticulosRepository _repo;
        private readonly IUnitOfWork _uow;

        public CrearProductoComboUseCase(
            ICatalogoArticulosRepository repo,
            IUnitOfWork uow)
        {
            _repo = repo;
            _uow  = uow;
        }

        public async Task<Guid> HandleAsync(CrearProductoComboDto dto)
        {
            // Validar que cada componente exista y esté activo
            var componentes = await Task.WhenAll(
                dto.Componentes.Select(async c =>
                {
                    var p = await _repo.GetByIdAsync(c.productoId)
                            ?? throw new InvalidOperationException($"Producto {c.productoId} no existe.");
                    if (!p.Activo) throw new InvalidOperationException($"Componente {c.productoId} inactivo.");  // :contentReference[oaicite:2]{index=2}
                    return new ComponenteCombo(p.ProductoId, c.cantidad);
                }));

            // Construir combo
            var combo = new ProductoCombo(
                dto.SkuCombo,
                dto.NombreCombo,
                componentes.ToList(),
                dto.PrecioCombo,
                new UnidadMedida(dto.UnidadMedida),
                new AfectacionIGV(dto.AfectacionIgv));

            // Persistir
            await _repo.AddProductoComboAsync(combo);
            await _uow.CommitAsync();

            return combo.ProductoComboId;
        }
    }
}
