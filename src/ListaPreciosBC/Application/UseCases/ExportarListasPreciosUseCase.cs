using System.Threading.Tasks;
using ListaPreciosBC.Application.DTOs;
using ListaPreciosBC.Application.Interfaces;

namespace ListaPreciosBC.Application.UseCases
{
    /// <summary>
    /// Caso de uso: Exportar listas de precios a Excel.
    /// Permite al usuario descargar todas las listas y precios filtrados para edición externa.
    /// </summary>
    public class ExportarListasPreciosUseCase
    {
        private readonly IImportExportService _service;

        /// <summary>
        /// Constructor con inyección del servicio de exportación.
        /// </summary>
        public ExportarListasPreciosUseCase(IImportExportService service)
        {
            _service = service;
        }

        /// <summary>
        /// Ejecuta la exportación de listas de precios según los filtros seleccionados.
        /// </summary>
        /// <param name="filtrosJson">Filtros opcionales en formato JSON (tipoLista, estado, fecha, etc.)</param>
        /// <returns>Archivo Excel generado y metadatos para descarga.</returns>
        public Task<ResultadoExportacionDto> HandleAsync(string filtrosJson)
            => _service.ExportarListasPreciosAsync(filtrosJson);
    }
}