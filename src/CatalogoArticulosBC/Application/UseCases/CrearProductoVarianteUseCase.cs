// src/CatalogoArticulosBC/Application/UseCases/CrearProductoVarianteUseCase.cs
using System;
using System.Threading.Tasks;
using CatalogoArticulosBC.Application.DTOs;
using CatalogoArticulosBC.Application.Interfaces;
using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.ValueObjects;
using CatalogoArticulosBC.Domain.Entities;

namespace CatalogoArticulosBC.Application.UseCases
{
    /// <summary>
    /// Genera un ProductoVariante a partir de un padre existente.
    /// </summary>
    public class CrearProductoVarianteUseCase
    {
        private readonly ICatalogoArticulosRepository _repo;
        private readonly IUnitOfWork _uow;

        public CrearProductoVarianteUseCase(
            ICatalogoArticulosRepository repo,
            IUnitOfWork uow)
        {
            _repo = repo;
            _uow  = uow;
        }

        public async Task<Guid> HandleAsync(CrearProductoVarianteDto dto)
        {
            // 1. Obtener padre y validar estado
            var padre = await _repo.GetProductoSimpleBySkuAsync(new SKU(dto.SkuVariante.Substring(0, dto.SkuVariante.IndexOf('-'))));
            if (padre is null)
                throw new InvalidOperationException("Producto padre no existe.");           // :contentReference[oaicite:1]{index=1}
            if (!padre.Activo)
                throw new InvalidOperationException("Producto padre está inactivo.");     // RN-CA-005

            // 2. Crear y persistir
            var variante = padre.CrearVariante(
                dto.SkuVariante,
                dto.Atributos
                    .Select(kv => new AtributoVariante(kv.Key, kv.Value))
                    .ToList());

            await _repo.AddProductoVarianteAsync(variante);
            await _uow.CommitAsync();

            return variante.ProductoVarianteId;
        }
    }
}
