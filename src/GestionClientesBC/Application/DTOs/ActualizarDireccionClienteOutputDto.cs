using System;

namespace GestionClientesBC.Application.Clientes.Direccion.Actualizar
{
    /// <summary>
    /// Resultado de actualizar el domicilio fiscal del cliente.
    /// </summary>
    public sealed class ActualizarDireccionClienteOutputDto
    {
        public Guid ClienteId { get; init; }
        public string EmpresaId { get; init; } = null!;
        public string PaisCodigoIso { get; init; } = null!;
        public string? DireccionLinea { get; init; }
        public string? Ubigeo { get; init; }
        public string? Departamento { get; init; }
        public string? Provincia { get; init; }
        public string? Distrito { get; init; }
        public string? AddressTypeCode { get; init; }
        public string DireccionFormateada { get; init; } = string.Empty;
        public DateTime FechaActualizacionUtc { get; init; }
        public int Version { get; init; }
    }
}
