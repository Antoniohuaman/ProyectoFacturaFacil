using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GestionCobranzasBC.Domain.Aggregates;
using GestionCobranzasBC.Domain.ValueObjects;
using SharedKernel.Specifications;

namespace GestionCobranzasBC.Domain.Repositories;

/// <summary>
/// Contrato de persistencia para el agregado Cobranza (documento C1).
/// </summary>
public interface ICobranzaRepository
{
    Task<Cobranza?> ObtenerPorIdAsync(
        CobranzaId id,
        CancellationToken cancellationToken = default);

    Task AgregarAsync(
        Cobranza cobranza,
        CancellationToken cancellationToken = default);

    Task ActualizarAsync(
        Cobranza cobranza,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Cobranza>> ListarAsync(
        ISpecification<Cobranza>? specification,
        CancellationToken cancellationToken = default);
}
