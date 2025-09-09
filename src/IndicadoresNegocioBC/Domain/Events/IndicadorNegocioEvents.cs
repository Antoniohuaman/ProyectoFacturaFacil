#nullable enable
using System;
using System.Collections.Generic;
using SharedKernel.Events;
using SharedKernel.ValueObjects;
using IndicadoresNegocioBC.Domain.ValueObjects;

namespace IndicadoresNegocioBC.Domain.Events
{
    /// <summary>
    /// Contenedor de eventos de dominio relacionados al agregado <see cref="Aggregates.IndicadorNegocio"/>.
    /// Todos los eventos heredan de <see cref="DomainEvent"/> del Shared Kernel.
    /// </summary>
    public static class IndicadorNegocioEvents
    {
        /// <summary>
        /// Evento emitido al crear un IndicadorNegocio (fotografía inicial de KPIs).
        /// </summary>
        public sealed class IndicadorNegocioCreado : DomainEvent
        {
            public Guid IndicadorId { get; }
            public Aggregates.IndicadorNegocio.TipoIndicador Tipo { get; }
            public Periodo Periodo { get; }
            public SegmentoIndicador Segmento { get; }
            public DateTimeOffset CreadoEn { get; }
            public int Version { get; }

            public IndicadorNegocioCreado(
                Guid indicadorId,
                Aggregates.IndicadorNegocio.TipoIndicador tipo,
                Periodo periodo,
                SegmentoIndicador segmento,
                DateTimeOffset creadoEn,
                int version,
                Guid? eventId = null,
                DateTime? occurredOnUtc = null)
                : base(eventId, occurredOnUtc)
            {
                IndicadorId = indicadorId;
                Tipo = tipo ?? throw new ArgumentNullException(nameof(tipo));
                Periodo = periodo ?? throw new ArgumentNullException(nameof(periodo));
                Segmento = segmento ?? throw new ArgumentNullException(nameof(segmento));
                CreadoEn = creadoEn;
                Version = version;
            }
        }

        /// <summary>
        /// Evento emitido al registrar (aplicar) una venta aceptada en el indicador.
        /// </summary>
        public sealed class VentaAceptadaRegistrada : DomainEvent
        {
            public Guid IndicadorId { get; }
            public Guid ComprobanteId { get; }
            public DateOnly Fecha { get; }
            public Guid? ClienteId { get; }
            public Dinero Total { get; }
            public Dinero Igv { get; }
            public IReadOnlyList<VentaItemEventData> Items { get; }
            public UsuarioId? VendedorId { get; }
            public string TipoComprobante { get; } // normalizado (Trim().ToUpperInvariant())
            public EstablecimientoId EstablecimientoId { get; }
            public int Version { get; }

            public VentaAceptadaRegistrada(
                Guid indicadorId,
                Guid comprobanteId,
                DateOnly fecha,
                Guid? clienteId,
                Dinero total,
                Dinero igv,
                IReadOnlyList<VentaItemEventData> items,
                UsuarioId? vendedorId,
                string tipoComprobante,
                EstablecimientoId establecimientoId,
                int version,
                Guid? eventId = null,
                DateTime? occurredOnUtc = null)
                : base(eventId, occurredOnUtc)
            {
                IndicadorId = indicadorId;
                ComprobanteId = comprobanteId;
                Fecha = fecha;
                ClienteId = clienteId;
                Total = total ?? throw new ArgumentNullException(nameof(total));
                Igv = igv ?? throw new ArgumentNullException(nameof(igv));
                Items = items ?? throw new ArgumentNullException(nameof(items));
                VendedorId = vendedorId;
                TipoComprobante = string.IsNullOrWhiteSpace(tipoComprobante)
                    ? throw new ArgumentNullException(nameof(tipoComprobante))
                    : tipoComprobante;
                EstablecimientoId = establecimientoId ?? throw new ArgumentNullException(nameof(establecimientoId));
                Version = version;
            }
        }

