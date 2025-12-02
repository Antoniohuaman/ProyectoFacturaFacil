// src/GestionCobranzasBC/Application/Interfaces/ICobranzasDomainService.cs
#nullable enable
namespace GestionCobranzasBC.Application.Interfaces;

using System;
using System.Threading;
using System.Threading.Tasks;
using GestionCobranzasBC.Application.DTOs;
using SharedKernel.ValueObjects;

public interface ICobranzasDomainService
{
    Task<CuentaPorCobrarDto> GenerarCuentaPorCobrarDesdeComprobanteAsync(
        TenantId tenantId,
        EmpresaId empresaId,
        EstablecimientoId? establecimientoId,
        Guid comprobanteId,
        CancellationToken cancellationToken = default);

    Task<CuentaPorCobrarDto> EmitirSinCobranzaAsync(
        TenantId tenantId,
        EmpresaId empresaId,
        EstablecimientoId? establecimientoId,
        Guid comprobanteId,
        CancellationToken cancellationToken = default);

    Task<CobranzaDto> RegistrarCobranzaInmediataAsync(
        TenantId tenantId,
        EmpresaId empresaId,
        EstablecimientoId? establecimientoId,
        CobranzaDto comando,
        CancellationToken cancellationToken = default);

    Task<CobranzaDto> RegistrarCobranzaParcialAsync(
        TenantId tenantId,
        EmpresaId empresaId,
        EstablecimientoId? establecimientoId,
        CobranzaDto comando,
        CancellationToken cancellationToken = default);

    Task<CobranzaDto> RegistrarCobranzaPorCuotaAsync(
        TenantId tenantId,
        EmpresaId empresaId,
        EstablecimientoId? establecimientoId,
        CobranzaDto comando,
        CancellationToken cancellationToken = default);
}
