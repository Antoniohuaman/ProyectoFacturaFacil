using System;
using SharedKernel.ValueObjects;

namespace GestionClientesBC.Application.Clientes.Editar
{
    /// <summary>
    /// Entrada para editar un cliente existente. Todos los campos son opcionales,
    /// salvo el identificador. Solo se actualiza lo que venga informado.
    /// </summary>
    public sealed class EditarClienteInputDto
    {
        public Guid ClienteId { get; init; }
        /// <summary>Versión esperada del agregado para concurrencia optimista.</summary>
        public int? ExpectedVersion { get; init; }

        // --- Documento (opcional) ---
        public TipoDocumento? TipoDocumento { get; init; }
        public string? NumeroDocumento { get; init; }

        /// <summary>Si el documento final es RUC, se requiere esta razón social (si el cliente aún no la tiene).</summary>
        public string? RazonSocial { get; init; }

        /// <summary>Si el documento final no es RUC, se requieren nombres (si el cliente aún no los tiene).</summary>
        public string? NombresCompletos { get; init; }
        public string? Nombres { get; init; }
        public string? Apellidos { get; init; }

        // --- Contacto (opcionales) ---
        public string? Correo { get; init; }
        /// <summary>Teléfonos en un solo campo (VO Telefono.FromTexto los normaliza).</summary>
        public string? Telefonos { get; init; }

        // --- Metadatos opcionales ---
        public string? NombreComercial { get; init; }
        public string? PaginaWeb { get; init; }
        public string? Observaciones { get; init; }
        public string? FotoPerfilNombreArchivo { get; init; }
        public string? FotoPerfilUrl { get; init; }

        // --- Dirección (opcionales) ---
        public string? PaisCodigoIso { get; init; } = "PE";
        public string? DireccionLinea { get; init; }
        public string? Ubigeo { get; init; }
        public string? Departamento { get; init; }
        public string? Provincia { get; init; }
        public string? Distrito { get; init; }
        public string? AddressTypeCode { get; init; }

        // --- Segmentación (opcionales) ---
        /// <summary>"C", "P" o "CP".</summary>
        public string? TipoClienteCodigo { get; init; }

        /// <summary>"SIN","MAY","MIN","DIS","REV". Si no se informa, no cambia. Para eliminar rol, usar RemoverRolCliente=true.</summary>
        public string? RolClienteCodigo { get; init; }
        public bool? RemoverRolCliente { get; init; }

        // --- Estado (opcional) ---
        /// <summary>Si true, se habilita. Si false, se deshabilita (requiere motivo si tu negocio lo exige).</summary>
        public bool? Habilitado { get; init; }
        public string? MotivoDeshabilitacion { get; init; }
    }
}
