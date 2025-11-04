using System.Collections.Generic;

namespace GestionInventarioBC.Application.DTOs.OperacionesMasivas
{
    public class PrevalidarImportacionStockDto
    {
        public List<PrevalidarImportacionStockLineaDto> Lineas { get; set; } = new();
    }

    public class PrevalidarImportacionStockLineaDto
    {
        public string Sku { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
    }

    public class PrevalidarImportacionStockErrorDto
    {
        public int Index { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
    }

    public class PrevalidarImportacionStockResultDto
    {
        public int Total { get; set; }
        public int ConErrores { get; set; }
        public List<PrevalidarImportacionStockErrorDto> Errores { get; set; } = new();
    }
}