        /// <summary>
        /// Evento emitido al registrar la anulación de una venta previamente aplicada.
        /// </summary>
        public sealed class AnulacionRegistrada : DomainEvent
        {
            public Guid IndicadorId { get; }
            public Guid ComprobanteId { get; }
            public DateOnly Fecha { get; }
            public Guid? ClienteId { get; }
            public Dinero Total { get; }
            public Dinero Igv { get; }
            public IReadOnlyList<VentaItemEventData> Items { get; }
            public UsuarioId? VendedorId { get; }
            public string TipoComprobante { get; }
            public EstablecimientoId EstablecimientoId { get; }
            public int Version { get; }

            public AnulacionRegistrada(
                Guid indicadorId,
                Guid comprobanteId,
                DateOnly fecha,
                Guid? clienteId,
                Dinero total,
                Dinero igv,
                IReadOnlyList<VentaItemEventData> items,
                UsuarioId? vendedorId,
                string tipoComprobante,
                EstablecimientoId establecimientoId,
                int version,
                Guid? eventId = null,
                DateTime? occurredOnUtc = null)
                : base(eventId, occurredOnUtc)
            {
                IndicadorId = indicadorId;
                ComprobanteId = comprobanteId;
                Fecha = fecha;
                ClienteId = clienteId;
                Total = total ?? throw new ArgumentNullException(nameof(total));
                Igv = igv ?? throw new ArgumentNullException(nameof(igv));
                Items = items ?? throw new ArgumentNullException(nameof(items));
                VendedorId = vendedorId;
                TipoComprobante = string.IsNullOrWhiteSpace(tipoComprobante)
                    ? throw new ArgumentNullException(nameof(tipoComprobante))
                    : tipoComprobante;
                EstablecimientoId = establecimientoId ?? throw new ArgumentNullException(nameof(establecimientoId));
                Version = version;
            }
        }

        /// <summary>
        /// Evento emitido cuando el indicador transiciona a ACTUALIZADO (primera mutación aplicada en el periodo).
        /// </summary>
        public sealed class IndicadorNegocioActualizado : DomainEvent
        {
            public Guid IndicadorId { get; }
            public EstadoIndicador EstadoAnterior { get; }
            public EstadoIndicador EstadoNuevo { get; }
            public int Version { get; }

            public IndicadorNegocioActualizado(
                Guid indicadorId,
                EstadoIndicador estadoAnterior,
                EstadoIndicador estadoNuevo,
                int version,
                Guid? eventId = null,
                DateTime? occurredOnUtc = null)
                : base(eventId, occurredOnUtc)
            {
                IndicadorId = indicadorId;
                EstadoAnterior = estadoAnterior ?? throw new ArgumentNullException(nameof(estadoAnterior));
                EstadoNuevo = estadoNuevo ?? throw new ArgumentNullException(nameof(estadoNuevo));
                Version = version;
            }
        }

        /// <summary>
        /// Evento emitido al consolidar el periodo: bloquea nuevas mutaciones.
        /// </summary>
        public sealed class IndicadorNegocioConsolidado : DomainEvent
        {
            public Guid IndicadorId { get; }
            public Periodo Periodo { get; }
            public DateTimeOffset ConsolidadoEn { get; }
            public int Version { get; }

            public IndicadorNegocioConsolidado(
                Guid indicadorId,
                Periodo periodo,
                DateTimeOffset consolidadoEn,
                int version,
                Guid? eventId = null,
                DateTime? occurredOnUtc = null)
                : base(eventId, occurredOnUtc)
            {
                IndicadorId = indicadorId;
                Periodo = periodo ?? throw new ArgumentNullException(nameof(periodo));
                ConsolidadoEn = consolidadoEn;
                Version = version;
            }
        }

        // ------------------------- Tipos auxiliares de evento -------------------------

        /// <summary>
        /// Datos mínimos de ítem de venta transportados en eventos (para no acoplar a tipos internos del agregado).
        /// </summary>
        public sealed record VentaItemEventData(string ProductoId, decimal Cantidad, Dinero Subtotal);
    }
}
