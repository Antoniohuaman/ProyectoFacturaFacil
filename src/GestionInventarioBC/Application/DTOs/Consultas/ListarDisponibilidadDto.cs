using System;
using System.Collections.Generic;

namespace GestionInventarioBC.Application.DTOs.Consultas
{
    public class ListarDisponibilidadDto
    {
        public Guid EstablecimientoId { get; set; }
        public Guid AlmacenId { get; set; }
        public string? FiltroSku { get; set; }
        public bool SoloConDisponible { get; set; }
    }

    public class ListarDisponibilidadItemDto
    {
        public string Sku { get; set; } = string.Empty;
        public decimal Real { get; set; }
        public decimal Reservado { get; set; }
        public decimal Disponible { get; set; }
    }

    public class ListarDisponibilidadResultDto
    {
        public List<ListarDisponibilidadItemDto> Items { get; set; } = new();
    }
}
