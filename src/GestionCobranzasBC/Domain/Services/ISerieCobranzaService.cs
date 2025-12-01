using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProyectoFacturaFacil.GestionCobranzasBC.Domain.Services;

/// <summary>
/// Servicio de integración con el BC de Configuración para obtener y consumir
/// series de documentos de cobranza (C1 - Cobranza).
/// 
/// Se mantiene deliberadamente en términos primitivos para evitar acoplar
/// tipos de otros bounded contexts dentro de GestionCobranzasBC.
/// </summary>
public interface ISerieCobranzaService
{
    /// <summary>
    /// Obtiene y reserva el siguiente número disponible para un documento
    /// de cobranza en la empresa/establecimiento indicados.
    /// </summary>
    /// <param name="empresaId">Identificador de empresa (tenant).</param>
    /// <param name="establecimientoId">Identificador de establecimiento.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>
    /// Tupla con <c>serie</c> (ej. "CB01") y <c>numero</c> correlativo (ej. 123).
    /// </returns>
    Task<(string Serie, int Numero)> ReservarSiguienteNumeroAsync(
        Guid empresaId,
        Guid establecimientoId,
        CancellationToken cancellationToken = default);
}
