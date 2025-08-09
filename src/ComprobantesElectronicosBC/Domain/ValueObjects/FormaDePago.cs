namespace ComprobantesElectronicosBC.Domain.ValueObjects;

public readonly record struct FormaDePago(string Codigo)
{
    // Catálogo 20 (mínimo):
    public static readonly FormaDePago Contado = new("10");
    public static readonly FormaDePago Credito = new("20");

    public static FormaDePago Create(string codigo)
    {
        codigo = codigo?.Trim() ?? throw new ArgumentNullException(nameof(codigo));
        if (codigo is "10" or "20") return new(codigo);
        throw new ArgumentException("Forma de pago inválida. Use '10' (Contado) o '20' (Crédito).");
    }

    public bool EsContado => Codigo == "10";
    public bool EsCredito => Codigo == "20";
    public override string ToString() => Codigo;
}
