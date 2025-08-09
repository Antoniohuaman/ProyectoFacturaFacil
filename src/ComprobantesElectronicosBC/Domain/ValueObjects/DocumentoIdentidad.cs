using System.Text.RegularExpressions;

namespace ComprobantesElectronicosBC.Domain.ValueObjects;

public sealed record DocumentoIdentidad
{
    // SUNAT catálogo 06: usamos mínimos "6" RUC, "1" DNI (extensible)
    public string Tipo { get; }     // "6" = RUC, "1" = DNI
    public string Numero { get; }   // 11 dígitos (RUC) o 8 dígitos (DNI)

    private DocumentoIdentidad(string tipo, string numero)
    {
        Tipo = tipo;
        Numero = numero;
    }

    public static DocumentoIdentidad CreateRuc(string ruc)
    {
        ruc = ruc?.Trim() ?? throw new ArgumentNullException(nameof(ruc));
        if (!Regex.IsMatch(ruc, @"^\d{11}$")) throw new ArgumentException("RUC debe tener 11 dígitos.");
        if (!EsRucValido(ruc)) throw new ArgumentException("RUC inválido (dígito verificador).");
        return new("6", ruc);
    }

    public static DocumentoIdentidad CreateDni(string dni)
    {
        dni = dni?.Trim() ?? throw new ArgumentNullException(nameof(dni));
        if (!Regex.IsMatch(dni, @"^\d{8}$")) throw new ArgumentException("DNI debe tener 8 dígitos.");
        return new("1", dni);
    }

    public static DocumentoIdentidad Create(string tipo, string numero)
    {
        tipo = tipo?.Trim() ?? throw new ArgumentNullException(nameof(tipo));
        return tipo switch
        {
            "6" => CreateRuc(numero),
            "1" => CreateDni(numero),
            _ => throw new ArgumentException("Tipo de documento no soportado (use '6' RUC o '1' DNI).")
        };
    }

    public bool EsRuc => Tipo == "6";
    public bool EsDni => Tipo == "1";

    public override string ToString() => (EsRuc ? "RUC " : "DNI ") + Numero;

    // Algoritmo SUNAT (módulo 11) para RUC.
    private static bool EsRucValido(string ruc)
    {
        // pesos para los primeros 10 dígitos
        int[] pesos = { 5,4,3,2,7,6,5,4,3,2 };
        int suma = 0;
        for (int i = 0; i < 10; i++)
            suma += (ruc[i] - '0') * pesos[i];

        int resto = suma % 11;
        int digito = 11 - resto;
        if (digito == 10) digito = 0;
        else if (digito == 11) digito = 1;

        return digito == (ruc[10] - '0');
    }
}
