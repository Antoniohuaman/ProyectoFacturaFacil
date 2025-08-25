using System.Text.RegularExpressions;
using SharedKernel.Exceptions;

namespace SharedKernel.ValueObjects;

/// <summary>
/// Documento de identidad para clientes/proveedores, alineado a SUNAT/UBL.
/// - Usa Catálogo 06 para emitir (schemeID): 0,1,4,6,7,A,B,C,D.
/// - 'SinDocumento' es solo de uso interno (no emitable a UBL).
/// - Sin dependencias de infraestructura; valida y normaliza por tipo.
/// </summary>
public sealed record DocumentoIdentidad
{
    // TipoDocumento ahora se importa desde SharedKernel.ValueObjects.TipoDocumento

    /// <summary>Tipo normativo de documento.</summary>
    public TipoDocumento Tipo { get; }

    /// <summary>
    /// Número normalizado.
    /// - RUC/DNI: solo dígitos.
    /// - Otros (A/B/C/D/4/7): A–Z, 0–9, guion; mayúsculas; 1..15 chars (configurable).
    /// - SinDocumento: cadena vacía.
    /// </summary>
    public string Numero { get; }

    private DocumentoIdentidad(TipoDocumento tipo, string numero)
        => (Tipo, Numero) = (tipo, numero);

    // ===== Fábricas públicas =====

    public static DocumentoIdentidad Crear(TipoDocumento tipo, string? numero)
    {
        numero = (numero ?? string.Empty).Trim();

        return tipo switch
        {
            TipoDocumento.Ruc  => CrearRuc(numero),
            TipoDocumento.Dni  => CrearDni(numero),

            TipoDocumento.CarnetExtranjeria                     => CrearAlfanum(tipo, numero, "Carnet de extranjería"),
            TipoDocumento.Pasaporte                             => CrearAlfanum(tipo, numero, "Pasaporte"),
            TipoDocumento.CedulaDiplomatica                     => CrearAlfanum(tipo, numero, "Cédula diplomática"),
            TipoDocumento.DocIdentidadPaisResidenciaNoDomiciliado => CrearAlfanum(tipo, numero, "Documento identidad país de residencia (no dom.)"),
            TipoDocumento.TinPersonaNatural                      => CrearAlfanum(tipo, numero, "TIN (persona natural)"),
            TipoDocumento.InPersonaJuridica                      => CrearAlfanum(tipo, numero, "IN (persona jurídica)"),

            TipoDocumento.SinDocumento => new(TipoDocumento.SinDocumento, string.Empty),

            _ => throw new BusinessRuleException("Tipo de documento no soportado.")
        };
    }

    /// <summary>
    /// Detecta por longitud de dígitos (11→RUC, 8→DNI). Para otros tipos usa Crear(tipo,...).
    /// </summary>
    public static DocumentoIdentidad FromNumeroDetectandoTipo(string numero)
    {
        var digits = SoloDigitos(numero);
        return digits.Length switch
        {
            11 => Crear(TipoDocumento.Ruc, digits),
            8  => Crear(TipoDocumento.Dni, digits),
            _  => throw new BusinessRuleException("No se puede detectar tipo. Usa la fábrica explícita.")
        };
    }

    public static bool TryCrear(TipoDocumento tipo, string? numero, out DocumentoIdentidad? doc)
    {
        try { doc = Crear(tipo, numero); return true; }
        catch { doc = null; return false; }
    }

    public static bool TryFromNumeroDetectandoTipo(string numero, out DocumentoIdentidad? doc)
    {
        try { doc = FromNumeroDetectandoTipo(numero); return true; }
        catch { doc = null; return false; }
    }

    // ===== Consultas de negocio =====

    public bool EsRuc => Tipo == TipoDocumento.Ruc;
    public bool EsDni => Tipo == TipoDocumento.Dni;

    /// <summary>Heurística habitual: RUC de PN empieza en "10"; no es regla normativa.</summary>
    public bool EsRuc10 => EsRuc && Numero.StartsWith("10");

