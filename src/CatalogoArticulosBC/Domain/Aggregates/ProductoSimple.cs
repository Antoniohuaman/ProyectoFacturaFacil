// src/CatalogoArticulosBC/Domain/Aggregates/ProductoSimple.cs
using System;
using System.Collections.Generic;
using CatalogoArticulosBC.Domain.Events;
using CatalogoArticulosBC.Domain.ValueObjects;

namespace CatalogoArticulosBC.Domain.Aggregates
{
    public class ProductoSimple
    {
        private readonly List<MultimediaProducto> _multimedia = new();

        public Guid ProductoId { get; }
        public SKU Sku { get; }
        public string Nombre { get; private set; }
        public string Descripcion { get; private set; }
        public UnidadMedida UnidadMedida { get; }
        public AfectacionIGV AfectacionIgv { get; }
        public CodigoSUNAT CodigoSunat { get; }
        public CuentaContable CuentaContable { get; }
        public CentroCosto CentroCosto { get; }
        public Presupuesto Presupuesto { get; }
        public Peso Peso { get; private set; }
        public bool Activo { get; private set; } = true;
        public IReadOnlyCollection<MultimediaProducto> Multimedia => _multimedia.AsReadOnly();

        public ProductoSimple(
            string sku,
            string nombre,
            string descripcion,
            UnidadMedida unidadMedida,
            AfectacionIGV afectacionIgv,
            CodigoSUNAT codigoSunat,
            CuentaContable cuentaContable,
            CentroCosto centroCosto,
            Presupuesto presupuesto,
            Peso peso)
        {
            if (string.IsNullOrWhiteSpace(sku)) throw new ArgumentException("SKU no puede estar vacío.", nameof(sku));
            ProductoId    = Guid.NewGuid();
            Sku           = new SKU(sku);
            Nombre        = nombre ?? throw new ArgumentNullException(nameof(nombre));
            Descripcion   = descripcion ?? string.Empty;
            UnidadMedida  = unidadMedida;
            AfectacionIgv = afectacionIgv;
            CodigoSunat   = codigoSunat;
            CuentaContable= cuentaContable;
            CentroCosto   = centroCosto;
            Presupuesto   = presupuesto;
            Peso          = peso;

            var ev = new ProductoCreado(ProductoId, Sku);
            // Dispatch(ev);
        }

        public ProductoVariante CrearVariante(string skuVariante, IEnumerable<AtributoVariante> atributos)
        {
            if (!Activo) throw new InvalidOperationException("No se puede crear variante de producto inactivo.");
            var variante = new ProductoVariante(ProductoId, skuVariante, atributos);
            // Dispatch(...)
            return variante;
        }

        public ProductoCombo CrearCombo(string skuCombo, string nombreCombo, IEnumerable<ComponenteCombo> componentes, decimal precio, UnidadMedida unidad, AfectacionIGV igv)
        {
            if (!Activo) throw new InvalidOperationException("No se puede crear combo de producto inactivo.");
            var combo = new ProductoCombo(skuCombo, nombreCombo, componentes, precio, unidad, igv);
            // Dispatch(...)
            return combo;
        }

        public void ActualizarDescripcion(string descripcion)
        {
            Descripcion = descripcion ?? throw new ArgumentNullException(nameof(descripcion));
            var ev = new ProductoModificado(ProductoId);
            // Dispatch(ev);
        }

        public void ActualizarPeso(Peso peso)
        {
            Peso = peso;
            var ev = new ProductoModificado(ProductoId);
            // Dispatch(ev);
        }

        public void Deshabilitar(string motivo)
        {
            Activo = false;
            var ev = new ProductoInhabilitado(ProductoId, motivo);
            // Dispatch(ev);
        }

        public void AgregarMultimedia(Guid multimediaId, string tipo, byte[] contenido)
        {
            var media = new MultimediaProducto(multimediaId, ProductoId, tipo, contenido);
            _multimedia.Add(media);
            // Dispatch(...)
        }

        public void EliminarMultimedia(Guid multimediaId)
        {
            var media = _multimedia.Find(m => m.MultimediaId == multimediaId);
            if (media == null) throw new InvalidOperationException("Multimedia no encontrada.");
            _multimedia.Remove(media);
            // Dispatch(...)
        }
    }
}