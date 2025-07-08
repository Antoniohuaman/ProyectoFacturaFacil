using System;
using ListaPreciosBC.Domain.ValueObjects;

namespace ListaPreciosBC.Application.DTOs
{
    public class AgregarPrecioEspecificoDto
    {
        public Guid ListaPrecioId { get; set; }
        public decimal Valor { get; set; }
        public Moneda Moneda { get; set; }
        public Prioridad Prioridad { get; set; } = default!;
        public PeriodoVigencia Vigencia { get; set; } = default!;
        public RangoVolumen? RangoVolumen { get; set; }
        public string UsuarioId { get; set; } = default!;
    }
}