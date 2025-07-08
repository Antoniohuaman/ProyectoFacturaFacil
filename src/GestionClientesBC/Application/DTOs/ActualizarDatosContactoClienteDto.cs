using System;

namespace GestionClientesBC.Application.DTOs
{
    public class ActualizarDatosContactoClienteDto
    {
        public Guid ClienteId { get; set; }
        public string NuevoCorreo { get; set; } = default!;
        public string NuevoCelular { get; set; } = default!;
        public string UsuarioId { get; set; } = default!;
    }
}