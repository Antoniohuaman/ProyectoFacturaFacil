#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SharedKernel.ValueObjects
{
    /// <summary>
    /// VO de Afectación del Impuesto (SUNAT Catálogo N° 07).
    /// Modela solo el CÓDIGO de afectación (p.ej. "10", "20", "21", "30"–"36", "40", "17")
    /// y deriva semántica necesaria para UBL/cálculo:
    ///  - Categoría (Gravado, Exonerado, Inafecto, Exportacion, IVAP)
    ///  - Código/Nombre de Tributo (Cat. 05): 1000 IGV, 1016 IVAP, 9997 EXO, 9996 GRA, 9998 INA, 9995 EXP
    ///  - GravaImpuesto (true en 10–16 y 17)
    ///  - EsGratuita (true en 21)
    /// </summary>
    [DebuggerDisplay("{Codigo} ({Categoria})")]
    public sealed record AfectacionImpuesto
    {
        /// <summary>Código Cat. 07 (dos dígitos). Ej.: "10", "20", "21", "30"…</summary>
        public string Codigo { get; }

        /// <summary>Categoría derivada según Cat. 07.</summary>
        public CategoriaAfectacion Categoria { get; }

        /// <summary>Código de tributo (Cat. 05): 1000, 1016, 9997, 9996, 9998, 9995.</summary>
        public string TributoCodigo { get; }

        /// <summary>Nombre corto del tributo (IGV, IVAP, EXO, GRA, INA, EXP).</summary>
        public string TributoNombre { get; }

        /// <summary>True si corresponde calcular impuesto sobre la base (10–16 = IGV, 17 = IVAP).</summary>
        public bool GravaImpuesto { get; }

        /// <summary>True si la operación es gratuita (21). En UBL implica PriceTypeCode="02".</summary>
        public bool EsGratuita { get; }

        private AfectacionImpuesto(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                throw new ArgumentException("Código de afectación obligatorio.", nameof(codigo));

            var norm = codigo.Trim();

            // Formato: exactamente 2 dígitos
            if (norm.Length != 2 || !char.IsDigit(norm[0]) || !char.IsDigit(norm[1]))
                throw new ArgumentException("El código debe tener exactamente dos dígitos (ej. \"10\").", nameof(codigo));

            if (!Mapa.TryGetValue(norm, out var info))
                throw new ArgumentOutOfRangeException(nameof(codigo), $"Código de afectación no reconocido: {norm}");

            Codigo        = norm;
            Categoria     = info.Categoria;
            TributoCodigo = info.TributoCodigo;
            TributoNombre = info.TributoNombre;
            GravaImpuesto = info.Grava;
            EsGratuita    = info.Gratuita;
        }

        /// <summary>Crea el VO validando contra Catálogo 07.</summary>
        public static AfectacionImpuesto From(string codigo) => new(codigo);

        /// <summary>Try sin excepciones.</summary>
        public static bool TryFrom(string codigo, out AfectacionImpuesto? afectacion)
        {
            try { afectacion = new AfectacionImpuesto(codigo); return true; }
            catch { afectacion = null; return false; }
        }

        public enum CategoriaAfectacion
        {
            Gravado,     // 10–16 (IGV)
            Exonerado,   // 20–21 (EXO/Gratuita)
            Inafecto,    // 30–36
            Exportacion, // 40
            IVAP         // 17
        }

        // ----- Mapa normativo Cat. 07 → Cat. 05 / semántica -----
        private sealed record Info(
            CategoriaAfectacion Categoria,
            string TributoCodigo,
            string TributoNombre,
            bool Grava,
            bool Gratuita);

        private static readonly IReadOnlyDictionary<string, Info> Mapa = new Dictionary<string, Info>
        {
            // GRAVADO IGV (tributo 1000)
            ["10"] = new Info(CategoriaAfectacion.Gravado, "1000", "IGV",  true,  false),
            ["11"] = new Info(CategoriaAfectacion.Gravado, "1000", "IGV",  true,  false),
            ["12"] = new Info(CategoriaAfectacion.Gravado, "1000", "IGV",  true,  false),
            ["13"] = new Info(CategoriaAfectacion.Gravado, "1000", "IGV",  true,  false),
            ["14"] = new Info(CategoriaAfectacion.Gravado, "1000", "IGV",  true,  false),
            ["15"] = new Info(CategoriaAfectacion.Gravado, "1000", "IGV",  true,  false),
            ["16"] = new Info(CategoriaAfectacion.Gravado, "1000", "IGV",  true,  false),

            // IVAP (tributo 1016)
            ["17"] = new Info(CategoriaAfectacion.IVAP,    "1016", "IVAP", true,  false),

            // EXONERADO (tributos 9997/9996)
            ["20"] = new Info(CategoriaAfectacion.Exonerado, "9997", "EXO", false, false), // Onerosa exonerada
            ["21"] = new Info(CategoriaAfectacion.Exonerado, "9996", "GRA", false, true ), // Transferencia gratuita

            // INAFECTO (tributo 9998)
            ["30"] = new Info(CategoriaAfectacion.Inafecto, "9998", "INA", false, false),
            ["31"] = new Info(CategoriaAfectacion.Inafecto, "9998", "INA", false, false),
            ["32"] = new Info(CategoriaAfectacion.Inafecto, "9998", "INA", false, false),
            ["33"] = new Info(CategoriaAfectacion.Inafecto, "9998", "INA", false, false),
            ["34"] = new Info(CategoriaAfectacion.Inafecto, "9998", "INA", false, false),
            ["35"] = new Info(CategoriaAfectacion.Inafecto, "9998", "INA", false, false),
            ["36"] = new Info(CategoriaAfectacion.Inafecto, "9998", "INA", false, false),

            // EXPORTACIÓN (tributo 9995)
            ["40"] = new Info(CategoriaAfectacion.Exportacion, "9995", "EXP", false, false),
        };

        // Atajos frecuentes
        public static readonly AfectacionImpuesto Gravado_10     = new("10");
        public static readonly AfectacionImpuesto Exonerado_20   = new("20");
        public static readonly AfectacionImpuesto Gratuita_21    = new("21");
        public static readonly AfectacionImpuesto Inafecto_30    = new("30");
        public static readonly AfectacionImpuesto Exportacion_40 = new("40");
        public static readonly AfectacionImpuesto IVAP_17        = new("17");

        // Helpers de dominio
        public bool EsIGV        => TributoCodigo == "1000"; // 10–16
        public bool EsIVAP       => TributoCodigo == "1016"; // 17
        public bool EsNoGravado  => !GravaImpuesto;          // 20,21,30–36,40

        public override string ToString() => Codigo;
    }
}
