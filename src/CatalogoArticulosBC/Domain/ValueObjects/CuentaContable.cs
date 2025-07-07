namespace CatalogoArticulosBC.Domain.ValueObjects
{
    public record CuentaContable
    {
        public string Numero { get; }
        private CuentaContable(string numero) => Numero = numero;

        public static CuentaContable From(string numero)
        {
            if (string.IsNullOrWhiteSpace(numero))
                throw new ArgumentException("Cuenta contable inválida.", nameof(numero));
            return new CuentaContable(numero);
        }

        public override string ToString() => Numero;
    }
}
