using System;
using System.Collections.Generic;

namespace GestionInventarioBC.Application.DTOs.OperacionesMasivas
{
    public class ImportarStockMasivoDto
    {
        public Guid EstablecimientoId { get; set; }
        public Guid AlmacenId { get; set; }
        public List<ImportarStockMasivoLineaDto> Lineas { get; set; } = new();
    }

    public class ImportarStockMasivoLineaDto
    {
        public string Sku { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
    }

    public class ImportarStockMasivoResultDto
    {
        public int Procesados { get; set; }
    }
}