    /// <summary>Heurística habitual: RUC de PJ empieza en "20"; no es regla normativa.</summary>
    public bool EsRuc20 => EsRuc && Numero.StartsWith("20");

    /// <summary>True para tipos de no domiciliado (B/C/D).</summary>
    public bool EsNoDomiciliado =>
        Tipo is TipoDocumento.DocIdentidadPaisResidenciaNoDomiciliado
            or TipoDocumento.TinPersonaNatural
            or TipoDocumento.InPersonaJuridica;

    /// <summary>
    /// Código Catálogo 06 para UBL (schemeID).
    /// Lanza si es SinDocumento (no emitable).
    /// </summary>
    public string SchemeId => Tipo switch
    {
        TipoDocumento.Ruc                                   => "6",
        TipoDocumento.Dni                                   => "1",
        TipoDocumento.CarnetExtranjeria                     => "4",
        TipoDocumento.Pasaporte                             => "7",
        TipoDocumento.CedulaDiplomatica                     => "A",
        TipoDocumento.DocIdentidadPaisResidenciaNoDomiciliado => "B",
        TipoDocumento.TinPersonaNatural                      => "C",
        TipoDocumento.InPersonaJuridica                      => "D",
        TipoDocumento.SinDocumento                           => throw new BusinessRuleException("SinDocumento no es válido para UBL."),
        _ => throw new BusinessRuleException("Tipo de documento no soportado.")
    };

    public override string ToString() => Tipo switch
    {
        TipoDocumento.Ruc                                   => $"RUC {Numero}",
        TipoDocumento.Dni                                   => $"DNI {Numero}",
        TipoDocumento.CarnetExtranjeria                     => $"Carnet Extranjería {Numero}",
        TipoDocumento.Pasaporte                             => $"Pasaporte {Numero}",
        TipoDocumento.CedulaDiplomatica                     => $"Cédula Diplomática {Numero}",
        TipoDocumento.DocIdentidadPaisResidenciaNoDomiciliado => $"Doc. identidad país residencia {Numero}",
        TipoDocumento.TinPersonaNatural                      => $"TIN {Numero}",
        TipoDocumento.InPersonaJuridica                      => $"IN {Numero}",
        TipoDocumento.SinDocumento                           => "Sin documento",
        _ => $"{Tipo}:{Numero}"
    };

    // ===== Internos =====

    private const string AlfanumPattern = @"^[A-Z0-9-]{1,15}$";

    private static DocumentoIdentidad CrearRuc(string input)
    {
        var ruc = SoloDigitos(input);
        if (ruc.Length != 11) throw new BusinessRuleException("RUC debe tener 11 dígitos.");
        if (!EsRucValido(ruc)) throw new BusinessRuleException("RUC inválido (dígito verificador).");
        return new(TipoDocumento.Ruc, ruc);
    }

    private static DocumentoIdentidad CrearDni(string input)
    {
        var dni = SoloDigitos(input);
        if (dni.Length != 8) throw new BusinessRuleException("DNI debe tener 8 dígitos.");
        return new(TipoDocumento.Dni, dni);
    }

    private static DocumentoIdentidad CrearAlfanum(TipoDocumento tipo, string input, string etiqueta)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new BusinessRuleException($"{etiqueta}: el número no puede estar vacío.");

        var norm = input.Trim().ToUpperInvariant();
        if (!Regex.IsMatch(norm, AlfanumPattern))
            throw new BusinessRuleException($"{etiqueta}: use A–Z, 0–9 o '-' (1–15).");

        return new(tipo, norm);
    }

    private static string SoloDigitos(string? s)
        => s is null
            ? throw new BusinessRuleException("Número no puede ser nulo.")
            : Regex.Replace(s, @"\D", "");

    // Algoritmo SUNAT (módulo 11) para dígito verificador del RUC.
    private static bool EsRucValido(string ruc11)
    {
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
