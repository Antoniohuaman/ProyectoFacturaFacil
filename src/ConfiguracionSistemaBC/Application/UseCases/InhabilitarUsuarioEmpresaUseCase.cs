using System;

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Comando para inhabilitar a un usuario dentro de una empresa (membresía).
    /// </summary>
    public sealed class InhabilitarUsuarioEmpresaInputDto
    {
        /// <summary>
        /// Identidad de la empresa. Opcional si el contexto (ITenantContext) ya la provee.
        /// </summary>
        public string? EmpresaId { get; init; }

        /// <summary>Identidad global del usuario (mismo que usa Identidad).</summary>
        public Guid UsuarioId { get; init; }

        /// <summary>Motivo de inhabilitación (auditoría / eventos de dominio).</summary>
        public string Razon { get; init; } = "Inhabilitado por el administrador.";

        /// <summary>
        /// Versión esperada del agregado (concurrencia optimista).
        /// Debe ser la versión actual al momento de enviar el comando.
        /// </summary>
        public int ExpectedVersion { get; init; }
    }
}
