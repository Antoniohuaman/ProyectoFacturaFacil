public class ImportarClientesDto
{
    public byte[] Archivo { get; set; } = Array.Empty<byte>(); // El archivo subido (CSV/Excel)
    public string NombreArchivo { get; set; } = string.Empty;  // Para saber si es .csv o .xlsx
}