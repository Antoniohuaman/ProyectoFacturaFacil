namespace CatalogoArticulosBC.Domain.ValueObjects
{
    public record CentroCosto
    {
        public string Codigo { get; }
        private CentroCosto(string codigo) => Codigo = codigo;

        public static CentroCosto From(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                throw new ArgumentException("Centro de costo inválido.", nameof(codigo));
            return new CentroCosto(codigo);
        }

        public override string ToString() => Codigo;
    }
}
