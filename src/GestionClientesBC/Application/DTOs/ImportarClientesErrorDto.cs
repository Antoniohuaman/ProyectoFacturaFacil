public class ImportarClientesResultadoDto
{
    public int TotalFilas { get; set; }
    public int Creados { get; set; }
    public int Actualizados { get; set; }
    public List<ImportarClientesErrorDto> Errores { get; set; } = new();
}

public class ImportarClientesErrorDto
{
    public int Fila { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}