namespace GestionClientesBC.Application.DTOs
{
    public class EditarClienteDto
    {
        public Guid ClienteId { get; set; }
        public string? TipoDocumento { get; set; }
        public string? NumeroDocumento { get; set; }
        public string? RazonSocialONombres { get; set; }
        public string? Correo { get; set; }
        public string? Celular { get; set; }
        public string? DireccionPostal { get; set; }
        public string? TipoCliente { get; set; }
        // Puedes agregar más campos si lo necesitas
    }
}