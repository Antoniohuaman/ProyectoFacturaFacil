using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ProyectoFacturaFacil.GestionCobranzasBC.Domain.Aggregates;
using ProyectoFacturaFacil.GestionCobranzasBC.Domain.ValueObjects;
using ProyectoFacturaFacil.SharedKernel.Specifications;

namespace ProyectoFacturaFacil.GestionCobranzasBC.Domain.Repositories;

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
