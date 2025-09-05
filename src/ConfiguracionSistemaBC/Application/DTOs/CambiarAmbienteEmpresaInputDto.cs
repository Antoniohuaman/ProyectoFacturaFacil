using System;

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Solicitud para cambiar el ambiente de la empresa del contexto.
    /// </summary>
    public sealed class CambiarAmbienteEmpresaInputDto
    {
        /// <summary>
        /// Destino del ambiente: "PRUEBA" o "PRODUCCION".
        /// </summary>
        public string Destino { get; init; } = "PRODUCCION";

        /// <summary>
        /// Si es true y el destino es PRODUCCION, se purgan los documentos emitidos en PRUEBA.
        /// </summary>
        public bool BorrarDocumentosEmitidosEnPrueba { get; init; } = true;
    }
}
