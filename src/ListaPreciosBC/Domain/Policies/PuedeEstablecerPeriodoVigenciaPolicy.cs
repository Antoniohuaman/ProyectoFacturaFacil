using ListaPreciosBC.Domain.ValueObjects;
using System;

namespace ListaPreciosBC.Domain.Policies
{
    /// <summary>
    /// Policy para validar si el periodo de vigencia es válido.
    /// Reglas:
    /// - Desde debe ser menor o igual que Hasta (si existe).
    /// </summary>
    public static class PuedeEstablecerPeriodoVigenciaPolicy
    {
        public static bool Validar(DateTime desde, DateTime? hasta)
        {
            return PeriodoVigencia.TryCrear(desde, hasta, out var _);
        }
    }
}