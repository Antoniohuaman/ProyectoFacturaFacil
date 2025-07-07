// src/CatalogoArticulosBC/Domain/Entities/MultimediaProducto.cs
using System;

namespace CatalogoArticulosBC.Domain.Entities
{
    public sealed class MultimediaProducto
    {
        public Guid MultimediaId  { get; }
        public Guid ProductoId    { get; }
        public string Tipo        { get; }
        public byte[] Contenido   { get; }

        public MultimediaProducto(Guid multimediaId, Guid productoId, string tipo, byte[] contenido)
        {
            MultimediaId = multimediaId != Guid.Empty
                ? multimediaId
                : throw new ArgumentException("MultimediaId inválido.", nameof(multimediaId));
            ProductoId = productoId;
            Tipo = !string.IsNullOrWhiteSpace(tipo)
                ? tipo
                : throw new ArgumentException("El tipo no puede estar vacío.", nameof(tipo));
            Contenido = contenido ?? throw new ArgumentNullException(nameof(contenido));
        }
    }
}