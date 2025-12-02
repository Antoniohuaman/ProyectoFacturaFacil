using System;
using System.Collections.Generic;
using System.Linq;
using GestionCobranzasBC.Domain.Entities;
using GestionCobranzasBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace GestionCobranzasBC.Domain.Services;

/// <summary>
/// Servicio que calcula el saldo pendiente y el estado de una cuenta por cobrar
/// en función de sus cuotas.
/// </summary>
public sealed class CalculadoraEstadoCuentaService
{
    public SaldoPendiente CalcularSaldo(
        IReadOnlyList<CuotaCredito> cuotas,
        ToleranciaRedondeo tolerancia)
    {
        if (cuotas is null) throw new ArgumentNullException(nameof(cuotas));
        if (tolerancia is null) throw new ArgumentNullException(nameof(tolerancia));

        var moneda = cuotas.First().ImporteProgramado.Moneda;

        var total = cuotas.Aggregate(
            Dinero.Create(0m, moneda),
            (acc, cuota) => acc + cuota.ImporteProgramado);

        var cobrado = cuotas.Aggregate(
            Dinero.Create(0m, moneda),
            (acc, cuota) => acc + cuota.MontoPagado);

        var saldo = total - cobrado;

        return SaldoPendiente.Crear(
            total,
            cobrado,
            saldo,
            tolerancia);
    }

    public EstadoCuentaPorCobrar CalcularEstado(
        SaldoPendiente saldo,
        IReadOnlyList<CuotaCredito> cuotas,
        DateOnly fechaActual)
    {
        if (saldo is null) throw new ArgumentNullException(nameof(saldo));
        if (cuotas is null) throw new ArgumentNullException(nameof(cuotas));

        if (saldo.EsCancelado)
        {
            return EstadoCuentaPorCobrar.Cancelado;
        }

        var algunaVencidaConSaldo =
            cuotas.Any(c =>
                c.Saldo.Monto > 0m &&
                c.FechaVencimiento < fechaActual);

        if (algunaVencidaConSaldo)
        {
            return EstadoCuentaPorCobrar.Vencido;
        }

        return saldo.Saldo.Monto < saldo.Total.Monto
            ? EstadoCuentaPorCobrar.Parcial
            : EstadoCuentaPorCobrar.Pendiente;
    }
}
