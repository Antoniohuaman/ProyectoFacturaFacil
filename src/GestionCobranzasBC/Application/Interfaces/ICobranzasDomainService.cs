// src/GestionCobranzasBC/Application/Interfaces/ICobranzasDomainService.cs
#nullable enable
namespace GestionCobranzasBC.Application.Interfaces;

using System;
using System.Threading;
using System.Threading.Tasks;
using GestionCobranzasBC.Application.DTOs;

public interface ICobranzasDomainService
{
    Task<CuentaPorCobrarDto> GenerarCuentaPorCobrarDesdeComprobanteAsync(
        Guid tenantId,
        Guid empresaId,
        Guid? establecimientoId,
        Guid comprobanteId,
        CancellationToken cancellationToken = default);

    Task<CuentaPorCobrarDto> EmitirSinCobranzaAsync(
        Guid tenantId,
        Guid empresaId,
        Guid? establecimientoId,
        Guid comprobanteId,
        CancellationToken cancellationToken = default);

    Task<CobranzaDto> RegistrarCobranzaInmediataAsync(
        CobranzaDto comando,
        CancellationToken cancellationToken = default);

    Task<CobranzaDto> RegistrarCobranzaParcialAsync(
        CobranzaDto comando,
        CancellationToken cancellationToken = default);

    Task<CobranzaDto> RegistrarCobranzaPorCuotaAsync(
        CobranzaDto comando,
        CancellationToken cancellationToken = default);
}
