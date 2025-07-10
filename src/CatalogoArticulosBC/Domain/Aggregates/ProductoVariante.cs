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
        public SKU Sku { get; private set; }
        public IReadOnlyCollection<AtributoVariante> Atributos { get; private set; }
        public bool Activo { get; private set; } = true;

        public ProductoVariante(Guid padreId, string sku, IEnumerable<AtributoVariante> atributos)
        {
            ProductoVarianteId = Guid.NewGuid();
            ProductoPadreId = padreId;
            Sku = new SKU(sku);
            Atributos = new List<AtributoVariante>(atributos);

            var ev = new ProductoCreado(ProductoVarianteId, Sku);
            // Dispatch(ev);
        }

        public void Deshabilitar(string motivo)
        {
            Activo = false;
            var ev = new ProductoInhabilitado(ProductoVarianteId, motivo);
            // Dispatch(ev);
        }
        // En ProductoVariante.cs
      public void Editar(string nuevoSku, IEnumerable<KeyValuePair<string, string>> nuevosAtributos)
{
    if (string.IsNullOrWhiteSpace(nuevoSku))
        throw new ArgumentException("El SKU no puede estar vacío", nameof(nuevoSku));

    if (nuevosAtributos == null)
        throw new ArgumentNullException(nameof(nuevosAtributos));

    Sku = new SKU(nuevoSku);

    var atributosList = new List<AtributoVariante>();
    foreach (var kv in nuevosAtributos)
    {
        atributosList.Add(new AtributoVariante(kv.Key, kv.Value));
    }
    Atributos = atributosList;

    // Puedes lanzar un evento de dominio si lo necesitas
    // var ev = new ProductoVarianteEditado(ProductoVarianteId, Sku, atributosList);
    // Dispatch(ev);

        }
    }
}