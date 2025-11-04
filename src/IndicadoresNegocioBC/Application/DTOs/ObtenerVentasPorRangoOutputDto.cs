using System;
using System.Collections.Generic;
using IndicadoresNegocioBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace IndicadoresNegocioBC.Application.UseCases.IndicadorNegocio
{
    /// <summary>
    /// DTO de salida de "ObtenerVentasPorRango".
    /// Incluye metadatos del indicador, el rango consultado, la lista de ventas y un resumen del rango.
    /// </summary>
    public sealed class ObtenerVentasPorRangoOutputDto
    {
        public Guid IndicadorId { get; }
        public Domain.Aggregates.IndicadorNegocio.TipoIndicador Tipo { get; }
        public Periodo Periodo { get; }
        public SegmentoIndicador Segmento { get; }
        public DateOnly Desde { get; }
        public DateOnly Hasta { get; }
        public IReadOnlyList<VentaItem> Ventas { get; }
        public Dinero TotalVentasRango { get; }
        public Dinero TotalIgvRango { get; }
        public int TotalComprobantesRango { get; }

        public ObtenerVentasPorRangoOutputDto(
            Guid indicadorId,
            Domain.Aggregates.IndicadorNegocio.TipoIndicador tipo,
            Periodo periodo,
            SegmentoIndicador segmento,
            DateOnly desde,
            DateOnly hasta,
            IReadOnlyList<VentaItem> ventas,
            Dinero totalVentasRango,
            Dinero totalIgvRango,
            int totalComprobantesRango)
        {
            IndicadorId = indicadorId;
            Tipo = tipo ?? throw new ArgumentNullException(nameof(tipo));
            Periodo = periodo ?? throw new ArgumentNullException(nameof(periodo));
            Segmento = segmento ?? throw new ArgumentNullException(nameof(segmento));
            Desde = desde;
            Hasta = hasta;
            Ventas = ventas ?? throw new ArgumentNullException(nameof(ventas));
            TotalVentasRango = totalVentasRango ?? throw new ArgumentNullException(nameof(totalVentasRango));
            TotalIgvRango = totalIgvRango ?? throw new ArgumentNullException(nameof(totalIgvRango));
            TotalComprobantesRango = totalComprobantesRango;
        }

        public sealed class VentaItem
        {
            public Guid ComprobanteId { get; }
            public DateOnly Fecha { get; }
            public Guid? ClienteId { get; }
            public Dinero Total { get; }
            public Dinero Igv { get; }
            public string TipoComprobante { get; }
            public UsuarioId? VendedorId { get; }
            public EstablecimientoId EstablecimientoId { get; }
            public IReadOnlyList<Item> Items { get; }

            public VentaItem(
                Guid comprobanteId,
                DateOnly fecha,
                Guid? clienteId,
                Dinero total,
                Dinero igv,
                string tipoComprobante,
                UsuarioId? vendedorId,
                EstablecimientoId establecimientoId,
                IReadOnlyList<Item> items)
            {
                if (string.IsNullOrWhiteSpace(tipoComprobante))
                    throw new ArgumentNullException(nameof(tipoComprobante));

                ComprobanteId = comprobanteId;
                Fecha = fecha;
                ClienteId = clienteId;
                Total = total ?? throw new ArgumentNullException(nameof(total));
                Igv = igv ?? throw new ArgumentNullException(nameof(igv));
                TipoComprobante = tipoComprobante;
                VendedorId = vendedorId;
                EstablecimientoId = establecimientoId ?? throw new ArgumentNullException(nameof(establecimientoId));
                Items = items ?? throw new ArgumentNullException(nameof(items));
            }

            public sealed class Item
            {
                public Guid ProductoId { get; }
                public decimal Cantidad { get; }
                public Dinero Subtotal { get; }

                public Item(Guid productoId, decimal cantidad, Dinero subtotal)
                {
                    if (productoId == Guid.Empty)
                        throw new ArgumentException("ProductoId requerido.", nameof(productoId));
                    if (cantidad <= 0m)
                        throw new ArgumentOutOfRangeException(nameof(cantidad), "Cantidad debe ser > 0.");

                    ProductoId = productoId;
                    Cantidad = cantidad;
                    Subtotal = subtotal ?? throw new ArgumentNullException(nameof(subtotal));
                }
            }
        }
    }
}
