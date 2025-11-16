using System;

namespace GestionClientesBC.Application.Clientes.FotoPerfil.Actualizar
{
    /// <summary>
    /// Resultado de actualizar la foto de perfil.
    /// </summary>
    public sealed class ActualizarFotoPerfilClienteOutputDto
    {
        public Guid ClienteId { get; init; }
        public string EmpresaId { get; init; } = null!;
        public bool TieneFoto { get; init; }
        public string? NombreArchivo { get; init; }
        public string? UrlPublica { get; init; }
        public DateTime FechaActualizacionUtc { get; init; }
        public int Version { get; init; }
    }
}
