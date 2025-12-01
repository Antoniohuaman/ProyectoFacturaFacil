using GestionCobranzasBC.Domain.ValueObjects;
using SharedKernel.Exceptions;

namespace GestionCobranzasBC.Domain.Entities;

/// <summary>
/// Línea de cobro dentro de una Cobranza (documento C1).
/// Descompone la cobranza en método de pago, importe y referencia.
/// </summary>
public sealed class LineaCobro
{
    public int NumeroLinea { get; private set; }
    public MedioPagoCobranza MedioPago { get; private set; }
    public decimal Importe { get; private set; }
    public string? ReferenciaOperacion { get; private set; }
    public CajaDestino CajaDestino { get; private set; }

    private LineaCobro(
        int numeroLinea,
        MedioPagoCobranza medioPago,
        decimal importe,
        CajaDestino cajaDestino,
        string? referenciaOperacion)
    {
        NumeroLinea = numeroLinea;
        MedioPago = medioPago;
        Importe = importe;
        CajaDestino = cajaDestino;
        ReferenciaOperacion = string.IsNullOrWhiteSpace(referenciaOperacion)
            ? null
            : referenciaOperacion.Trim();
    }

    public static LineaCobro Crear(
        int numeroLinea,
        MedioPagoCobranza medioPago,
        decimal importe,
        CajaDestino cajaDestino,
        string? referenciaOperacion)
    {
        if (numeroLinea <= 0)
        {
            throw new BusinessRuleException("El número de línea debe ser mayor que cero.");
        }

        if (medioPago is null)
        {
            throw new BusinessRuleException("El medio de pago es obligatorio.");
        }

        if (cajaDestino is null)
        {
            throw new BusinessRuleException("La caja destino es obligatoria.");
        }

        if (importe <= 0m)
        {
            throw new BusinessRuleException("El importe de la línea de cobro debe ser mayor que cero.");
        }

        return new LineaCobro(numeroLinea, medioPago, importe, cajaDestino, referenciaOperacion);
    }

    public void ActualizarImporte(decimal nuevoImporte)
    {
        if (nuevoImporte <= 0m)
        {
            throw new BusinessRuleException("El importe de la línea de cobro debe ser mayor que cero.");
        }

        Importe = nuevoImporte;
    }

    public void ActualizarReferencia(string? nuevaReferencia)
    {
        ReferenciaOperacion = string.IsNullOrWhiteSpace(nuevaReferencia)
            ? null
            : nuevaReferencia.Trim();
    }
}
