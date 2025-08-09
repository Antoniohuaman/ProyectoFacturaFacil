namespace ComprobantesElectronicosBC.Domain.ValueObjects;

public readonly record struct Moneda(string Codigo)
{
    // Operativo mínimo para Perú
    public static readonly Moneda PEN = new("PEN");
    public static readonly Moneda USD = new("USD");

    public static Moneda Create(string codigo)
    {
        codigo = codigo?.Trim().ToUpperInvariant()
                 ?? throw new ArgumentNullException(nameof(codigo));
        if (codigo is "PEN" or "USD") return new(codigo);
        throw new ArgumentException("Moneda inválida. Soportadas: PEN, USD.");
    }

    public int Decimales => 2;
    public override string ToString() => Codigo;
}
