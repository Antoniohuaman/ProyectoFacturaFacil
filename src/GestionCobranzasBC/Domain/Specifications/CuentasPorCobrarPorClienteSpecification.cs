using System;
using GestionCobranzasBC.Domain.Aggregates;
using SharedKernel.Specifications;

namespace GestionCobranzasBC.Domain.Specifications;

/// <summary>
/// Filtra cuentas por cobrar pertenecientes a un cliente específico.
/// </summary>
public sealed class CuentasPorCobrarPorClienteSpecification : IBooleanSpecification<CuentaPorCobrar>
{
    public Guid ClienteId { get; }

    public CuentasPorCobrarPorClienteSpecification(Guid clienteId)
    {
        if (clienteId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de cliente no puede ser vacío.", nameof(clienteId));
        }

        ClienteId = clienteId;
    }

    public bool IsSatisfiedBy(CuentaPorCobrar candidate)
    {
        if (candidate is null)
        {
            return false;
        }

        return candidate.ClienteId == ClienteId;
    }
}
