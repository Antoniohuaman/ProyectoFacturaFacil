using System;

namespace GestionClientesBC.Application.Clientes.FotoPerfil.Actualizar
{
    /// <summary>
    /// Entrada para actualizar o limpiar la foto de perfil de un cliente.
    /// Si ambos campos se envían vacíos, la foto se elimina.
    /// </summary>
    public sealed class ActualizarFotoPerfilClienteInputDto
    {
        public Guid ClienteId { get; init; }
        public int? ExpectedVersion { get; init; }
        public string? NombreArchivo { get; init; }
        public string? UrlPublica { get; init; }
    }
}
