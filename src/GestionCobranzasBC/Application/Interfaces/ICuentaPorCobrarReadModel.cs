// src/GestionCobranzasBC/Application/Interfaces/ICuentaPorCobrarReadModel.cs
#nullable enable
namespace GestionCobranzasBC.Application.Interfaces;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GestionCobranzasBC.Application.DTOs;

public interface ICuentaPorCobrarReadModel
{
    Task<IReadOnlyList<CuentaPorCobrarDto>> ObtenerCuentasAsync(
        FiltrosCobranzaDto filtros,
        CancellationToken cancellationToken = default);
}
