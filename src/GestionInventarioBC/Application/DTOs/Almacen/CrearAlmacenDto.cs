using System;

namespace GestionInventarioBC.Application.DTOs.Almacen
{
    public class CrearAlmacenDto
    {
        public Guid EstablecimientoId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public Guid? AlmacenId { get; set; }
    }

    public class CrearAlmacenResultDto
    {
        public Guid EstablecimientoId { get; set; }
        public Guid AlmacenId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public int Version { get; set; }
    }
}
