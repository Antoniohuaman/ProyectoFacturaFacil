namespace CatalogoArticulosBC.Domain.ValueObjects
{
    public record Peso
    {
        public double Valor { get; }
        private Peso(double valor) => Valor = valor;

        public static Peso From(double valor)
        {
            if (valor <= 0)
                throw new ArgumentException("Peso debe ser mayor que cero.", nameof(valor));
            return new Peso(valor);
        }

        public override string ToString() => Valor.ToString("F2");
    }
}
