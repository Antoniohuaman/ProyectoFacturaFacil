using System;

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Respuesta al inhabilitar un usuario de empresa.
    /// </summary>
    public sealed class InhabilitarUsuarioEmpresaOutputDto
    {
        public string EmpresaId { get; init; } = string.Empty;
        public Guid UsuarioId { get; init; }
        public string Estado { get; init; } = "Inhabilitado";
        public int NuevaVersion { get; init; }
        public bool YaEstabaInhabilitado { get; init; }
    }
}
