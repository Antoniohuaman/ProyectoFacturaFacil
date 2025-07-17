namespace ListaPreciosBC.Application.DTOs
{
    public class ModificarPrecioEspecificoDto
    {
        public Guid PrecioEspecificoId { get; set; }
        public decimal? NuevoValor { get; set; }
        public string? NuevaMoneda { get; set; }
        public DateTime? NuevaFechaInicio { get; set; }
        public DateTime? NuevaFechaFin { get; set; }
        public string UsuarioId { get; set; } = default!;
        public string Motivo { get; set; } = default!;
    }
}
