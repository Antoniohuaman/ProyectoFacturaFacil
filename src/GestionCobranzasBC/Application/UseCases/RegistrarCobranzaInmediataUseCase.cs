// src/GestionCobranzasBC/Application/UseCases/RegistrarCobranzaInmediataUseCase.cs
namespace GestionCobranzasBC.Application.UseCases;

using System;
using System.Threading;
using System.Threading.Tasks;
using GestionCobranzasBC.Application.DTOs;
using GestionCobranzasBC.Application.Interfaces;
using SharedKernel.ValueObjects;

public sealed class RegistrarCobranzaInmediataUseCase
{
    private readonly ICobranzasDomainService _cobranzasDomainService;
    private readonly IUnitOfWorkGestionCobranzas _unitOfWork;

    public RegistrarCobranzaInmediataUseCase(
        ICobranzasDomainService cobranzasDomainService,
        IUnitOfWorkGestionCobranzas unitOfWork)
    {
        _cobranzasDomainService = cobranzasDomainService ?? throw new ArgumentNullException(nameof(cobranzasDomainService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<CobranzaDto> ExecuteAsync(
        CobranzaDto comando,
        CancellationToken cancellationToken = default)
    {
        if (comando is null) throw new ArgumentNullException(nameof(comando));
        if (comando.CuentaPorCobrarId == Guid.Empty)
        {
            throw new ArgumentException("CuentaPorCobrarId no puede ser vacío.", nameof(comando));
        }
        if (comando.TenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId no puede ser vacío.", nameof(comando));
        }
        if (comando.EmpresaId == Guid.Empty)
        {
            throw new ArgumentException("EmpresaId no puede ser vacío.", nameof(comando));
        }

        var tenant = TenantId.From(comando.TenantId);
        var empresa = EmpresaId.From(comando.EmpresaId.ToString());
        var establecimiento = comando.EstablecimientoId.HasValue
            ? EstablecimientoId.From(comando.EstablecimientoId.Value)
            : null;

        var resultado = await _cobranzasDomainService
            .RegistrarCobranzaInmediataAsync(
                tenant,
                empresa,
                establecimiento,
                comando,
                cancellationToken)
            .ConfigureAwait(false);

        await _unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);

        return resultado;
    }
}
