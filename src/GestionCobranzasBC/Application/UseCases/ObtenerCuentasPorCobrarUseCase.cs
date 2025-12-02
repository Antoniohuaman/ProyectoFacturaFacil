// src/GestionCobranzasBC/Application/UseCases/ObtenerCuentasPorCobrarUseCase.cs
namespace GestionCobranzasBC.Application.UseCases;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GestionCobranzasBC.Application.DTOs;
using GestionCobranzasBC.Application.Interfaces;
using SharedKernel.ValueObjects;

public sealed class ObtenerCuentasPorCobrarUseCase
{
    private readonly ICuentaPorCobrarReadModel _readModel;

    public ObtenerCuentasPorCobrarUseCase(ICuentaPorCobrarReadModel readModel)
    {
        _readModel = readModel ?? throw new ArgumentNullException(nameof(readModel));
    }

    public Task<IReadOnlyList<CuentaPorCobrarDto>> ExecuteAsync(
        FiltrosCobranzaDto filtros,
        CancellationToken cancellationToken = default)
    {
        if (filtros is null) throw new ArgumentNullException(nameof(filtros));

        if (filtros.TenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId no puede ser vacío.", nameof(filtros));
        }

        _ = TenantId.From(filtros.TenantId);

        if (filtros.EmpresaId == Guid.Empty)
        {
            throw new ArgumentException("EmpresaId no puede ser vacío.", nameof(filtros));
        }

        _ = EmpresaId.From(filtros.EmpresaId.ToString());

        if (filtros.EstablecimientoId.HasValue)
        {
            _ = EstablecimientoId.From(filtros.EstablecimientoId.Value);
        }

        return _readModel.ObtenerCuentasAsync(filtros, cancellationToken);
    }
}
