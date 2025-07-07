// IImportExportService.cs
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CatalogoArticulosBC.Application.DTOs;

using System.Threading.Tasks;

namespace CatalogoArticulosBC.Application.Interfaces
{
    public interface IImportExportService
    {
       Task ImportarAsync(Stream csvStream, CancellationToken ct = default);
        Task<Stream> ExportarAsync(CancellationToken ct = default);
        Task<ResultadoImportacionDto> ImportarDesdeExcelAsync(string filePath); // <-- cambia aquí
        Task<ResultadoExportacionDto> ExportarProductosAsync(string filePath);   // <-- y aquí
    }
}
