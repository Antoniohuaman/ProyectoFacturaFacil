using GestionClientesBC.Domain.ValueObjects;
public class RegistrarOperacionClienteDto
{
    public Guid ClienteId { get; set; }
    public TipoOperacion TipoOperacion { get; set; }
    public decimal Monto { get; set; }
    public string Referencia { get; set; } = string.Empty;
    public DateTime FechaOperacion { get; set; }
}