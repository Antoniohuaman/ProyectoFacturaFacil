using System.Text.RegularExpressions;

namespace ComprobantesElectronicosBC.Domain.ValueObjects;

public sealed record SerieYNumero
{
    private static readonly Regex SerieRx  = new(@"^[A-Z][0-9]{3}$", RegexOptions.Compiled);
    private static readonly Regex NumeroRx = new(@"^[0-9]{8}$", RegexOptions.Compiled);

    public string Serie { get; }
    public string Numero { get; } // 8 dígitos con padding

    private SerieYNumero(string serie, string numero)
    {
        Serie = serie;
        Numero = numero;
    }

    public static SerieYNumero Create(string serie, int numero)
    {
        if (string.IsNullOrWhiteSpace(serie)) throw new ArgumentNullException(nameof(serie));
        serie = serie.Trim().ToUpperInvariant();
        if (!SerieRx.IsMatch(serie)) throw new ArgumentException("Serie inválida (ej: F001, B001).", nameof(serie));

        if (numero <= 0 || numero > 99_999_999) throw new ArgumentOutOfRangeException(nameof(numero));
        var numeroStr = numero.ToString("00000000");

        return new SerieYNumero(serie, numeroStr);
    }

    public override string ToString() => $"{Serie}-{Numero}";
}
