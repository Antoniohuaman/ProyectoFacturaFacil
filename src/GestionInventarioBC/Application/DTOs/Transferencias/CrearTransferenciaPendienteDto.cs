using System;

namespace GestionInventarioBC.Application.DTOs.Transferencias
{
    public class CrearTransferenciaPendienteDto
    {
        public Guid OrigenEstablecimientoId { get; set; }
        public Guid OrigenAlmacenId { get; set; }
        public Guid DestinoEstablecimientoId { get; set; }
        public Guid DestinoAlmacenId { get; set; }
        public string? Sku { get; set; }
        public Guid? ProductoId { get; set; }
        public decimal Cantidad { get; set; }
    }

    public class CrearTransferenciaPendienteResultDto
    {
        public Guid TransferenciaId { get; set; }
    }
}
