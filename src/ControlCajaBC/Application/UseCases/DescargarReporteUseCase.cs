using System;
using System.Threading.Tasks;
using ControlCajaBC.Application.Interfaces;
using ControlCajaBC.Domain.ValueObjects;

namespace ControlCajaBC.Application.UseCases
{
    /// <summary>
    /// Caso de uso: genera y devuelve el PDF con el reporte de cierre de turno.
    /// </summary>
    public class DescargarReporteUseCase
    {
        private readonly IControlCajaRepository _repo;
        private readonly IReportGenerator       _pdfGen;

        public DescargarReporteUseCase(
            IControlCajaRepository repo,
            IReportGenerator pdfGen)
        {
            _repo  = repo  ?? throw new ArgumentNullException(nameof(repo));
            _pdfGen = pdfGen ?? throw new ArgumentNullException(nameof(pdfGen));
        }

        /// <summary>
        /// Ejecuta la generación y descarga del reporte de cierre.
        /// </summary>
        public async Task<ReporteDto> HandleAsync(CodigoCaja codigoCaja)
        {
            // 1. Obtener turno cerrado
            var turno = await _repo.GetTurnoCerradoAsync(codigoCaja)
                        ?? throw new InvalidOperationException(
                               $"No existe un turno cerrado para la caja {codigoCaja.Value}.");

            // 2. Generar el PDF
            var pdfBytes = _pdfGen.GenerateClosingReport(turno);

            // 3. Construir DTO con un nombre de archivo legible
            var filename = $"ReporteCierre_{codigoCaja.Value}_{turno.FechaCierre!.Value:yyyyMMddHHmmss}.pdf";

            return new ReporteDto(turno.CodigoCaja.Value, pdfBytes, filename);
        }
    }
}
