using SharedKernel.ValueObjects;

namespace GestionClientesBC.Application.Clientes.Crear
{
    /// <summary>
    /// Entrada para crear un cliente. Solo lo obligatorio es requerido;
    /// el resto es opcional.
    /// </summary>
    public sealed class CrearClienteInputDto
    {
        // Obligatorios
        public TipoDocumento TipoDocumento { get; init; }
        public string NumeroDocumento { get; init; } = null!;

        /// <summary>Obligatorio si TipoDocumento = Ruc (se validará en el caso de uso).</summary>
        public string? RazonSocial { get; init; }

        /// <summary>Obligatorio si TipoDocumento != Ruc (se validará en el caso de uso).</summary>
        public string? NombresCompletos { get; init; }
        public string? Nombres { get; init; }
        public string? Apellidos { get; init; }

        // Opcionales
        public string? Correo { get; init; }
        /// <summary>Teléfonos en un solo campo (VO Telefono.FromTexto los normaliza).</summary>
        public string? Telefonos { get; init; }

        // Metadatos opcionales
        /// <summary>Nombre comercial o amigable (VO NombreCliente).</summary>
        public string? NombreComercial { get; init; }
        public string? PaginaWeb { get; init; }
        public string? Observaciones { get; init; }
        public string? FotoPerfilNombreArchivo { get; init; }
        public string? FotoPerfilUrl { get; init; }

        // Dirección (opcional)
        public string? PaisCodigoIso { get; init; } = "PE"; // por defecto PE
        public string? DireccionLinea { get; init; }
        public string? Ubigeo { get; init; }
        public string? Departamento { get; init; }
        public string? Provincia { get; init; }
        public string? Distrito { get; init; }
        public string? AddressTypeCode { get; init; }

        // Segmentación (opcionales)
        /// <summary>"C", "P" o "CP". Si no viene, se usa "C".</summary>
        public string? TipoClienteCodigo { get; init; }
        /// <summary>"SIN","MAY","MIN","DIS","REV".</summary>
        public string? RolClienteCodigo { get; init; }
    }
}
