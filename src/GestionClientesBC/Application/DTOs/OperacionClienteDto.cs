using System;
using GestionClientesBC.Domain.Entities;

namespace GestionClientesBC.Application.DTOs
{
    public class OperacionClienteDto
    {
        public Guid OperacionId { get; set; }
        public string TipoOperacion { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public string Referencia { get; set; } = string.Empty;
        public DateTime FechaOperacion { get; set; }
        public bool EstaPendiente { get; set; }
        public bool EsCriticaParaRetencion { get; set; }

        public static OperacionClienteDto FromEntity(OperacionCliente entity)
        {
            return new OperacionClienteDto
            {
                OperacionId = entity.OperacionId,
                TipoOperacion = entity.TipoOperacion.ToString(),
                Monto = entity.MontoOperacion.Value, // Ajusta si tu value object tiene otro nombre
                Referencia = entity.ReferenciaId.ToString(),
                FechaOperacion = entity.FechaOperacion,
                EstaPendiente = entity.EstaPendiente,
                EsCriticaParaRetencion = entity.EsCriticaParaRetencion
            };
        }
    }
}