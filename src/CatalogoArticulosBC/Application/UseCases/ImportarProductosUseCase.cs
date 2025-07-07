// src/CatalogoArticulosBC/Application/UseCases/ImportarProductosUseCase.cs
using System.Threading.Tasks;
using CatalogoArticulosBC.Application.DTOs;
using CatalogoArticulosBC.Application.Interfaces;

namespace CatalogoArticulosBC.Application.UseCases
{
    /// <summary>
    /// Orquesta la importación masiva de productos desde Excel/CSV.
    /// </summary>
    public class ImportarProductosUseCase
    {
        private readonly IImportExportService _service;

        public ImportarProductosUseCase(IImportExportService service)
        {
            _service = service;
        }

        public Task<ResultadoImportacionDto> HandleAsync(string rutaArchivo)
            => _service.ImportarDesdeExcelAsync(rutaArchivo);
    }
}
