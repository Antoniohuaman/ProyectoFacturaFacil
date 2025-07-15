using System.Collections.Generic;

namespace GestionClientesBC.Application.DTOs
{
    /// <summary>
    /// DTO principal para exponer la ficha completa de un cliente.
    /// </summary>
    public class ConsultarClienteDto
    {
        public ClienteDto Cliente { get; set; } = new ClienteDto();
        public List<ContactoClienteDto> Contactos { get; set; } = new();
        public List<AdjuntoClienteDto> Adjuntos { get; set; } = new();
        public List<OperacionClienteDto> Historial { get; set; } = new();
    }
}