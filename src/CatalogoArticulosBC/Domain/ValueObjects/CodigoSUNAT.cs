namespace CatalogoArticulosBC.Domain.ValueObjects
{
    public record CodigoSUNAT
    {
        public string Value { get; }
        private CodigoSUNAT(string value) => Value = value;

        public static CodigoSUNAT From(string code)
        {
            if (string.IsNullOrWhiteSpace(code) || code.Length != 8)
                throw new ArgumentException("Código SUNAT inválido.", nameof(code));
            return new CodigoSUNAT(code);
        }

        public override string ToString() => Value;
    }
}
