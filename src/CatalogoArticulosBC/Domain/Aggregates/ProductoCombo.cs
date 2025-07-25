using System;
using System.Collections.Generic;
using CatalogoArticulosBC.Domain.ValueObjects;
using CatalogoArticulosBC.Domain.Entities;
using CatalogoArticulosBC.Domain.Events;
using CatalogoArticulosBC.Domain.Exceptions;

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
        public decimal? PesoTotal { get; private set; } // Opcional
        public string Estado { get; private set; }
        public bool Activo { get; private set; } = true;

        // Implementación explícita de la interfaz
        decimal IProductoConPeso.Peso => PesoTotal ?? 0;

        public ProductoCombo(
            string sku,
            string nombre,
            IEnumerable<ComponenteCombo> componentes,
            decimal precio,
            UnidadMedida unidad,
            AfectacionIGV igv,
            decimal? pesoTotal,
            string estado)
        {
            if (string.IsNullOrWhiteSpace(sku))
                throw new ArgumentException("El SKU del combo es obligatorio.", nameof(sku));
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("El nombre del combo es obligatorio.", nameof(nombre));
            if (componentes == null)
                throw new ArgumentException("Debe especificar al menos un componente.", nameof(componentes));
            if (precio < 0)
                throw new ArgumentException("El precio del combo no puede ser negativo.", nameof(precio));
            if (string.IsNullOrWhiteSpace(estado))
                throw new ArgumentException("El estado es obligatorio.", nameof(estado));

            ProductoComboId = Guid.NewGuid();
            Sku = new SKU(sku);
            Nombre = nombre;
            Componentes = new List<ComponenteCombo>(componentes);
            Precio = precio;
            UnidadMedida = unidad ?? throw new ArgumentNullException(nameof(unidad));
            AfectacionIgv = igv ?? throw new ArgumentNullException(nameof(igv));
            PesoTotal = pesoTotal;
            Estado = estado;

            var ev = new ProductoCreado(ProductoComboId, Sku, "COMBO");
            // Dispatch(ev);
        }

        public void Deshabilitar(string motivo)
        {
            Activo = false;
            var ev = new ProductoInhabilitado(ProductoComboId, motivo);
            // Dispatch(ev);
        }
        // --- Gestión de multimedia ---
        private const int MAX_MULTIMEDIA = 5;
        private const long MAX_TAMANO = 10 * 1024 * 1024; // 10 MB

        public List<MultimediaProducto> Multimedia { get; } = new();

        public void AgregarMultimedia(Guid multimediaId, string tipoAdjunto, string nombreArchivo, string ruta, string comentario, long tamano)
        {
            if (Multimedia.Count >= MAX_MULTIMEDIA)
                throw new LimiteMultimediaException();

            if (!EsTipoPermitido(tipoAdjunto))
                throw new MultimediaInvalidaException("Tipo de archivo no permitido.");

            if (tamano > MAX_TAMANO)
                throw new MultimediaInvalidaException("El archivo excede el tamaño máximo permitido (10 MB).");

            var multimedia = new MultimediaProducto(multimediaId, tipoAdjunto, nombreArchivo, ruta, comentario, tamano);
            Multimedia.Add(multimedia);
            // AgregarEvento(new ProductoModificado(this.ProductoComboId, "MULTIMEDIA_AGREGADA", multimediaId));
        }

        public void EliminarMultimedia(Guid multimediaId)
        {
            var multimedia = Multimedia.Find(m => m.MultimediaId == multimediaId)
                ?? throw new InvalidOperationException("Recurso multimedia no encontrado.");

            Multimedia.Remove(multimedia);
            // AgregarEvento(new ProductoModificado(this.ProductoComboId, "MULTIMEDIA_ELIMINADA", multimediaId));
        }

        private bool EsTipoPermitido(string tipoAdjunto)
        {
            var permitidos = new[] { "image/jpeg", "image/png", "application/pdf" };
            return Array.Exists(permitidos, t => t.Equals(tipoAdjunto, StringComparison.OrdinalIgnoreCase));
        }
        // --- Fin gestión de multimedia ---
    }
}