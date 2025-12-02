// src/GestionCobranzasBC/Application/Interfaces/ICobranzasReadModel.cs
#nullable enable
namespace GestionCobranzasBC.Application.Interfaces;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GestionCobranzasBC.Application.DTOs;

public interface ICobranzasReadModel
{
    Task<IReadOnlyList<CobranzaDto>> ListarCobranzasAsync(
        FiltrosCobranzaDto filtros,
        CancellationToken cancellationToken = default);

    Task<HistorialCobranzaDto> ObtenerHistorialCobranzaAsync(
        Guid cuentaPorCobrarId,
        CancellationToken cancellationToken = default);
}
