namespace ComprobantesElectronicosBC.Domain.ValueObjects;

public sealed record ImporteMonetario
{
    public decimal Monto { get; }
    public Moneda Moneda { get; }

    private ImporteMonetario(decimal monto, Moneda moneda)
    {
        Monto = decimal.Round(monto, moneda.Decimales, MidpointRounding.AwayFromZero);
        Moneda = moneda;
    }

    public static ImporteMonetario Create(decimal monto, Moneda moneda)
    {
        if (monto < 0m) throw new ArgumentOutOfRangeException(nameof(monto), "El monto no puede ser negativo.");
        return new(monto, moneda);
    }

    public ImporteMonetario Multiplicar(decimal factor) =>
        new( decimal.Round(Monto * factor, Moneda.Decimales, MidpointRounding.AwayFromZero), Moneda );

    public ImporteMonetario Sumar(ImporteMonetario otro)
    {
        if (Moneda != otro.Moneda) throw new InvalidOperationException("No se pueden sumar montos con distinta moneda.");
        return new(Monto + otro.Monto, Moneda);
    }

    public override string ToString() => $"{Moneda} {Monto:n2}";
}
