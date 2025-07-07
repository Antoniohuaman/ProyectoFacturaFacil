using System.Threading.Tasks;

namespace ControlCajaBC.Application.Interfaces
{
    /// <summary>
    /// Unidad de trabajo para asegurar consistencia transaccional.
    /// </summary>
    public interface IUnitOfWork
    {
        /// <summary>
        /// Persiste todos los cambios pendientes.
        /// </summary>
        Task CommitAsync();
    }
}
