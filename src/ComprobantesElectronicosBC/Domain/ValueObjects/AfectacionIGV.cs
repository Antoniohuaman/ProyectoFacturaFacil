namespace ComprobantesElectronicosBC.Domain.ValueObjects;

public readonly record struct AfectacionIGV(string Codigo)
{
    // Mínimos más comunes:
    // 10 = Gravado - Operación onerosa
    // 20 = Exonerado
    // 30 = Inafecto
    // 40 = Exportación
    public static readonly AfectacionIGV Gravado    = new("10");
    public static readonly AfectacionIGV Exonerado  = new("20");
    public static readonly AfectacionIGV Inafecto   = new("30");
    public static readonly AfectacionIGV Exportacion= new("40");

    public static AfectacionIGV Create(string codigo)
    {
        codigo = codigo?.Trim() ?? throw new ArgumentNullException(nameof(codigo));
        if (codigo.Length is 2 && codigo.All(char.IsDigit)) return new(codigo);
        throw new ArgumentException("Afectación IGV inválida (2 dígitos del catálogo 7).");
    }

    public bool EsGravado => Codigo == "10";
    public override string ToString() => Codigo;
}
