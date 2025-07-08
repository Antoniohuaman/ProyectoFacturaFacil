using System;
using ListaPreciosBC.Domain.ValueObjects;

namespace ListaPreciosBC.Domain.ValueObjects
{
    public record PeriodoVigencia(DateTime FechaInicio, DateTime FechaFin)
    {
        public bool EsValido() => FechaInicio < FechaFin;
    }
}