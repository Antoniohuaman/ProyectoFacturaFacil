// src/GestionCobranzasBC/Application/UseCases/ObtenerHistorialCobranzaUseCase.cs
namespace GestionCobranzasBC.Application.UseCases;

using System;
using System.Threading;
using System.Threading.Tasks;
using GestionCobranzasBC.Application.DTOs;
using GestionCobranzasBC.Application.Interfaces;

public sealed class ObtenerHistorialCobranzaUseCase
{
    private readonly ICobranzasReadModel _readModel;

    public ObtenerHistorialCobranzaUseCase(ICobranzasReadModel readModel)
    {
        _readModel = readModel ?? throw new ArgumentNullException(nameof(readModel));
    }

    public Task<HistorialCobranzaDto> ExecuteAsync(
        Guid cuentaPorCobrarId,
        CancellationToken cancellationToken = default)
    {
        if (cuentaPorCobrarId == Guid.Empty)
        {
            throw new ArgumentException("CuentaPorCobrarId no puede ser vacío.", nameof(cuentaPorCobrarId));
        }

        return _readModel.ObtenerHistorialCobranzaAsync(cuentaPorCobrarId, cancellationToken);
    }
}
