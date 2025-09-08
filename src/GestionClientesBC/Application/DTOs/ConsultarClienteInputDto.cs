using System;
using SharedKernel.ValueObjects;

namespace GestionClientesBC.Application.Clientes.Consultar
{
    /// <summary>
    /// Entrada para consultar un cliente.
    /// Puede consultarse por ClienteId O por TipoDocumento+NumeroDocumento.
    /// </summary>
    public sealed class ConsultarClienteInputDto
    {
        public Guid? ClienteId { get; init; }

        // Alternativa de búsqueda por documento
        public TipoDocumento? TipoDocumento { get; init; }
        public string? NumeroDocumento { get; init; }

        /// <summary>
        /// Si true, incluye la lista de contactos en la respuesta. Default: true.
        /// </summary>
        public bool IncluirContactos { get; init; } = true;

        /// <summary>
        /// Si true, incluye la lista de adjuntos en la respuesta. Default: true.
        /// </summary>
        public bool IncluirAdjuntos { get; init; } = true;
    }
}
