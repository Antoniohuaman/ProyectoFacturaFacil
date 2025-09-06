using System;

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Comando para eliminar a un usuario de la empresa actual.
    /// Requiere control de concurrencia optimista.
    /// </summary>
    public sealed class EliminarUsuarioEmpresaInputDto
    {
        /// <summary>Identidad global del usuario a eliminar.</summary>
        public Guid UsuarioId { get; init; }

        /// <summary>Versión esperada del agregado para concurrencia optimista.</summary>
        public int ExpectedVersion { get; init; }
    }
}
