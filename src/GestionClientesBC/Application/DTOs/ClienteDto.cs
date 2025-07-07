using System;

namespace GestionClientesBC.Application.DTOs
{
    public class ClienteDto
    {
        public Guid ClienteId { get; set; }
        public string TipoDocumento { get; set; } = default!;
        public string NumeroDocumento { get; set; } = default!;
        public string RazonSocialONombres { get; set; } = default!;
        public string Correo { get; set; } = default!;
        public string Celular { get; set; } = default!;
        public string DireccionPostal { get; set; } = default!;
        public string TipoCliente { get; set; } = default!;
    }
}