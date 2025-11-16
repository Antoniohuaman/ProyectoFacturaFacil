using System;

namespace GestionClientesBC.Application.Clientes.Direccion.Actualizar
{
    /// <summary>
    /// Entrada para actualizar el domicilio fiscal de un cliente.
    /// </summary>
    public sealed class ActualizarDireccionClienteInputDto
    {
        /// <summary>Cliente destino.</summary>
        public Guid ClienteId { get; init; }
        /// <summary>Versión esperada del agregado para concurrencia optimista.</summary>
        public int? ExpectedVersion { get; init; }

        /// <summary>Código ISO-3166 alpha-2. Si se omite se usará "PE".</summary>
        public string? PaisCodigoIso { get; init; }
        public string? DireccionLinea { get; init; }
        public string? Ubigeo { get; init; }
        public string? Departamento { get; init; }
        public string? Provincia { get; init; }
        public string? Distrito { get; init; }
        public string? AddressTypeCode { get; init; }
    }
}
