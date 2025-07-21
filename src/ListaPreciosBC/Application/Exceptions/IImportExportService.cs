using System.Threading.Tasks;
using ListaPreciosBC.Application.DTOs;

namespace ListaPreciosBC.Application.Interfaces
{
    public interface IImportExportService
    {
        Task<ResultadoExportacionDto> ExportarListasPreciosAsync(string filtrosJson);
    }
}