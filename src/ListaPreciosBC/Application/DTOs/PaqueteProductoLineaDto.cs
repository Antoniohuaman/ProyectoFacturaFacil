using System;
using SharedKernel.ValueObjects;

namespace ListaPreciosBC.Application.DTOs
{
    /// <summary>
    /// Línea de producto que compone un paquete (sin exponer entidades de dominio).
    /// </summary>
    public sealed class PaqueteProductoLineaDto
    {
        public Guid ProductoId { get; init; }
        public string UnidadMedidaCodigo { get; init; } = UnidadDeMedida.NIU.Codigo;
        public int Cantidad { get; init; }
        public decimal PrecioUnitario { get; init; }
    }
}
