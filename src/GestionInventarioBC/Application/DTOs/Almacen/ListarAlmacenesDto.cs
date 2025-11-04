using System;
using System.Collections.Generic;

namespace GestionInventarioBC.Application.DTOs.Almacen
{
    public class ListarAlmacenesDto
    {
        public Guid EstablecimientoId { get; set; }
    }

    public class ListarAlmacenesItemDto
    {
        public Guid EstablecimientoId { get; set; }
        public Guid AlmacenId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public int Version { get; set; }
    }

    public class ListarAlmacenesResultDto
    {
        public List<ListarAlmacenesItemDto> Almacenes { get; set; } = new();
    }
}
