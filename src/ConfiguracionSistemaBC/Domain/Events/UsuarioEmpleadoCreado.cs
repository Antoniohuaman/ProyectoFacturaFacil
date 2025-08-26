using System;
using SharedKernel.ValueObjects;
using ConfiguracionSistemaBC.Domain.ValueObjects;
using SharedKernel.Events;
using ConfiguracionSistemaBC.Domain.Aggregates; // RolUsuario enum

namespace ConfiguracionSistemaBC.Domain.Events
{
    /// <summary>
    /// Evento de dominio que representa la creación de un UsuarioEmpleado.
    /// Ahora se asocia a uno o varios establecimientos, no sucursales.
    /// </summary>
    public sealed record UsuarioEmpleadoCreado(
        Guid UsuarioEmpleadoId,
        EmpresaId EmpresaId,
        IReadOnlyCollection<EstablecimientoId> Establecimientos,
    Email Email,
        NombrePersona Nombre,
        RolUsuario Rol
    ) : IDomainEvent;
}
