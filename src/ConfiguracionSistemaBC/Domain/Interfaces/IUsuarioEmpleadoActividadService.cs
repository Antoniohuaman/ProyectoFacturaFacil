using ConfiguracionSistemaBC.Domain.ValueObjects;
using System.Collections.Generic;
namespace ConfiguracionSistemaBC.Domain.Interfaces
{
    /// <summary>
    /// Servicio para consultar si un usuario empleado ha realizado acciones en el sistema.
    /// </summary>
    public interface IUsuarioEmpleadoActividadService
    {
        /// <summary>
        /// Indica si el usuario empleado ha realizado alguna acción relevante en el sistema.
        /// </summary>
        bool TieneAcciones(Guid usuarioEmpleadoId);

        /// <summary>
        /// Indica si el usuario empleado ha realizado acciones en un establecimiento específico.
        /// </summary>
        bool TieneAccionesEnEstablecimiento(Guid usuarioEmpleadoId, EstablecimientoId establecimientoId);

        /// <summary>
        /// Indica si el usuario empleado ha realizado acciones en alguno de los establecimientos indicados.
        /// </summary>
        bool TieneAccionesEnEstablecimientos(Guid usuarioEmpleadoId, IEnumerable<EstablecimientoId> establecimientos);
    }
}
