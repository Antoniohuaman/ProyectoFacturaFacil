using System;

namespace GestionClientesBC.Application.Clientes.EliminarTodos
{
    /// <summary>
    /// Entrada para eliminar todos los clientes de la empresa (tenant) actual.
    /// No requiere campos obligatorios; se incluye para consistencia y extensibilidad.
    /// </summary>
    public sealed class EliminarTodosLosClientesInputDto
    {
        /// <summary>
        /// Campo opcional para trazabilidad o UI; no es obligatorio.
        /// </summary>
        public string? Motivo { get; init; }
    }
}
