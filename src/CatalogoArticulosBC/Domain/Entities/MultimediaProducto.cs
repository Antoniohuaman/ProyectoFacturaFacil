using System;

namespace CatalogoArticulosBC.Domain.Entities
{
    public class MultimediaProducto
    {
        public Guid MultimediaId { get; }
        public string TipoAdjunto { get; }
        public string NombreArchivo { get; }
        public string Ruta { get; }
        public string Comentario { get; }
        public long Tamano { get; }
        public DateTime FechaCarga { get; }

        public MultimediaProducto(Guid multimediaId, string tipoAdjunto, string nombreArchivo, string ruta, string comentario, long tamano)
        {
            if (string.IsNullOrWhiteSpace(tipoAdjunto))
                throw new ArgumentException("El tipo de adjunto es obligatorio.", nameof(tipoAdjunto));
            if (string.IsNullOrWhiteSpace(nombreArchivo))
                throw new ArgumentException("El nombre de archivo es obligatorio.", nameof(nombreArchivo));
            if (string.IsNullOrWhiteSpace(ruta))
                throw new ArgumentException("La ruta es obligatoria.", nameof(ruta));
            if (tamano <= 0)
                throw new ArgumentException("El tamaño debe ser mayor a cero.", nameof(tamano));

            MultimediaId = multimediaId;
            TipoAdjunto = tipoAdjunto;
            NombreArchivo = nombreArchivo;
            Ruta = ruta;
            Comentario = comentario;
            Tamano = tamano;
            FechaCarga = DateTime.UtcNow;
        }
    }
}