using System.Threading.Tasks;
using ListaPreciosBC.Application.DTOs;

namespace ListaPreciosBC.Application.Interfaces
{
    public interface IReintentosService
    {
        Task RegistrarReintentoAsync(ListaPrecioEventoDto evento, string motivo);
    }
}