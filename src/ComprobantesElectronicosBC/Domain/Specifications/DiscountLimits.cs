using System;

namespace ComprobantesElectronicosBC.Domain.Specifications
{
    /// <summary>
    /// Constantes relacionadas con límites de descuento usadas por las specifications.
    /// - Se define como exclusivo: 100 significa que 100% no es válido como "descuento"
    ///   (en caso de necesitar una transferencia a afectación gratuita, debe modelarse explícitamente).
    /// </summary>
    public static class DiscountLimits
    {
        /// <summary>Porcentaje máximo permitido EXCLUSIVO para descuentos (100 = no permitido).</summary>
        public const decimal MaxPercentAllowedExclusive = 100m;
    }
}
