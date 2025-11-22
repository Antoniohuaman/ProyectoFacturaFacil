using System;

namespace ListaPreciosBC.Application.DTOs
{
    /// <summary>
    /// DTO para listar paquetes en la tarjeta/resumen (vista de grilla).
    /// </summary>
    public sealed class PaqueteResumenDto
    {
        public Guid PaqueteId { get; init; }
        public string Nombre { get; init; } = string.Empty;
        public string? Descripcion { get; init; }
        public decimal DescuentoPorcentaje { get; init; }
        public decimal Subtotal { get; init; }
        public decimal Total { get; init; }
        public DateTime FechaCreacionUtc { get; init; }
    }
}
