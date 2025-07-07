// src/CatalogoArticulosBC/Domain/Aggregates/ProductoVariante.cs
using System;
using System.Collections.Generic;
using CatalogoArticulosBC.Domain.ValueObjects;
using CatalogoArticulosBC.Domain.Entities;
using CatalogoArticulosBC.Domain.Events;

namespace CatalogoArticulosBC.Domain.Aggregates
{
    public class ProductoVariante
    {
        public Guid ProductoVarianteId { get; }
        public Guid ProductoPadreId { get; }
        public SKU Sku { get; }
        public IReadOnlyCollection<AtributoVariante> Atributos { get; }
        public bool Activo { get; private set; } = true;

        public ProductoVariante(Guid padreId, string sku, IEnumerable<AtributoVariante> atributos)
        {
            ProductoVarianteId = Guid.NewGuid();
            ProductoPadreId    = padreId;
            Sku                = new SKU(sku);
            Atributos          = new List<AtributoVariante>(atributos);

            var ev = new ProductoCreado(ProductoVarianteId, Sku);
            // Dispatch(ev);
        }

        public void Deshabilitar(string motivo)
        {
            Activo = false;
            var ev = new ProductoInhabilitado(ProductoVarianteId, motivo);
            // Dispatch(ev);
        }
    }
}