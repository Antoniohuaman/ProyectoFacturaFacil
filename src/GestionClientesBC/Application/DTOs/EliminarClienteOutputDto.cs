using System;

namespace GestionClientesBC.Application.Clientes.Eliminar
{
    public sealed class EliminarClienteOutputDto
    {
        public Guid ClienteId { get; init; }
        public string EmpresaId { get; init; } = null!;
        public bool Eliminado { get; init; }
        public DateTime FechaEliminacionUtc { get; init; }

        // Datos informativos del cliente eliminado
        public string TipoDocumento { get; init; } = null!;
        public string NumeroDocumento { get; init; } = null!;
    }
}
