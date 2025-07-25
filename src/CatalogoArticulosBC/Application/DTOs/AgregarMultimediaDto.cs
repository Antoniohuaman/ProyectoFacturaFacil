using System;

namespace CatalogoArticulosBC.Application.DTOs
{
    public class AgregarMultimediaDto
    {
    public Guid MultimediaId { get; set; }
    public string? NombreArchivo { get; set; }
    public string? TipoAdjunto { get; set; }
    public long Tamano { get; set; }
    public string? Comentario { get; set; }
    public DateTime FechaCarga { get; set; }
    }
}