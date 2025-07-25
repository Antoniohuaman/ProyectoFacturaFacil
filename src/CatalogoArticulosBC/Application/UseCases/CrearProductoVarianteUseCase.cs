using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using CatalogoArticulosBC.Application.DTOs;
using CatalogoArticulosBC.Application.Interfaces;
using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.ValueObjects;
using CatalogoArticulosBC.Domain.Entities;
using CatalogoArticulosBC.Domain.Events;

namespace CatalogoArticulosBC.Application.UseCases
{
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
            // 1. Obtener padre por ID y validar estado
            var padre = await _repo.GetProductoSimpleByIdAsync(dto.ProductoPadreId);
            if (padre is null)
                throw new InvalidOperationException("Producto padre no existe.");
            if (!padre.Activo)
                throw new InvalidStateException("Producto padre está inactivo.");

            // 2. Validar variante duplicada (misma combinación de atributos)
            var variantes = await _repo.GetVariantesByPadreIdAsync(dto.ProductoPadreId);
            bool existeDuplicada = variantes.Any(v =>
                v.Atributos.Count == dto.Atributos.Count &&
                !v.Atributos.Except(dto.Atributos.Select(a => new AtributoVariante(a.Key, a.Value)), new AtributoVarianteComparer()).Any()
            );
            if (existeDuplicada)
                throw new VarianteDuplicadaException("Ya existe una variante con los mismos atributos para este producto.");

            // 3. Crear y persistir
            var variante = padre.CrearVariante(
                dto.SkuVariante,
                dto.Atributos.Select(kv => new AtributoVariante(kv.Key, kv.Value)).ToList());

            await _repo.AddProductoVarianteAsync(variante);
            await _uow.CommitAsync();

            // 4. Publicar evento ProductoCreado (tipoProducto=VARIANTE)
            var ev = new ProductoCreado(variante.ProductoVarianteId, variante.Sku, "VARIANTE");
            // TODO: Dispatch(ev); // Implementa el dispatch según tu infraestructura

            return variante.ProductoVarianteId;
        }
    }

    // Comparador para comparar atributos de variante
    public class AtributoVarianteComparer : IEqualityComparer<AtributoVariante>
    {
        public bool Equals(AtributoVariante? x, AtributoVariante? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;
            return x.Nombre == y.Nombre && x.Valor == y.Valor;
        }

        public int GetHashCode(AtributoVariante obj)
        {
            return HashCode.Combine(obj.Nombre, obj.Valor);
        }
    }

    public class VarianteDuplicadaException : Exception
    {
        public VarianteDuplicadaException(string message) : base(message) { }
    }

    public class InvalidStateException : Exception
    {
        public InvalidStateException(string message) : base(message) { }
    }
}