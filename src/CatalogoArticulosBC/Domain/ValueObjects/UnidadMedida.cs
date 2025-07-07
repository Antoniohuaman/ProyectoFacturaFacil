namespace CatalogoArticulosBC.Domain.ValueObjects
{
    public record UnidadMedida
    {
        public string Valor { get; }
        private UnidadMedida(string valor) => Valor = valor;

        public static UnidadMedida From(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                throw new ArgumentException("Unidad inválida.", nameof(valor));
            return new UnidadMedida(valor);
        }

        public override string ToString() => Valor;
    }
}
