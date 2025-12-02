using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GestionCobranzasBC.Domain.Aggregates;
using GestionCobranzasBC.Domain.ValueObjects;
using SharedKernel.Specifications;

namespace GestionCobranzasBC.Domain.Repositories;

/// <summary>
/// Contrato de persistencia para el agregado CuentaPorCobrar.
/// </summary>
public interface ICuentaPorCobrarRepository
{
    Task<CuentaPorCobrar?> ObtenerPorIdAsync(
        CuentaPorCobrarId id,
        CancellationToken cancellationToken = default);

    Task AgregarAsync(
        CuentaPorCobrar cuenta,
        CancellationToken cancellationToken = default);

    Task ActualizarAsync(
        CuentaPorCobrar cuenta,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CuentaPorCobrar>> ListarAsync(
        ISpecification<CuentaPorCobrar>? specification,
        CancellationToken cancellationToken = default);
}
