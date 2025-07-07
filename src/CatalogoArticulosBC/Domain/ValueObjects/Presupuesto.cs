namespace CatalogoArticulosBC.Domain.ValueObjects
{
    public record Presupuesto
    {
        public decimal Monto { get; }
        private Presupuesto(decimal monto) => Monto = monto;

        public static Presupuesto From(decimal monto)
        {
            if (monto < 0)
                throw new ArgumentException("Presupuesto no puede ser negativo.", nameof(monto));
            return new Presupuesto(monto);
        }

        public override string ToString() => Monto.ToString("F2");
    }
}
