using System.Threading;
using System.Threading.Tasks;
using ProyectoFacturaFacil.GestionCobranzasBC.Domain.Aggregates;

namespace ProyectoFacturaFacil.GestionCobranzasBC.Domain.Services;

/// <summary>
/// Servicio de dominio que coordina la operación de registrar una cobranza
/// sobre una cuenta por cobrar (incluye reglas transversales que no encajan
/// del todo dentro de un único agregado).
/// 
/// La implementación típica orquestará:
/// - Validación de caja disponible
/// - Reserva de serie RC
/// - Aplicación de pagos sobre cuotas
/// - Actualización del estado de la cuenta
/// - Publicación de eventos de dominio
/// </summary>
public interface ICobranzasDomainService
{
    Task RegistrarCobranzaAsync(
        CuentaPorCobrar cuenta,
        Cobranza cobranza,
        CancellationToken cancellationToken = default);
}
