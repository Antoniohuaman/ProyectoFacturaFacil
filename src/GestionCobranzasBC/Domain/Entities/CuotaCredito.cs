using System;
using SharedKernel.Exceptions;

namespace GestionCobranzasBC.Domain.Entities;

/// <summary>
/// Representa una cuota de un cronograma de crédito.
/// No conoce de monedas; sólo maneja montos decimales.
/// La validación de moneda se hace a nivel de agregado/VO de Dinero.
/// </summary>
public sealed class CuotaCredito
{
    public int Numero { get; private set; }
    public DateOnly FechaVencimiento { get; private set; }
    public decimal ImporteOriginal { get; private set; }
    public decimal MontoPagado { get; private set; }

    /// <summary>
    /// Saldo pendiente de la cuota (ImporteOriginal - MontoPagado).
    /// Nunca es negativo; si se intenta pagar de más se lanza excepción.
    /// </summary>
    public decimal Saldo => ImporteOriginal - MontoPagado;

    private CuotaCredito(int numero, DateOnly fechaVencimiento, decimal importeOriginal)
    {
        Numero = numero;
        FechaVencimiento = fechaVencimiento;
        ImporteOriginal = importeOriginal;
        MontoPagado = 0m;
    }

    /// <summary>
    /// Fábrica principal de cuotas.
    /// </summary>
    public static CuotaCredito Crear(int numero, DateOnly fechaVencimiento, decimal importeOriginal)
    {
        if (numero <= 0)
        {
            throw new BusinessRuleException("El número de cuota debe ser mayor que cero.");
        }

        if (importeOriginal <= 0m)
        {
            throw new BusinessRuleException("El importe de la cuota debe ser mayor que cero.");
        }

        return new CuotaCredito(numero, fechaVencimiento, importeOriginal);
    }

    /// <summary>
    /// Registra un pago sobre la cuota.
    /// La tolerancia se expresa en moneda de la cuota (por ejemplo 0.01).
    /// </summary>
    /// <param name="monto">Monto a aplicar a la cuota.</param>
    /// <param name="toleranciaRedondeo">
    /// Diferencia máxima permitida entre el saldo y el monto, para considerar la cuota cancelada.
    /// </param>
    public void RegistrarPago(decimal monto, decimal toleranciaRedondeo)
    {
        if (monto <= 0m)
        {
            throw new BusinessRuleException("El monto del pago debe ser mayor que cero.");
        }

        if (toleranciaRedondeo < 0m)
        {
            throw new BusinessRuleException("La tolerancia de redondeo no puede ser negativa.");
        }

        var nuevoMontoPagado = MontoPagado + monto;
        var nuevoSaldo = ImporteOriginal - nuevoMontoPagado;

        if (nuevoSaldo < -toleranciaRedondeo)
        {
            throw new BusinessRuleException("El pago excede el saldo permitido para la cuota.");
        }

        MontoPagado = nuevoMontoPagado;

        // Normalizamos saldos muy pequeños dentro de la tolerancia a cero.
        if (Saldo < 0m && Saldo > -toleranciaRedondeo)
        {
            MontoPagado = ImporteOriginal;
        }
    }

    /// <summary>
    /// Indica si la cuota está completamente cancelada considerando la tolerancia.
    /// </summary>
    public bool EstaCancelada(decimal toleranciaRedondeo)
    {
        if (toleranciaRedondeo < 0m)
        {
            throw new BusinessRuleException("La tolerancia de redondeo no puede ser negativa.");
        }

        return Saldo <= toleranciaRedondeo;
    }
}
