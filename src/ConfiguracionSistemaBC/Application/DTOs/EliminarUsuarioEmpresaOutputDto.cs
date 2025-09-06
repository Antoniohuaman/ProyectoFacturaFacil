using System;

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Resultado de la eliminación del usuario en la empresa.
    /// </summary>
    public sealed class EliminarUsuarioEmpresaOutputDto
    {
        public string EmpresaId { get; init; } = string.Empty;
        public Guid UsuarioId { get; init; }
        public bool Eliminado { get; init; }
    }
}
