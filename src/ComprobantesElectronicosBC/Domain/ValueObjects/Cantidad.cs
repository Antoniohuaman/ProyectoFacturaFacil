using System;
using System.Globalization;

namespace ComprobantesElectronicosBC.Domain.ValueObjects;

/// <summary>
/// VO de cantidad (inmutable, igualdad por valor).
/// Reglas básicas:
/// - Debe ser > 0
/// - Escala máxima configurable (por defecto 6 decimales)
/// Helpers:
/// - Uno (valor por defecto de UI, se usa desde la aplicación)
/// - EnforceMaxScale: valida que no exceda la escala permitida (NIU=0, KG=3, etc.)
/// - RoundTo: redondea a la escala indicada (si decides permitir redondeo controlado)
/// </summary>
public readonly record struct Cantidad
{
    public decimal Value { get; }

    private Cantidad(decimal value) => Value = value;

    /// <summary>Atajo para 1 (útil como default en la capa de aplicación).</summary>
    public static Cantidad Uno => Create(1m);

    /// <summary>
    /// Crea una cantidad válida.
    /// <paramref name="maxScale"/> controla la cantidad de decimales aceptados (por defecto 6).
    /// No redondea: si supera la escala, lanza excepción.
    /// </summary>
    public static Cantidad Create(decimal value, int maxScale = 6)
    {
        if (value <= 0m)
            throw new ArgumentOutOfRangeException(nameof(value), "La cantidad debe ser > 0.");

        var scale = GetScale(value);
        if (scale > maxScale)
            throw new ArgumentException($"Cantidad con más de {maxScale} decimales (recibido: {scale}).", nameof(value));

        return new Cantidad(value);
    }

    /// <summary>
    /// Valida que la cantidad no exceda la escala permitida para la UM.
    /// Ej.: NIU=0, KG=3, MTR=3. Lanza si excede.
    /// </summary>
    public Cantidad EnforceMaxScale(int maxScale)
    {
        if (maxScale < 0) throw new ArgumentOutOfRangeException(nameof(maxScale));
        var scale = GetScale(Value);
        if (scale > maxScale)
            throw new ArgumentException($"Cantidad excede la precisión permitida ({maxScale} decimales).");
        return this;
    }

    /// <summary>
    /// Redondea a la escala indicada (si decides permitir redondeo controlado en tu flujo).
    /// Por defecto usa AwayFromZero (contable).
    /// </summary>
    public Cantidad RoundTo(int scale, MidpointRounding mode = MidpointRounding.AwayFromZero)
    {
        if (scale < 0) throw new ArgumentOutOfRangeException(nameof(scale));
        var rounded = decimal.Round(Value, scale, mode);
        if (rounded <= 0m)
            throw new InvalidOperationException("La cantidad resultante debe ser > 0.");
        return new Cantidad(rounded);
    }

    public override string ToString() => Value.ToString("0.######", CultureInfo.InvariantCulture);

    private static int GetScale(decimal value)
    {
        value = Math.Abs(value);
        var bits = decimal.GetBits(value);
        var scale = (byte)((bits[3] >> 16) & 0x7F);
        return scale;
    }
}
