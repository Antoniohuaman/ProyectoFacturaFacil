using System;

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Solicitud para cambiar el ambiente de la empresa del contexto.
    /// Solo permite el cambio de PRUEBA a PRODUCCION.
    /// </summary>
    public sealed class CambiarAmbienteEmpresaInputDto
    {
        /// <summary>
        /// Destino del ambiente: solo puede ser "PRODUCCION" (no se permite volver a "PRUEBA").
        /// </summary>
        public string Destino { get; init; } = "PRODUCCION";

        /// <summary>
        /// Si es true y el destino es PRODUCCION, se purgan los documentos emitidos en PRUEBA.
        /// </summary>
        public bool BorrarDocumentosEmitidosEnPrueba { get; init; } = true;

        /// <summary>
        /// Valida que el destino solo pueda ser "PRODUCCION".
        /// </summary>
        public void Validar()
        {
            if (!string.Equals(Destino, "PRODUCCION", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Solo se permite cambiar el ambiente a 'PRODUCCION'.");
        }
    }
}
