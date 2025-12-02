// src/GestionCobranzasBC/Application/Interfaces/IUnitOfWorkGestionCobranzas.cs
namespace GestionCobranzasBC.Application.Interfaces;

using System.Threading;
using System.Threading.Tasks;

public interface IUnitOfWorkGestionCobranzas
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}
