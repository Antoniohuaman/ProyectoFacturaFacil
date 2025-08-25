using System;
using ConfiguracionSistemaBC.Domain.ValueObjects;
using SharedKernel.Events;
using ConfiguracionSistemaBC.Domain.Aggregates; // RolUsuario enum

namespace ConfiguracionSistemaBC.Domain.Events
{
    /// <summary>
    /// Evento de dominio que representa la creación de un UsuarioEmpleado.
    /// </summary>
    public sealed record UsuarioEmpleadoCreado(
        Guid UsuarioEmpleadoId,
        EmpresaId EmpresaId,
        SucursalId SucursalId,
        CorreoElectronico Email,
        NombrePersona Nombre,
        RolUsuario Rol
    ) : IDomainEvent;
}
