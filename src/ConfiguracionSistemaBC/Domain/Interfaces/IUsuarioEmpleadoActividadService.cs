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
    }
}
