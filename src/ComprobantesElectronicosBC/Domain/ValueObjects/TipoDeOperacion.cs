namespace ComprobantesElectronicosBC.Domain.ValueObjects;

public readonly record struct TipoDeOperacion(string Value)
{
    // Mínimo operativo; podrás ampliar según catálogo 51.
    public static readonly TipoDeOperacion VentaInterna = new("0101");

    public static TipoDeOperacion Create(string value)
    {
        value = value?.Trim() ?? throw new ArgumentNullException(nameof(value));
        if (value.Length is 4 && value.All(char.IsDigit)) return new(value);
        throw new ArgumentException("TipoDeOperacion inválido (esperado: código de 4 dígitos, p.ej. '0101').");
    }

    public override string ToString() => Value;
}
