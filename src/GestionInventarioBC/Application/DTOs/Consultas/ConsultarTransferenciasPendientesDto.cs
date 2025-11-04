using System;
using System.Collections.Generic;

namespace GestionInventarioBC.Application.DTOs.Consultas
{
    public class ConsultarTransferenciasPendientesDto
    {
        public Guid? OrigenEstablecimientoId { get; set; }
        public Guid? OrigenAlmacenId { get; set; }
        public Guid? DestinoEstablecimientoId { get; set; }
        public Guid? DestinoAlmacenId { get; set; }
    }

    public class ConsultarTransferenciasPendientesItemDto
    {
        public Guid TransferenciaId { get; set; }
        public Guid OrigenEstablecimientoId { get; set; }
        public Guid OrigenAlmacenId { get; set; }
        public Guid DestinoEstablecimientoId { get; set; }
        public Guid DestinoAlmacenId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public DateTimeOffset CreadoEn { get; set; }
    }

    public class ConsultarTransferenciasPendientesResultDto
    {
        public List<ConsultarTransferenciasPendientesItemDto> Transferencias { get; set; } = new();
    }
}
