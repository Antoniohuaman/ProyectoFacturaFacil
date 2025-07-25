using System;
using System.Collections.Generic;
using CatalogoArticulosBC.Domain.Events;
using CatalogoArticulosBC.Domain.ValueObjects;
using CatalogoArticulosBC.Domain.Entities;

namespace CatalogoArticulosBC.Domain.Aggregates
{
    public enum TipoProducto
    {
        Bien,
        Servicio
    }

    public class ProductoSimple : IProductoConPeso
    {
        private readonly List<MultimediaProducto> _multimedia = new();

        public Guid ProductoId { get; }
        public SKU Sku { get; }
        public string Nombre { get; private set; }
        public string Descripcion { get; private set; }
        public UnidadMedida UnidadMedida { get; }
        public AfectacionIGV AfectacionIgv { get; }
        public CodigoSUNAT? CodigoSunat { get; }
        public BaseImponibleVentas? BaseImponibleVentas { get; }
        public CentroCosto? CentroCosto { get; }
        public Presupuesto? Presupuesto { get; }
        public Peso? Peso { get; private set; }
        public bool Activo { get; private set; } = true;
        public TipoProducto Tipo { get; private set; }
        public decimal Precio { get; private set; }
        public IReadOnlyCollection<MultimediaProducto> Multimedia => _multimedia.AsReadOnly();

        // Implementación explícita de la interfaz
        decimal IProductoConPeso.Peso => Peso?.Valor ?? 0;

        public ProductoSimple(
            string sku,
            string nombre,
            string descripcion,
            UnidadMedida unidadMedida,
            AfectacionIGV afectacionIgv,
            CodigoSUNAT? codigoSunat = null,
            BaseImponibleVentas? baseImponibleVentas = null,
            CentroCosto? centroCosto = null,
            Presupuesto? presupuesto = null,
            Peso? peso = null,
            TipoProducto tipo = TipoProducto.Bien,
            decimal precio = 0)
        {
            if (string.IsNullOrWhiteSpace(sku)) throw new ArgumentException("SKU no puede estar vacío.", nameof(sku));
            ProductoId = Guid.NewGuid();
            Sku = new SKU(sku);
            Nombre = nombre ?? throw new ArgumentNullException(nameof(nombre));
            Descripcion = descripcion ?? string.Empty;
            UnidadMedida = unidadMedida ?? throw new ArgumentNullException(nameof(unidadMedida));
            AfectacionIgv = afectacionIgv ?? throw new ArgumentNullException(nameof(afectacionIgv));
            CodigoSunat = codigoSunat;
            BaseImponibleVentas = baseImponibleVentas;
            CentroCosto = centroCosto;
            Presupuesto = presupuesto;
            Peso = peso;
            Precio = precio;
            Tipo = tipo;

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

        public ProductoCombo CrearCombo(
            string skuCombo,
            string nombreCombo,
            IEnumerable<ComponenteCombo> componentes,
            decimal precio,
            UnidadMedida unidad,
            AfectacionIGV igv,
            decimal pesoTotal,
            string estado)
        {
            if (!Activo) throw new InvalidOperationException("No se puede crear combo de producto inactivo.");
            var combo = new ProductoCombo(skuCombo, nombreCombo, componentes, precio, unidad, igv, pesoTotal, estado);
            // Dispatch(...)
            return combo;
        }

        public void ActualizarDescripcion(string descripcion)
        {
            Descripcion = descripcion ?? throw new ArgumentNullException(nameof(descripcion));
            var ev = new ProductoModificado(ProductoId);
            // Dispatch(ev);
        }

        public void ActualizarPeso(Peso? peso)
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

        public void EditarDatos(string nuevoNombre, string nuevaDescripcion, decimal nuevoPrecio)
        {
            if (string.IsNullOrWhiteSpace(nuevoNombre))
                throw new ArgumentException("El nombre no puede estar vacío.");
            if (nuevoPrecio < 0)
                throw new ArgumentException("El precio no puede ser negativo.");

            Nombre = nuevoNombre;
            Descripcion = nuevaDescripcion;
            Precio = nuevoPrecio;
        }
    }
}