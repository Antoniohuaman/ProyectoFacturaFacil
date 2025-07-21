namespace ListaPreciosBC.Application.DTOs
{
    /// <summary>
    /// Resultado de la exportación de listas de precios.
    /// </summary>
    public class ResultadoExportacionDto
    {
        public string NombreArchivo { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public byte[] Archivo { get; set; } = Array.Empty<byte>();
    }
}