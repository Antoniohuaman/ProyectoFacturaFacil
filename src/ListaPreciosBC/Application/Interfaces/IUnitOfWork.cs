using System.Threading.Tasks;

namespace ListaPreciosBC.Application.Interfaces
{
    /// <summary>
    /// Contrato para la unidad de trabajo (transacciones y persistencia).
    /// </summary>
    public interface IUnitOfWork
    {
        Task SaveChangesAsync();
    }
}
