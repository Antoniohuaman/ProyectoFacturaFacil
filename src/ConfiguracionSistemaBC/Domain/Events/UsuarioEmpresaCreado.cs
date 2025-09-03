using System;
using SharedKernel.ValueObjects;
using SharedKernel.Events;
using ConfiguracionSistemaBC.Domain.Aggregates;

namespace ConfiguracionSistemaBC.Domain.Events
{
    /// <summary>
    /// Evento de dominio que representa la creación de un UsuarioEmpresa.
    /// Ahora se asocia a uno o varios establecimientos, no sucursales.
    /// </summary>
    public sealed record UsuarioEmpresaCreado(
        UsuarioId UsuarioId,
        EmpresaId EmpresaId,
        IReadOnlyCollection<EstablecimientoId> Establecimientos,
        Email Email,
        NombrePersona Nombre,
        RolEmpresa? Rol // Ahora es opcional
    ) : IDomainEvent;
}
