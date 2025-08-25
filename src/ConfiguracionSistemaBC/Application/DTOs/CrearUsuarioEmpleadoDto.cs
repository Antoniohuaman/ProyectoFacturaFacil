using System;

namespace ConfiguracionSistemaBC.Application.DTOs
{
    /// <summary>
    /// DTO para la creación de UsuarioEmpleado, permite definir rol y nombre de perfil personalizado.
    /// </summary>
    public class CrearUsuarioEmpleadoDto
    {
        public Guid? EmpresaId { get; set; }
        public Guid? SucursalId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public int Rol { get; set; } // Enum RolUsuario
        public string? NombrePerfilPersonalizado { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
    }
}
