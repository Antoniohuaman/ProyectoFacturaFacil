using System;
using System.Text.RegularExpressions;

namespace ComprobantesElectronicosBC.Domain.ValueObjects;

/// <summary>
/// Centro de costo asociado al comprobante (opcional).
/// VO porque su identidad es el valor (Code + Name).
/// Mapea natural a UBL: AccountingCostCode (código, ≤35) y AccountingCost (texto).
/// </summary>
public readonly record struct CentroDeCosto
{
    // ---- Reglas y límites
    public const int MaxCodeLength = 35;   // límite típico UBL para *Code*
    public const int MaxNameLength = 100;  // para PDF/UI (ajustable)

    // Permitidos: A-Z, 0-9, espacio, -, _, ., /
    private static readonly Regex CodePattern =
        new(@"^[A-Z0-9\-_.\/ ]+$", RegexOptions.Compiled);

    // ---- Propiedades (solo get = inmutables)
    public string Code { get; }
    public string? Name { get; }

    // ---- Constructor EXPLÍCITO con validaciones e inmutabilidad
    public CentroDeCosto(string code, string? name)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("El código de centro de costo es obligatorio cuando se informa.", nameof(code));

        // Normalización
        code = code.Trim().ToUpperInvariant();

        if (code.Length > MaxCodeLength)
            throw new ArgumentException($"El código no puede exceder {MaxCodeLength} caracteres.", nameof(code));

        if (!CodePattern.IsMatch(code))
            throw new ArgumentException("Código con caracteres no permitidos. Permitidos: A-Z, 0-9, espacio, -, _, ., /", nameof(code));

        if (!string.IsNullOrWhiteSpace(name))
        {
            name = name.Trim();
            if (name.Length > MaxNameLength)
                throw new ArgumentException($"El nombre no puede exceder {MaxNameLength} caracteres.", nameof(name));
        }
        else
        {
            name = null;
        }

        Code = code;
        Name = name;
    }

    // ---- Fábricas
    public static CentroDeCosto Create(string code, string? name = null) => new(code, name);

    /// <summary>Conveniente para campos opcionales del formulario.</summary>
    public static CentroDeCosto? FromOptional(string? code, string? name = null)
        => string.IsNullOrWhiteSpace(code) ? null : new CentroDeCosto(code!, name);

    // ---- Helpers para mapeo/visualización
    public string ForUbl_AccountingCostCode() => Code;
    public string ForUbl_AccountingCost() => string.IsNullOrWhiteSpace(Name) ? Code : Name!;
    public override string ToString() => string.IsNullOrWhiteSpace(Name) ? Code : $"{Code} - {Name}";
}
