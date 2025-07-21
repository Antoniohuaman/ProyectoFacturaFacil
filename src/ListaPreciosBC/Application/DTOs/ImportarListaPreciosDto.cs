using System.Collections.Generic;

namespace ListaPreciosBC.Application.DTOs
{
    public class ImportarListaPreciosDto
    {
        public int Exitos { get; set; } = 0;
        public int Omitidos { get; set; } = 0;
        public List<string> Errores { get; set; } = new();
    }
}