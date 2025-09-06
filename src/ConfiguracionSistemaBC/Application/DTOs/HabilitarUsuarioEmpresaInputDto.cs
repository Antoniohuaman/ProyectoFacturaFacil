using System;

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Datos para habilitar a un usuario (poner su estado en Habilitado) en la empresa actual.
    /// </summary>
    public sealed class HabilitarUsuarioEmpresaInputDto
    {
        /// <summary>Identidad global del usuario (Guid).</summary>
        public Guid UsuarioId { get; init; }

        /// <summary>
        /// Versión esperada del agregado (concurrencia optimista).
        /// Debe ser la versión que el cliente observó antes de solicitar el cambio.
        /// </summary>
        public int ExpectedVersion { get; init; }
    }
}
