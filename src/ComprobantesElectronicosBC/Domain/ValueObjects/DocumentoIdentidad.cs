using System.Text.RegularExpressions;

namespace ComprobantesElectronicosBC.Domain.ValueObjects;

/// <summary>
/// VO de identidad de personas/empresas según Catálogo 06 de SUNAT.
/// - Tipos soportados (alcance actual): "6" = RUC, "1" = DNI.
/// - Invariantes: RUC = 11 dígitos con dígito verificador válido (módulo 11); DNI = 8 dígitos.
/// - Normaliza la entrada quitando separadores (espacios, guiones, etc.).
///
/// IMPORTANTE (límites del dominio):
/// - Este VO **no** consulta SUNAT ni RENIEC. Esas integraciones se hacen en Application/Adapters.
/// - Úsalo junto a un VO/DTO tipo ClienteSnapshot para guardar razón social/dirección obtenidas
///   de la base interna o de las consultas externas.
/// </summary>
public sealed record DocumentoIdentidad
{
    // Cat. 06 (alcance actual)
    public const string TipoRuc = "6";
    public const string TipoDni = "1";

    /// <summary>Cat.06: "6" (RUC) o "1" (DNI).</summary>
    public string Tipo { get; }

    /// <summary>Número normalizado: solo dígitos (RUC=11, DNI=8).</summary>
    public string Numero { get; }

    private DocumentoIdentidad(string tipo, string numero)
    {
        Tipo = tipo;
        Numero = numero;
    }

    // ===================== Fábricas principales =====================

    /// <summary>Crea detectando tipo según longitud: 11→RUC, 8→DNI. Lanza si no coincide.</summary>
    public static DocumentoIdentidad FromNumeroDetectandoTipo(string numero)
    {
        var digits = SoloDigitos(numero);
        return digits.Length switch
        {
            11 => CreateRuc(digits),
            8  => CreateDni(digits),
            _  => throw new ArgumentException("El número debe tener 11 (RUC) o 8 (DNI) dígitos.", nameof(numero))
        };
    }

    /// <summary>Crea según tipo explícito (Cat.06). Valida número y DV de RUC.</summary>
    public static DocumentoIdentidad Create(string tipo, string numero)
    {
        tipo = tipo?.Trim() ?? throw new ArgumentNullException(nameof(tipo));
        return tipo switch
        {
            TipoRuc => CreateRuc(numero),
            TipoDni => CreateDni(numero),
            _       => throw new ArgumentException("Tipo no soportado. Use \"6\" (RUC) o \"1\" (DNI).", nameof(tipo))
        };
    }

    /// <summary>RUC válido: 11 dígitos + verificador (SUNAT módulo 11).</summary>
    public static DocumentoIdentidad CreateRuc(string ruc)
    {
        var digits = SoloDigitos(ruc);
        if (digits.Length != 11) throw new ArgumentException("RUC debe tener 11 dígitos.", nameof(ruc));
        if (!EsRucValido(digits)) throw new ArgumentException("RUC inválido (dígito verificador).", nameof(ruc));
        return new(TipoRuc, digits);
    }

    /// <summary>DNI válido: 8 dígitos.</summary>
    public static DocumentoIdentidad CreateDni(string dni)
    {
        var digits = SoloDigitos(dni);
        if (digits.Length != 8) throw new ArgumentException("DNI debe tener 8 dígitos.", nameof(dni));
        return new(TipoDni, digits);
    }

    // ===================== TryCreate (útil para parseos en lote) =====================

    public static bool TryCreate(string tipo, string numero, out DocumentoIdentidad? doc)
    {
        try { doc = Create(tipo, numero); return true; }
        catch { doc = null; return false; }
    }

    public static bool TryFromNumeroDetectandoTipo(string numero, out DocumentoIdentidad? doc)
    {
        try { doc = FromNumeroDetectandoTipo(numero); return true; }
        catch { doc = null; return false; }
    }

    // ===================== Consultas de negocio =====================

    public bool EsRuc  => Tipo == TipoRuc;
    public bool EsDni  => Tipo == TipoDni;

    /// <summary>Heurística común: RUC de persona natural suele empezar con "10".</summary>
    public bool EsRuc10 => EsRuc && Numero.StartsWith("10");
    /// <summary>RUC de persona jurídica suele empezar con "20".</summary>
    public bool EsRuc20 => EsRuc && Numero.StartsWith("20");

    /// <summary>
    /// Para UBL: el schemeID es el propio Tipo ("6" RUC, "1" DNI).
    /// Esto se usa al serializar a XML (&lt;cbc:ID schemeID="6"&gt;...&lt;/cbc:ID&gt;).
    /// </summary>
    public string SchemeId => Tipo;

    public override string ToString() => Tipo switch
    {
        "6" => $"RUC {Numero}",
        "1" => $"DNI {Numero}",
        _   => $"{Tipo}:{Numero}"
    };

    // ===================== Helpers internos =====================

    private static string SoloDigitos(string? s)
        => s is null ? throw new ArgumentNullException(nameof(s)) : Regex.Replace(s, @"\D", "");

    // Algoritmo SUNAT (módulo 11) para el dígito verificador del RUC.
    private static bool EsRucValido(string ruc11)
    {
        // pesos para los primeros 10 dígitos
        int[] pesos = { 5, 4, 3, 2, 7, 6, 5, 4, 3, 2 };
        int suma = 0;
        for (int i = 0; i < 10; i++)
            suma += (ruc11[i] - '0') * pesos[i];

        int resto = suma % 11;
        int digito = 11 - resto;
        if (digito == 10) digito = 0;
        else if (digito == 11) digito = 1;

        return digito == (ruc11[10] - '0');
    }
}
