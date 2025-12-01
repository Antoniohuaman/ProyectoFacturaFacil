using System;
using System.Collections.Generic;
using System.Linq;
using ProyectoFacturaFacil.GestionCobranzasBC.Domain.Entities;
using ProyectoFacturaFacil.GestionCobranzasBC.Domain.ValueObjects;

namespace ProyectoFacturaFacil.GestionCobranzasBC.Domain.Services;

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

        var total = cuotas.Aggregate(
            0m,
            (acc, cuota) => acc + cuota.ImporteOriginal.Monto);

        var cobrado = cuotas.Aggregate(
            0m,
            (acc, cuota) => acc + cuota.MontoPagado.Monto);

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

        // Si no está cancelada y no hay vencidas, pero hay saldo > 0,
        // consideramos que está parcial.
        return EstadoCuentaPorCobrar.Parcial;
    }
}
