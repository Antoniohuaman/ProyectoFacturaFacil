using System;

namespace GestionClientesBC.Application.Clientes.EliminarTodos
{
    /// <summary>
    /// Resultado de la eliminación masiva por empresa.
    /// </summary>
    public sealed class EliminarTodosLosClientesOutputDto
    {
        public string EmpresaId { get; init; } = null!;
        public int Eliminados { get; init; }
        public DateTime FechaEjecucionUtc { get; init; }
        public string? Motivo { get; init; }
    }
}
