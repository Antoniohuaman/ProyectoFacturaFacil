using System;
using GestionClientesBC.Domain.Aggregates;

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

        // NUEVO para este caso de uso consultarCliente:
        public string? Estado { get; set; }
        public DateTime? FechaRegistro { get; set; }

        public static ClienteDto FromEntity(Cliente entity)
        {
            return new ClienteDto
            {
                TipoDocumento = entity.DocumentoIdentidad.Tipo.ToString(),
                NumeroDocumento = entity.DocumentoIdentidad.Numero,
                RazonSocialONombres = entity.RazonSocialONombres,
                Correo = entity.Correo,
                Celular = entity.Celular,
                DireccionPostal = entity.DireccionPostal,
                TipoCliente = entity.TipoCliente.ToString(),
                Estado = entity.Estado.ToString(),
                FechaRegistro = entity.FechaRegistro
            };
        }
        // Si luego necesitas moneda o forma de pago, agrégalas aquí como opcionales.
    }
}