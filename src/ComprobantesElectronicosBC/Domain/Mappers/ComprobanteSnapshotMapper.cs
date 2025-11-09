using System;
using System.Collections.Generic;
using System.Linq;
using ComprobantesElectronicosBC.Domain.Aggregates;
using ComprobantesElectronicosBC.Domain.Entities;
using ComprobantesElectronicosBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace ComprobantesElectronicosBC.Domain.Mappers
{
    /// <summary>
    /// Snapshot inmutable derivado del aggregate <see cref="ComprobanteElectronico"/> para persistencia y salida.
    /// No recalcula nada: sólo lee los valores ya calculados en el agregado.
    /// </summary>
    public sealed record ComprobanteEmitidoSnapshot(
        Guid ComprobanteId,
        EmpresaId EmpresaId,
        EstablecimientoId EstablecimientoId,
        string TipoComprobanteCodigo,
        SerieYNumero SerieNumero,
        DateOnly FechaEmision,
        Moneda Moneda,
        decimal SubtotalBase,
        decimal DescuentoGlobalMonto,
        decimal IgvTotal,
        decimal Total,
        IReadOnlyList<ComprobanteEmitidoSnapshot.LineaSnapshot> Lineas,
        string ClienteEtiqueta,
        string? Observaciones,
        DateTimeOffset EmitidoEnUtc
    )
    {
        public sealed record LineaSnapshot(
            int Numero,
            string Descripcion,
            string UnidadMedida,
            decimal Cantidad,
            decimal UnitPriceSinIgv,
            decimal UnitPriceConIgv,
            string AfectacionCodigo,
            decimal TasaImpuestoFraccion,
            decimal BaseImponible,
            decimal Igv,
            decimal ImporteTotal
        );
    }

    public static class ComprobanteSnapshotMapper
    {
        /// <summary>
        /// Crea el snapshot completo desde el agregado ya emitido.
        /// Precondición: <paramref name="cpe"/> debe estar en estado Enviado y tener SerieNumero.
        /// </summary>
        public static ComprobanteEmitidoSnapshot FromAggregate(ComprobanteElectronico cpe)
        {
            if (cpe is null) throw new ArgumentNullException(nameof(cpe));
            if (cpe.SerieNumero is null)
                throw new InvalidOperationException("El comprobante aún no tiene Serie/Numero asignado.");
            if (cpe.Estado != EstadoComprobante.Enviado && cpe.Estado != EstadoComprobante.Corregir && cpe.Estado != EstadoComprobante.Aceptado && cpe.Estado != EstadoComprobante.Rechazado)
                throw new InvalidOperationException("Se esperaba un comprobante al menos emitido.");

            var lineas = cpe.Lineas.Select(l => new ComprobanteEmitidoSnapshot.LineaSnapshot(
                Numero: l.NumeroLinea,
                Descripcion: l.Descripcion.Nombre, // detalle omitido por simplicidad; se puede concatenar si necesario
                UnidadMedida: l.UM.Codigo,
                Cantidad: l.Cantidad.Value,
                UnitPriceSinIgv: l.UnitPriceSinIgv.Monto,
                UnitPriceConIgv: l.UnitPriceConIgv.Monto,
                AfectacionCodigo: l.AfectacionImpuesto.Codigo,
                TasaImpuestoFraccion: l.TasaImpuesto.Fraccion,
                BaseImponible: l.BaseImponible.Monto,
                Igv: l.Igv.Monto,
                ImporteTotal: l.ImporteTotal.Monto
            )).OrderBy(ls => ls.Numero).ToList();

            var etiquetaCliente = cpe.Cliente.ToString();

            return new ComprobanteEmitidoSnapshot(
                ComprobanteId: cpe.ComprobanteId,
                EmpresaId: cpe.EmpresaId,
                EstablecimientoId: cpe.EstablecimientoId,
                TipoComprobanteCodigo: cpe.Tipo.Codigo,
                SerieNumero: cpe.SerieNumero!,
                FechaEmision: cpe.Emision.Fecha,
                Moneda: cpe.Moneda,
                SubtotalBase: cpe.SubtotalBase,
                DescuentoGlobalMonto: cpe.DescuentoGlobalMonto,
                IgvTotal: cpe.IgvTotal,
                Total: cpe.Total,
                Lineas: lineas,
                ClienteEtiqueta: etiquetaCliente,
                Observaciones: cpe.Observaciones?.Texto,
                EmitidoEnUtc: cpe.EnviadoEnUtc ?? cpe.CreadoEnUtc
            );
        }
    }
}