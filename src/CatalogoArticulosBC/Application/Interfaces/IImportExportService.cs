// IImportExportService.cs
namespace CatalogoArticulosBC.Application.Interfaces
{
    public interface IImportExportService
    {
        Task ImportarAsync(Stream csvStream, CancellationToken ct = default);
        Task<Stream> ExportarAsync(CancellationToken ct = default);
    }
}
