using System;

namespace CatalogoArticulosBC.Domain.Entities
{
    /// <summary>
    /// Representa un archivo multimedia asociado a un producto,
    /// con su tipo MIME, metadatos y fecha de carga.
    /// </summary>
    public class MultimediaProducto
    {
        // Identidad (solo lectura)
        public Guid MultimediaId { get; }

        // Tipo MIME real del archivo (image/jpeg, image/png, etc.)
        public string TipoMime { get; }

        // Tipo de adjunto (p.ej. "ImagenPrincipal", "ManualPDF", etc.)
        public string TipoAdjunto { get; }

        public string NombreArchivo { get; }
        public string Ruta { get; }
        public string Comentario { get; }
        public long Tamano { get; }
        public DateTime FechaCarga { get; }

        /// <summary>
        /// Crea una nueva instancia de MultimediaProducto
        /// validando todos los campos obligatorios.
        /// </summary>
        public MultimediaProducto(
            Guid multimediaId,
            string tipoMime,
            string tipoAdjunto,
            string nombreArchivo,
            string ruta,
            string comentario,
            long tamano)
        {
            // Validaciones básicas
            if (multimediaId == Guid.Empty)
                throw new ArgumentException("El ID de multimedia es obligatorio.", nameof(multimediaId));

            if (string.IsNullOrWhiteSpace(tipoMime))
                throw new ArgumentException("El tipo MIME es obligatorio.", nameof(tipoMime));

            if (string.IsNullOrWhiteSpace(tipoAdjunto))
                throw new ArgumentException("El tipo de adjunto es obligatorio.", nameof(tipoAdjunto));

            if (string.IsNullOrWhiteSpace(nombreArchivo))
                throw new ArgumentException("El nombre de archivo es obligatorio.", nameof(nombreArchivo));

            if (string.IsNullOrWhiteSpace(ruta))
                throw new ArgumentException("La ruta es obligatoria.", nameof(ruta));

            if (tamano <= 0)
                throw new ArgumentException("El tamaño debe ser mayor a cero.", nameof(tamano));

            // Asignaciones normalizando espacios y mayúsculas/minúsculas
            MultimediaId   = multimediaId;
            TipoMime       = tipoMime.Trim();
            TipoAdjunto    = tipoAdjunto.Trim();
            NombreArchivo  = nombreArchivo.Trim();
            Ruta           = ruta.Trim();
            Comentario     = comentario?.Trim() ?? string.Empty;
            Tamano         = tamano;
            FechaCarga     = DateTime.UtcNow;
        }
    }
}
