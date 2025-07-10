using System;
using System.Collections.Generic;
using CatalogoArticulosBC.Domain.ValueObjects;
using CatalogoArticulosBC.Domain.Entities;
using CatalogoArticulosBC.Domain.Events;

namespace CatalogoArticulosBC.Domain.Aggregates
{
    public class ProductoCombo : IProductoConPeso
    {
        public Guid ProductoComboId { get; }
        public SKU Sku { get; }
        public string Nombre { get; private set; }
        public IReadOnlyCollection<ComponenteCombo> Componentes { get; }
        public decimal Precio { get; private set; }
        public UnidadMedida UnidadMedida { get; }
        public AfectacionIGV AfectacionIgv { get; }
        public decimal PesoTotal { get; private set; }
        public string Estado { get; private set; }
        public bool Activo { get; private set; } = true;

        // Implementación explícita de la interfaz
        decimal IProductoConPeso.Peso => PesoTotal;

        public ProductoCombo(
            string sku,
            string nombre,
            IEnumerable<ComponenteCombo> componentes,
            decimal precio,
            UnidadMedida unidad,
            AfectacionIGV igv,
            decimal pesoTotal,
            string estado)
        {
            ProductoComboId = Guid.NewGuid();
            Sku             = new SKU(sku);
            Nombre          = nombre;
            Componentes     = new List<ComponenteCombo>(componentes);
            Precio          = precio;
            UnidadMedida    = unidad;
            AfectacionIgv   = igv;
            PesoTotal       = pesoTotal;
            Estado          = estado;

            var ev = new ProductoCreado(ProductoComboId, Sku);
            // Dispatch(ev);
        }

        public void Deshabilitar(string motivo)
        {
            Activo = false;
            var ev = new ProductoInhabilitado(ProductoComboId, motivo);
            // Dispatch(ev);
        }
    }
}