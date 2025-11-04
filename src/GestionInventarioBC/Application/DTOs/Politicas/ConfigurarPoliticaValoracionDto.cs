namespace GestionInventarioBC.Application.DTOs.Politicas
{
    public class ConfigurarPoliticaValoracionDto
    {
        public string Metodo { get; set; } = "PromedioPonderado";
    }

    public class ConfigurarPoliticaValoracionResultDto
    {
        public string Metodo { get; set; } = string.Empty;
    }
}
