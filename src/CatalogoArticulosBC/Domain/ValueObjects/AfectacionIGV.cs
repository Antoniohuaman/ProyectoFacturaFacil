namespace CatalogoArticulosBC.Domain.ValueObjects
{
    public record AfectacionIGV
    {
        public string Codigo { get; }
        private AfectacionIGV(string codigo) => Codigo = codigo;

        public static AfectacionIGV From(string codigo)
        {
            var validos = new[] { "10", "20", "30" };
            if (!validos.Contains(codigo))
                throw new ArgumentException("Código de afectación IGV inválido.", nameof(codigo));
            return new AfectacionIGV(codigo);
        }

        public override string ToString() => Codigo;
    }
}
