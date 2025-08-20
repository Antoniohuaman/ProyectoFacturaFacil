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
        public DireccionPostalDto? DireccionPostal { get; set; }
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
                DireccionPostal = entity.DireccionPostal == null ? null : new DireccionPostalDto
                {
                    PaisCodigoIso = entity.DireccionPostal.PaisCodigoIso,
                    Linea = entity.DireccionPostal.Linea,
                    Ubigeo = entity.DireccionPostal.Ubigeo,
                    Departamento = entity.DireccionPostal.Departamento,
                    Provincia = entity.DireccionPostal.Provincia,
                    Distrito = entity.DireccionPostal.Distrito,
                    AddressTypeCode = entity.DireccionPostal.AddressTypeCode,
                    Urbanizacion = entity.DireccionPostal.Urbanizacion,
                    Referencia = entity.DireccionPostal.Referencia
                },
                TipoCliente = entity.TipoCliente.ToString(),
                Estado = entity.Estado.ToString(),
                FechaRegistro = entity.FechaRegistro
            };
        }
        // Si luego necesitas moneda o forma de pago, agrégalas aquí como opcionales.
    }

    public class DireccionPostalDto
    {
        public string PaisCodigoIso { get; set; } = string.Empty;
        public string Linea { get; set; } = string.Empty;
        public string Ubigeo { get; set; } = string.Empty;
        public string Departamento { get; set; } = string.Empty;
        public string Provincia { get; set; } = string.Empty;
        public string Distrito { get; set; } = string.Empty;
        public string AddressTypeCode { get; set; } = string.Empty;
        public string? Urbanizacion { get; set; }
        public string? Referencia { get; set; }
    }
}