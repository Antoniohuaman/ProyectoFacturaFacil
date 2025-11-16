using System;

namespace GestionClientesBC.Application.Clientes.Contactos.Eliminar
{
    /// <summary>
    /// Resultado de eliminar un contacto secundario.
    /// </summary>
    public sealed class EliminarContactoClienteOutputDto
    {
        public Guid ClienteId { get; init; }
        public string EmpresaId { get; init; } = null!;
        public Guid ContactoId { get; init; }
        public int TotalContactos { get; init; }
        public DateTime? FechaEventoUtc { get; init; }
        public int Version { get; init; }
    }
}
