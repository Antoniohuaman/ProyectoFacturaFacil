using System;

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Resultado de habilitar a un usuario dentro de la empresa.
    /// </summary>
    public sealed class HabilitarUsuarioEmpresaOutputDto
    {
        public Guid UsuarioId { get; init; }
        public string Estado { get; init; } = string.Empty;
        public int Version { get; init; }
    }
}
