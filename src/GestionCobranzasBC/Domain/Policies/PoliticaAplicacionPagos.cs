using System;
using System.Collections.Generic;
using System.Linq;
using ProyectoFacturaFacil.GestionCobranzasBC.Domain.Entities;

namespace ProyectoFacturaFacil.GestionCobranzasBC.Domain.Policies;

/// <summary>
/// Define la política de cómo se deben aplicar los pagos sobre las cuotas.
/// Actualmente aplica un esquema simple: primero las cuotas más antiguas
/// (por fecha de vencimiento) y, a igualdad de fecha, por número de cuota.
/// 
/// Si más adelante necesitas otra estrategia (p.ej. por monto, o por
/// selección manual), se puede extender este componente.
/// </summary>
public sealed class PoliticaAplicacionPagos
{
    /// <summary>
    /// Ordena las cuotas según la política de aplicación de pagos.
    /// </summary>
    /// <param name="cuotas">Colección de cuotas a ordenar.</param>
    /// <returns>Cuotas ordenadas según la política actual.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public IReadOnlyList<CuotaCredito> OrdenarCuotasParaPago(
        IEnumerable<CuotaCredito> cuotas)
    {
        if (cuotas is null)
        {
            throw new ArgumentNullException(nameof(cuotas));
        }

        // Regla actual:
        // 1. Primero las cuotas con fecha de vencimiento más antigua.
        // 2. A igualdad de fecha, por número de cuota.
        return cuotas
            .OrderBy(c => c.FechaVencimiento)
            .ThenBy(c => c.NumeroCuota)
            .ToList();
    }
}
