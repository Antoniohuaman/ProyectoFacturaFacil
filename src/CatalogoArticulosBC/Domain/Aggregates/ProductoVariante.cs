using System;
using System.Collections.Generic;
using CatalogoArticulosBC.Domain.ValueObjects;
using CatalogoArticulosBC.Domain.Entities;
using CatalogoArticulosBC.Domain.Events;
using CatalogoArticulosBC.Domain.Exceptions;

namespace CatalogoArticulosBC.Domain.Aggregates
{
    public class ProductoVariante
    {
        public Guid ProductoVarianteId { get; }
        public Guid ProductoPadreId { get; }
        public SKU Sku { get; private set; }
        public IReadOnlyCollection<AtributoVariante> Atributos { get; private set; }
        public bool Activo { get; private set; } = true;
        public DateTime FechaCreacion { get; }

        public ProductoVariante(Guid padreId, string sku, IEnumerable<AtributoVariante> atributos)
        {
            ProductoVarianteId = Guid.NewGuid();
            ProductoPadreId = padreId;
            Sku = new SKU(sku);
            Atributos = new List<AtributoVariante>(atributos);
            FechaCreacion = DateTime.UtcNow;

            var ev = new ProductoCreado(ProductoVarianteId, Sku, "VARIANTE");
            // Dispatch(ev);
        }

        public void Deshabilitar(string motivo)
        {
            Activo = false;
            var ev = new ProductoInhabilitado(ProductoVarianteId, motivo);
            // Dispatch(ev);
        }

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