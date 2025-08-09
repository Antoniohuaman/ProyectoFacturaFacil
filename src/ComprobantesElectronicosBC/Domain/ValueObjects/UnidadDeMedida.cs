namespace ComprobantesElectronicosBC.Domain.ValueObjects;

public readonly record struct UnidadDeMedida(string Codigo)
{
    // Mínimo operativo. Agrega más UNECE Rec20 según necesites.
    public static readonly UnidadDeMedida NIU = new("NIU"); // unidad
    public static readonly UnidadDeMedida E48 = new("E48"); // servicio
    public static readonly UnidadDeMedida KG  = new("KG");

    public static UnidadDeMedida Create(string codigo)
    {
        codigo = codigo?.Trim().ToUpperInvariant()
                 ?? throw new ArgumentNullException(nameof(codigo));
        if (codigo.Length is >= 2 and <= 3 && codigo.All(char.IsLetterOrDigit))
            return new(codigo);

        throw new ArgumentException("Unidad de medida inválida (2-3 caracteres alfanuméricos).");
    }

    public override string ToString() => Codigo;
}
