using System;

namespace GestionInventarioBC.Application.DTOs.Almacen
{
    public class DeshabilitarAlmacenDto
    {
        public Guid EstablecimientoId { get; set; }
        public Guid AlmacenId { get; set; }
    }

    public class DeshabilitarAlmacenResultDto
    {
        public Guid EstablecimientoId { get; set; }
        public Guid AlmacenId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public int Version { get; set; }
    }
}
