using System;

namespace ListaPreciosBC.Application.DTOs
{
    public class MensajeNotificacionDto
    {
        public string ListaNombre { get; set; } = string.Empty;
        public string TipoEvento { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
    }
}