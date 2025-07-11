namespace GestionClientesBC.Application.DTOs
{
    public class AutoCrearClienteDesdeComprobanteDto
    {
        public string TipoDocumento { get; set; } = default!;
        public string NumeroDocumento { get; set; } = default!;
        public string? RazonSocialONombres { get; set; }
        public string? DireccionPostal { get; set; }
    }
}