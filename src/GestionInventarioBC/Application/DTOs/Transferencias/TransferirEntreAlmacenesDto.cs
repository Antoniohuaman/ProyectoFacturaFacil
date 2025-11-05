using System;
using System.Collections.Generic;

namespace GestionInventarioBC.Application.DTOs.Transferencias
{
    public class TransferirEntreAlmacenesDto
    {
        public Guid OrigenEstablecimientoId { get; set; }
        public Guid OrigenAlmacenId { get; set; }
        public Guid DestinoEstablecimientoId { get; set; }
        public Guid DestinoAlmacenId { get; set; }
        public DateTimeOffset? Fecha { get; set; }
        public List<TransferirEntreAlmacenesLineaDto> Lineas { get; set; } = new();
    }

    public class TransferirEntreAlmacenesLineaDto
    {
        public string? Sku { get; set; }
        public Guid? ProductoId { get; set; }
        public decimal Cantidad { get; set; }
    }

    public class TransferirEntreAlmacenesResultDto
    {
        public Guid MovimientoSalidaId { get; set; }
        public Guid MovimientoEntradaId { get; set; }
    }
}
