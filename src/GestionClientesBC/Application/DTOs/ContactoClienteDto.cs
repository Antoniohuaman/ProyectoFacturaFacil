using System;

namespace GestionClientesBC.Application.DTOs
{
    /// <summary>
    /// DTO para exponer información de un contacto secundario de cliente.
    /// </summary>
    public class ContactoClienteDto
    {
        public Guid ContactoId { get; set; }
        public string Tipo { get; set; } = string.Empty; // Puede ser string o enum según tu API
        public string Valor { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }
    }
}