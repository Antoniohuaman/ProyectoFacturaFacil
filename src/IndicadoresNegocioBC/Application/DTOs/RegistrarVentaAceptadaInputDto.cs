using System;
using System.Collections.Generic;
using IndicadoresNegocioBC.Domain.Aggregates;
using IndicadoresNegocioBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace IndicadoresNegocioBC.Application.UseCases.IndicadorNegocio
{
    /// <summary>
    /// DTO de entrada para registrar una venta aceptada en un IndicadorNegocio.
    /// Clave natural del agregado: Tipo + Periodo + Segmento.
    /// EmpresaId es opcional si el scope multi-tenant lo aísla la infraestructura.
    /// </summary>
    public sealed class RegistrarVentaAceptadaInputDto
    {
        // Clave natural
        public Domain.Aggregates.IndicadorNegocio.TipoIndicador Tipo { get; }
        public Periodo Periodo { get; }
        public SegmentoIndicador Segmento { get; }
        public EmpresaId? EmpresaId { get; }

        // Datos de la venta (provenientes de ComprobantesElectronicosBC)
        public string TipoComprobante { get; }
        public Guid ComprobanteId { get; }
        public DateOnly Fecha { get; }
        public Guid? ClienteId { get; }
        public Dinero Total { get; }
        public Dinero Igv { get; }
        public IReadOnlyList<ItemInput> Items { get; }
        public UsuarioId? VendedorId { get; }
        public EstablecimientoId EstablecimientoId { get; }

        public RegistrarVentaAceptadaInputDto(
            Domain.Aggregates.IndicadorNegocio.TipoIndicador tipo,
            Periodo periodo,
            SegmentoIndicador segmento,
            string tipoComprobante,
            Guid comprobanteId,
            DateOnly fecha,
            Dinero total,
            Dinero igv,
            IEnumerable<ItemInput> items,
            EstablecimientoId establecimientoId,
            Guid? clienteId = null,
            UsuarioId? vendedorId = null,
            EmpresaId? empresaId = null)
        {
            Tipo = tipo ?? throw new ArgumentNullException(nameof(tipo));
            Periodo = periodo ?? throw new ArgumentNullException(nameof(periodo));
            Segmento = segmento ?? throw new ArgumentNullException(nameof(segmento));
            TipoComprobante = string.IsNullOrWhiteSpace(tipoComprobante) ? throw new ArgumentNullException(nameof(tipoComprobante)) : tipoComprobante;
            ComprobanteId = comprobanteId == Guid.Empty ? throw new ArgumentException("ComprobanteId vacío.", nameof(comprobanteId)) : comprobanteId;
            Fecha = fecha;
            Total = total ?? throw new ArgumentNullException(nameof(total));
            Igv = igv ?? throw new ArgumentNullException(nameof(igv));
            Items = items is null ? throw new ArgumentNullException(nameof(items)) : new List<ItemInput>(items);
            if (Items.Count == 0) throw new ArgumentException("Debe incluir al menos un ítem.", nameof(items));
            EstablecimientoId = establecimientoId ?? throw new ArgumentNullException(nameof(establecimientoId));
            ClienteId = clienteId;
            VendedorId = vendedorId;
            EmpresaId = empresaId;
        }

        public sealed class ItemInput
        {
            public string ProductoId { get; }
            public decimal Cantidad { get; }
            public Dinero Subtotal { get; }

            public ItemInput(string productoId, decimal cantidad, Dinero subtotal)
            {
                if (string.IsNullOrWhiteSpace(productoId)) throw new ArgumentException("ProductoId requerido.", nameof(productoId));
                if (cantidad <= 0m) throw new ArgumentOutOfRangeException(nameof(cantidad), "Cantidad debe ser > 0.");
                ProductoId = productoId;
                Cantidad = cantidad;
                Subtotal = subtotal ?? throw new ArgumentNullException(nameof(subtotal));
            }
        }
    }
}
