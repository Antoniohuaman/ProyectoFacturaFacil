using System;
using GestionCobranzasBC.Domain.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace GestionCobranzasBC.Domain.Entities;

/// <summary>
/// Representa una cuota del cronograma de crédito asociada a una cuenta por cobrar.
/// </summary>
public sealed class CuotaCredito
{
    public int NumeroCuota { get; }
    public DateOnly FechaVencimiento { get; }
    public Dinero ImporteProgramado { get; private set; }
    public Dinero MontoPagado { get; private set; }
    public Dinero Saldo => ImporteProgramado - MontoPagado;

    private CuotaCredito(int numeroCuota, DateOnly fechaVencimiento, Dinero importeProgramado, Dinero montoPagado)
    {
        NumeroCuota = numeroCuota;
        FechaVencimiento = fechaVencimiento;
        ImporteProgramado = importeProgramado;
        MontoPagado = montoPagado;
    }

    public static CuotaCredito Crear(int numeroCuota, DateOnly fechaVencimiento, Dinero importeProgramado)
    {
        if (numeroCuota <= 0)
        {
            throw new BusinessRuleException("El número de cuota debe ser mayor que cero.");
        }

        if (importeProgramado is null)
        {
            throw new BusinessRuleException("El importe programado de la cuota es obligatorio.");
        }

        if (importeProgramado.Monto <= 0m)
        {
            throw new BusinessRuleException("El importe programado debe ser mayor que cero.");
        }

        return new CuotaCredito(
            numeroCuota,
            fechaVencimiento,
            importeProgramado,
            Dinero.Create(0m, importeProgramado.Moneda));
    }

    public void AplicarPago(Dinero monto, ToleranciaRedondeo tolerancia)
    {
        if (monto is null)
        {
            throw new BusinessRuleException("El monto aplicado no puede ser nulo.");
        }

        if (tolerancia is null)
        {
            throw new ArgumentNullException(nameof(tolerancia));
        }

        if (monto.Moneda != ImporteProgramado.Moneda)
        {
            throw new BusinessRuleException("La moneda del pago debe coincidir con la de la cuota.");
        }

        if (monto.Monto <= 0m)
        {
            throw new BusinessRuleException("El monto aplicado debe ser mayor que cero.");
        }

        var nuevoMontoPagado = MontoPagado + monto;
        var nuevoSaldo = ImporteProgramado - nuevoMontoPagado;

        if (nuevoSaldo.Monto < 0m && !tolerancia.EstaDentro(nuevoSaldo.Monto))
        {
            throw new BusinessRuleException("El pago excede el saldo permitido para la cuota.");
        }

        if (nuevoSaldo.Monto < 0m && tolerancia.EstaDentro(nuevoSaldo.Monto))
        {
            MontoPagado = ImporteProgramado;
            return;
        }

        MontoPagado = nuevoMontoPagado;
    }

    public bool EstaCancelada(ToleranciaRedondeo tolerancia)
    {
        if (tolerancia is null) throw new ArgumentNullException(nameof(tolerancia));
        return Saldo.Monto <= 0m || tolerancia.EstaDentro(Saldo.Monto);
    }

    public CuotaCredito Clonar()
        => new(NumeroCuota, FechaVencimiento, ImporteProgramado, MontoPagado);
}
