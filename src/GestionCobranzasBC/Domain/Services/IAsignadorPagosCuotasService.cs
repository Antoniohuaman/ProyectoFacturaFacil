using System.Collections.Generic;
using GestionCobranzasBC.Domain.Entities;
using GestionCobranzasBC.Domain.ValueObjects;

namespace GestionCobranzasBC.Domain.Services;

/// <summary>
/// Servicio de dominio encargado de aplicar un pago sobre las cuotas
/// de una cuenta por cobrar, respetando la política de aplicación de pagos.
/// </summary>
public interface IAsignadorPagosCuotasService
{
    /// <summary>
    /// Aplica la distribución de pago sobre las cuotas indicadas.
    /// No muta la colección original; devuelve una nueva lista con los cambios.
    /// </summary>
    /// <param name="cuotas">Cuotas actuales de la cuenta.</param>
    /// <param name="distribuciones">Distribución del pago por cuota.</param>
    /// <param name="tolerancia">Tolerancia de redondeo del negocio.</param>
    /// <returns>Colección de cuotas actualizadas.</returns>
    IReadOnlyList<CuotaCredito> AplicarDistribucionPago(
        IReadOnlyList<CuotaCredito> cuotas,
        IReadOnlyList<DistribucionCuota> distribuciones,
        ToleranciaRedondeo tolerancia);
}
