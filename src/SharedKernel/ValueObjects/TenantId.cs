using System;

namespace SharedKernel.ValueObjects;

/// <summary>
/// Identificador de Tenant (cliente) como Value Object transversal.
/// - Inmutable
/// - Valida que no sea Guid.Empty
/// - Igualdad por valor (record struct)
/// </summary>
public readonly record struct TenantId
{
    public Guid Value { get; }

    public TenantId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("TenantId no puede ser Guid.Empty.", nameof(value));
        Value = value;
    }

    /// <summary>Crea un nuevo TenantId aleatorio.</summary>
    public static TenantId New() => new(Guid.NewGuid());

    /// <summary>Construye desde Guid validando no vacío.</summary>
    public static TenantId From(Guid value) => new(value);

    /// <summary>Construye desde string (formato Guid). Valida no-vacío.</summary>
    public static TenantId FromString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("TenantId (string) no puede ser nulo o vacío.", nameof(value));

        var g = Guid.Parse(value.Trim());
        return new TenantId(g);
    }

    /// <summary>Intenta parsear desde string (formato Guid).</summary>
    public static bool TryParse(string? input, out TenantId result)
    {
        result = default;
        if (!string.IsNullOrWhiteSpace(input) &&
            Guid.TryParse(input.Trim(), out var g) &&
            g != Guid.Empty)
        {
            result = new TenantId(g);
            return true;
        }
        return false;
    }

    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString();

    // Conversiones explícitas para evitar usos accidentales
    public static explicit operator Guid(TenantId id) => id.Value;
    public static explicit operator TenantId(Guid value) => From(value);
}
