using System;

namespace CatalogoArticulosBC.Application.DTOs
{
    public class EliminarProductoSimpleDto
    {
        public Guid ProductoId { get; }
        public string UsuarioId { get; }

        public EliminarProductoSimpleDto(Guid productoId, string usuarioId)
        {
            ProductoId = productoId;
            UsuarioId = usuarioId;
        }
    }
}