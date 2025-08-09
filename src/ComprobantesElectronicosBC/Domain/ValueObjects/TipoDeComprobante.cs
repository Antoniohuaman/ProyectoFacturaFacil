namespace ComprobantesElectronicosBC.Domain.ValueObjects;

public readonly record struct TipoDeComprobante(string Value)
{
    public static readonly TipoDeComprobante Factura = new("01");
    public static readonly TipoDeComprobante Boleta  = new("03");

    public static TipoDeComprobante Create(string value)
    {
        value = value?.Trim() ?? throw new ArgumentNullException(nameof(value));
        if (value is "01" or "03") return new(value);
        throw new ArgumentException("TipoDeComprobante inválido. Use '01' (Factura) o '03' (Boleta).");
    }

    public bool EsFactura => Value == "01";
    public bool EsBoleta  => Value == "03";
    public override string ToString() => Value;
}
