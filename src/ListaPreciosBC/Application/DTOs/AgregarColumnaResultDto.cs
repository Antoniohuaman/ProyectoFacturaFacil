using System;
using ListaPreciosBC.Domain.ValueObjects;

namespace ListaPreciosBC.Application.DTOs
{
    /// <summary>
    /// DTO de salida para el resultado de agregar una columna.
    /// </summary>
    public class AgregarColumnaResultDto
    {
        public Guid ListaPrecioId { get; set; }
        public IdentificadorColumnaPrecio IdColumna { get; set; } = null!;
        public NombreColumnaPrecio Nombre { get; set; } = null!;
        public byte Orden { get; set; }
        public bool EsBase { get; set; }
        public bool Visible { get; set; }
        public ModoValorizacionColumna Modo { get; set; } = ModoValorizacionColumna.Fijo;
    }
}
