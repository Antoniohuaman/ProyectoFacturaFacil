using System;

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Respuesta del cambio de ambiente.
    /// </summary>
    public sealed class CambiarAmbienteEmpresaOutputDto
    {
        public string EmpresaId { get; init; } = string.Empty;

        /// <summary>Ambiente antes del cambio.</summary>
        public string AmbienteAnterior { get; init; } = "PRUEBA";

        /// <summary>Ambiente luego del cambio (o igual si no hubo cambio).</summary>
        public string AmbienteActual { get; init; } = "PRUEBA";

        /// <summary>True si se ejecutó purga de documentos de prueba.</summary>
        public bool PurgaEjecutada { get; init; }

        /// <summary>Instante (UTC) en que se aplicó el cambio.</summary>
        public DateTime FechaCambioUtc { get; init; } = DateTime.UtcNow;
    }
}
