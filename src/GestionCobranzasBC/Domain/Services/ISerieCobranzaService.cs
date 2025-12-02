using System;
using System.Threading;
using System.Threading.Tasks;
using SharedKernel.ValueObjects;

namespace GestionCobranzasBC.Domain.Services;

/// <summary>
/// Servicio de integración con el BC de Configuración para obtener y consumir
/// series de documentos de cobranza (C1 - Cobranza).
/// 
/// Expone value objects compartidos (EmpresaId/EstablecimientoId) para
/// mantener la consistencia multiempresa sin acoplarse a otros BCs.
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
        EmpresaId empresaId,
        EstablecimientoId establecimientoId,
        CancellationToken cancellationToken = default);
}
