namespace GestionInventarioBC.Application.DTOs.Politicas
{
    public class ConfigurarRangoStockDto
    {
        public decimal Minimo { get; set; }
        public decimal Maximo { get; set; }
    }

    public class ConfigurarRangoStockResultDto
    {
        public decimal Minimo { get; set; }
        public decimal Maximo { get; set; }
    }
}
