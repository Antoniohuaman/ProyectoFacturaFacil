using GestionClientesBC.Domain.ValueObjects;
public class ConsultarHistorialOperacionesDto
{
    public Guid ClienteId { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    public TipoOperacion? TipoOperacion { get; set; }
    public int? Page { get; set; } // Para paginación
    public int? PageSize { get; set; } // Para paginación
}