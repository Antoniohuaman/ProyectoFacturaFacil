namespace ListaPreciosBC.Domain.ValueObjects
{
    public record Prioridad(int Valor)
    {
        public bool EsValida() => Valor >= 1 && Valor <= 10;
    }
}