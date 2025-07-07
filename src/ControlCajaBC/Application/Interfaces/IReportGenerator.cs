using System;
using ControlCajaBC.Domain.Aggregates;    // <-- Esto faltaba

namespace ControlCajaBC.Application.Interfaces
{
    /// <summary>
    /// Genera un documento PDF para un turno de caja cerrado.
    /// </summary>
    public interface IReportGenerator
    {
        /// <summary>
        /// Genera el PDF con el resumen del cierre y movimientos.
        /// </summary>
        byte[] GenerateClosingReport(TurnoCaja turno);
    }
}
