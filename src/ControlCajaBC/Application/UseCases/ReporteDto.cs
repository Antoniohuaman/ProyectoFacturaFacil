namespace ControlCajaBC.Application.UseCases
{
    /// <summary>
    /// DTO con la información para descargar el reporte de cierre.
    /// </summary>
    public sealed class ReporteDto
    {
        public ReporteDto(Guid turnoId, byte[] contenidoPdf, string nombreArchivo)
        {
            TurnoId       = turnoId;
            ContenidoPdf  = contenidoPdf;
            NombreArchivo = nombreArchivo;
        }

        /// <summary>Id del turno cerrado.</summary>
        public Guid   TurnoId       { get; }

        /// <summary>Bytes del PDF generado.</summary>
        public byte[] ContenidoPdf  { get; }

        /// <summary>Nombre sugerido para el archivo.</summary>
        public string NombreArchivo { get; }
    }
}
