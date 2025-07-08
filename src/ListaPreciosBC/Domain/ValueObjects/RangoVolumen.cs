using ListaPreciosBC.Domain.ValueObjects;

namespace ListaPreciosBC.Domain.ValueObjects
{
    public record RangoVolumen(int CantidadMin, int CantidadMax)
    {
        public bool EsValido() => CantidadMin < CantidadMax;
    }
}