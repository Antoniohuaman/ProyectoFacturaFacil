using System;
using ListaPreciosBC.Domain.ValueObjects;
using ListaPreciosBC.Domain.Entities;

namespace ListaPreciosBC.Application.DTOs
{
    public class ConfigurarListaPrecioDto
    {
        public TipoLista TipoLista { get; set; }
        public CriterioLista Criterio { get; set; } = default!;
        public Moneda Moneda { get; set; }
        public Prioridad Prioridad { get; set; } = default!;
        public PeriodoVigencia Vigencia { get; set; } = default!;
        public string UsuarioId { get; set; } = default!;
    }
}