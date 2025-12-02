using System;
using GestionCobranzasBC.Domain.Aggregates;
using SharedKernel.Specifications;

namespace GestionCobranzasBC.Domain.Specifications;

/// <summary>
/// Filtra cobranzas por rango de fechas de documento (inclusive).
/// </summary>
public sealed class CobranzasPorRangoFechaSpecification : IBooleanSpecification<Cobranza>
{
    public DateOnly FechaInicio { get; }
    public DateOnly FechaFin { get; }

    public CobranzasPorRangoFechaSpecification(DateOnly fechaInicio, DateOnly fechaFin)
    {
        if (fechaFin < fechaInicio)
        {
            throw new ArgumentException("La fecha fin no puede ser menor que la fecha inicio.");
        }

        FechaInicio = fechaInicio;
        FechaFin = fechaFin;
    }

    public bool IsSatisfiedBy(Cobranza candidate)
    {
        if (candidate is null)
        {
            return false;
        }

        return candidate.FechaDocumento >= FechaInicio
               && candidate.FechaDocumento <= FechaFin;
    }
}
