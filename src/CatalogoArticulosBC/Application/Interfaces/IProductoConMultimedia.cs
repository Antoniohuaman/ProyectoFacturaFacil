using System;
using System.Collections.Generic;

namespace CatalogoArticulosBC.Domain.Entities
{
    public interface IProductoConMultimedia
    {
        List<MultimediaProducto> Multimedia { get; }
        void AgregarMultimedia(Guid multimediaId, string tipoAdjunto, string nombreArchivo, string ruta, string comentario, long tamano);
        void EliminarMultimedia(Guid multimediaId);
    }
}