using System;
using System.Collections.Generic;
using ConfiguracionSistemaBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace ConfiguracionSistemaBC.Application.DTOs
{
    /// <summary>
    /// Entrada para registrar un usuario en la empresa (tenant actual).
    /// - Email: obligatorio
    /// - NombreCompleto: opcional (puede enviarse vacío)
    /// - Celular: opcional
    /// - Accesos: obligatorio (al menos un establecimiento con al menos un rol)
    /// </summary>
    public sealed class RegistrarUsuarioEmpresaInputDto
    {
        public string Email { get; init; } = string.Empty;
        public string? NombreCompleto { get; init; }
        public string? Celular { get; init; }

        /// <summary>
        /// Accesos por establecimiento y roles asignados.
        /// </summary>
        public List<AccesoItem> Accesos { get; init; } = new();

        public sealed class AccesoItem
        {
            public Guid EstablecimientoId { get; init; }
            public List<Guid> RolIds { get; init; } = new();
        }
    }
}
