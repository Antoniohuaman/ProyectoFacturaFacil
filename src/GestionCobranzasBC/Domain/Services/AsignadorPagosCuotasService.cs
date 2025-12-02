using System;
using System.Collections.Generic;
using System.Linq;
using GestionCobranzasBC.Domain.Entities;
using GestionCobranzasBC.Domain.Policies;
using GestionCobranzasBC.Domain.ValueObjects;

namespace GestionCobranzasBC.Domain.Services;

/// <summary>
/// Implementación por defecto del servicio de asignación de pagos
/// sobre cuotas de crédito.
/// </summary>
public sealed class AsignadorPagosCuotasService : IAsignadorPagosCuotasService
{
    private readonly PoliticaAplicacionPagos _politicaAplicacionPagos;

    public AsignadorPagosCuotasService(PoliticaAplicacionPagos politicaAplicacionPagos)
    {
        _politicaAplicacionPagos = politicaAplicacionPagos 
            ?? throw new ArgumentNullException(nameof(politicaAplicacionPagos));
    }

    public IReadOnlyList<CuotaCredito> AplicarDistribucionPago(
        IReadOnlyList<CuotaCredito> cuotas,
        IReadOnlyList<DistribucionCuota> distribuciones,
        ToleranciaRedondeo tolerancia)
    {
        if (cuotas is null) throw new ArgumentNullException(nameof(cuotas));
        if (distribuciones is null) throw new ArgumentNullException(nameof(distribuciones));
        if (tolerancia is null) throw new ArgumentNullException(nameof(tolerancia));

        if (!distribuciones.Any())
        {
            // No hay nada que aplicar; devolvemos copia inmutable.
            return cuotas.ToList();
        }

        var cuotasOrdenadas = _politicaAplicacionPagos
            .OrdenarCuotasParaPago(cuotas)
            .ToDictionary(c => c.NumeroCuota, c => c.Clonar()); // asumimos método Clonar en entidad

        foreach (var distribucion in distribuciones)
        {
            if (!cuotasOrdenadas.TryGetValue(distribucion.NumeroCuota, out var cuota))
            {
                // Si la cuota no existe, ignoramos esa distribución; la validación puede hacerse antes.
                continue;
            }

            cuota.AplicarPago(distribucion.Monto, tolerancia);
        }

        return cuotasOrdenadas
            .OrderBy(c => c.Value.FechaVencimiento)
            .ThenBy(c => c.Key)
            .Select(c => c.Value)
            .ToList();
    }
}
