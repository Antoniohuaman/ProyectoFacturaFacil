using System.Collections.Generic;
using System.Threading.Tasks;

namespace ListaPreciosBC.Application.Interfaces
{
    public interface IUsuarioRepository
    {
        Task<List<string>> GetUsuariosConPermisoAsync(string permiso);
    }
}