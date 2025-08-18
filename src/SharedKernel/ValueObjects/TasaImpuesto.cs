using System;
using System.Diagnostics;

namespace SharedKernel.ValueObjects
{
    /// <summary>
    /// VO que representa una <b>tasa de impuesto</b> como fracción en el intervalo [0,1].
    /// Ej.: 0.18 = 18%. No arrastra tipo de afectación (Cat. 07) ni cálculos monetarios.
    ///
    /// Reglas:
    /// - 0.00 ≤ Fracción ≤ 1.00 (inclusive).
    /// - Se normaliza a 6 decimales para evitar ruido numérico.
    /// - El redondeo de <i>dinero</i> vive en el VO Dinero/servicios de cálculo, no aquí.
    /// </summary>
    [DebuggerDisplay("{Fraccion,nq} ({Porcentaje}% )")]
    public sealed record TasaImpuesto
    {
        /// <summary>Fracción de la tasa (0.00–1.00). Ej.: 0.10m = 10%.</summary>
        public decimal Fraccion { get; }

        /// <summary>Porcentaje de la tasa (0–100). Ej.: 10 = 10%.</summary>
        public decimal Porcentaje => Math.Round(Fraccion * 100m, 6, MidpointRounding.AwayFromZero);

        /// <summary>True si la tasa es exactamente 0 tras la normalización.</summary>
        public bool EsCero => Fraccion == 0m;

        private TasaImpuesto(decimal fraccionNormalizada)
        {
            Fraccion = fraccionNormalizada; // ya validada/normalizada
        }

        // --------------------- FÁBRICAS ---------------------

        /// <summary>
        /// Crea desde fracción (0.18m). Valida y normaliza.
        /// </summary>
        public static TasaImpuesto FromFraction(decimal fraccion)
        {
            if (fraccion < 0m || fraccion > 1m)
                throw new ArgumentOutOfRangeException(nameof(fraccion), "La tasa debe estar entre 0.00 y 1.00.");
            return new TasaImpuesto(Normalizar(fraccion));
        }

        /// <summary>
        /// Crea desde porcentaje (18 =&gt; 0.18). Valida y normaliza.
        /// </summary>
        public static TasaImpuesto FromPercent(decimal porcentaje)
        {
            if (porcentaje < 0m || porcentaje > 100m)
                throw new ArgumentOutOfRangeException(nameof(porcentaje), "El porcentaje debe estar entre 0 y 100.");
            return FromFraction(porcentaje / 100m);
        }

        /// <summary>Intenta crear desde fracción sin lanzar excepciones.</summary>
        public static bool TryFromFraction(decimal fraccion, out TasaImpuesto? tasa)
        {
            try { tasa = FromFraction(fraccion); return true; }
            catch { tasa = null; return false; }
        }

        /// <summary>Intenta crear desde porcentaje sin lanzar excepciones.</summary>
        public static bool TryFromPercent(decimal porcentaje, out TasaImpuesto? tasa)
        {
            try { tasa = FromPercent(porcentaje); return true; }
            catch { tasa = null; return false; }
        }

        // --------------------- HELPERS ---------------------

        /// <summary>
        /// Si la afectación <paramref name="afectacion"/> no grava impuesto (exonerado/inafecto/exportación),
        /// retorna 0%; si grava (gravado/IVAP), retorna esta misma tasa.
        /// </summary>
        public TasaImpuesto CompatibilizarCon(AfectacionImpuesto afectacion) =>
            afectacion.GravaImpuesto ? this : Cero;

        /// <summary>Representación amigable en porcentaje, p. ej. "10.00%".</summary>
        public string ToPercentString(int decimales = 2)
        {
            var p = Math.Round(Porcentaje, decimales, MidpointRounding.AwayFromZero);
            return p.ToString($"0.{new string('0', decimales)}") + "%";
        }

        /// <summary>Etiqueta lista para UI, p. ej. "IGV (10.00%)".</summary>
        public string ToDisplay(string nombreImpuesto = "IGV", int decimales = 2) =>
            $"{nombreImpuesto} ({ToPercentString(decimales)})";

        public override string ToString() => Fraccion.ToString("0.######");

        // --------------------- ATajos USUALES ---------------------
        /// <summary>0%</summary>
        public static readonly TasaImpuesto Cero  = new(0m);

        /// <summary>IGV 18% (régimen general).</summary>
        public static readonly TasaImpuesto IGV18 = new(0.18m);

        /// <summary>IGV 10% (opción de UI para rubros con beneficio). Usar si la política/empresa lo permite.</summary>
        public static readonly TasaImpuesto IGV10 = new(0.10m);

        /// <summary>IGV 12% (transitorio según periodo/regla).</summary>
        public static readonly TasaImpuesto IGV12 = new(0.12m);

        /// <summary>IGV 8% (transitorio según periodo/regla).</summary>
        public static readonly TasaImpuesto IGV8  = new(0.08m);

        // --------------------- PRIVADO ---------------------
        private static decimal Normalizar(decimal value) =>
            Math.Round(value, 6, MidpointRounding.AwayFromZero);
    }
}