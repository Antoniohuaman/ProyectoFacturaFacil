using System;

namespace GestionClientesBC.Application.Clientes.Contactos.Eliminar
{
    /// <summary>
    /// Entrada para eliminar un contacto secundario de un cliente.
    /// </summary>
    public sealed class EliminarContactoClienteInputDto
    {
        public Guid ClienteId { get; init; }
        public Guid ContactoId { get; init; }
        public int? ExpectedVersion { get; init; }
    }
}
