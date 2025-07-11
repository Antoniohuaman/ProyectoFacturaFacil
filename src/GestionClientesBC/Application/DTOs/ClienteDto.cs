using System;

namespace GestionClientesBC.Application.DTOs
{
    public class ClienteDto
    {
        public string? TipoDocumento { get; set; }
        public string? NumeroDocumento { get; set; }
        public string? RazonSocialONombres { get; set; }
        public string? Correo { get; set; }
        public string? Celular { get; set; }
        public string? DireccionPostal { get; set; }
        public string? TipoCliente { get; set; }
        // Si luego necesitas moneda o forma de pago, agrégalas aquí como opcionales.
    }
}