// src/GestionCobranzasBC/Application/UseCases/EmitirSinCobranzaUseCase.cs
namespace GestionCobranzasBC.Application.UseCases;

using System;
using System.Threading;
using System.Threading.Tasks;
using GestionCobranzasBC.Application.DTOs;
using GestionCobranzasBC.Application.Interfaces;

public sealed class EmitirSinCobranzaUseCase
{
    private readonly ICobranzasDomainService _cobranzasDomainService;
    private readonly IUnitOfWorkGestionCobranzas _unitOfWork;

    public EmitirSinCobranzaUseCase(
        ICobranzasDomainService cobranzasDomainService,
        IUnitOfWorkGestionCobranzas unitOfWork)
    {
        _cobranzasDomainService = cobranzasDomainService ?? throw new ArgumentNullException(nameof(cobranzasDomainService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<CuentaPorCobrarDto> ExecuteAsync(
        Guid tenantId,
        Guid empresaId,
        Guid? establecimientoId,
        Guid comprobanteId,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId no puede ser vacío.", nameof(tenantId));
        if (empresaId == Guid.Empty) throw new ArgumentException("EmpresaId no puede ser vacío.", nameof(empresaId));
        if (comprobanteId == Guid.Empty) throw new ArgumentException("ComprobanteId no puede ser vacío.", nameof(comprobanteId));

        var cuenta = await _cobranzasDomainService
            .EmitirSinCobranzaAsync(
                tenantId,
                empresaId,
                establecimientoId,
                comprobanteId,
                cancellationToken)
            .ConfigureAwait(false);

        await _unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);

        return cuenta;
    }
}
