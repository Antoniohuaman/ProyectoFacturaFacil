using System;

namespace GestionInventarioBC.Application.DTOs.Almacen
{
    public class ActualizarAlmacenDto
    {
        public Guid EstablecimientoId { get; set; }
        public Guid AlmacenId { get; set; }
        public string NuevoNombre { get; set; } = string.Empty;
    }

    public class ActualizarAlmacenResultDto
    {
        public Guid EstablecimientoId { get; set; }
        public Guid AlmacenId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public int Version { get; set; }
    }
}
