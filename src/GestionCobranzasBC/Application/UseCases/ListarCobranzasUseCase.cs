// src/GestionCobranzasBC/Application/UseCases/ListarCobranzasUseCase.cs
namespace GestionCobranzasBC.Application.UseCases;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GestionCobranzasBC.Application.DTOs;
using GestionCobranzasBC.Application.Interfaces;

public sealed class ListarCobranzasUseCase
{
    private readonly ICobranzasReadModel _readModel;

    public ListarCobranzasUseCase(ICobranzasReadModel readModel)
    {
        _readModel = readModel ?? throw new ArgumentNullException(nameof(readModel));
    }

    public Task<IReadOnlyList<CobranzaDto>> ExecuteAsync(
        FiltrosCobranzaDto filtros,
        CancellationToken cancellationToken = default)
    {
        if (filtros is null) throw new ArgumentNullException(nameof(filtros));

        return _readModel.ListarCobranzasAsync(filtros, cancellationToken);
    }
}
