// src/CatalogoArticulosBC/Application/UseCases/ExportarProductosUseCase.cs
using System.Threading.Tasks;
using CatalogoArticulosBC.Application.DTOs;
using CatalogoArticulosBC.Application.Interfaces;


namespace CatalogoArticulosBC.Application.UseCases
{
    /// <summary>
    /// Genera un Excel/CSV con el catálogo filtrado.
    /// </summary>
    public class ExportarProductosUseCase
    {
        private readonly IImportExportService _service;

        public ExportarProductosUseCase(IImportExportService service)
        {
            _service = service;
        }

        public Task<ResultadoExportacionDto> HandleAsync(string filtrosJson)
            => _service.ExportarProductosAsync(filtrosJson);
    }
}
