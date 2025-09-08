using System;

namespace GestionClientesBC.Application.Clientes.Eliminar
{
    /// <summary>
    /// Entrada para eliminar un cliente existente.
    /// </summary>
    public sealed class EliminarClienteInputDto
    {
        public Guid ClienteId { get; init; }
    }
}
