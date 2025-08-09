using System;
using System.Collections.Generic;

namespace ComprobantesElectronicosBC.Domain.ValueObjects
{
    /// <summary>
    /// VO de afectación IGV de una línea/operación (SUNAT Cat. 07) + tasa IGV cuando corresponde.
    /// - Afectación (Cat. 07): "10"(gravado), "20"/"21"(exonerado), "30".."36"(inafecto), "40"(exportación)
    /// - TaxScheme (Cat. 05): 1000=IGV, 9998=Exonerado, 9997=Inafecto, 9995=Exportación
    /// - Percent (UBL): tasa IGV en %, p.ej. 10.00 o 18.00 cuando "10"
    /// </summary>
    public sealed record ImpuestoIGV
    {
        /// <summary>Código de afectación (Cat. 07).</summary>
        public string AfectacionCode { get; }

        /// <summary>Tasa IGV (fracción) cuando es gravado (10). Null en exonerado/inafecto/exportación.</summary>
        public decimal? IgvRate { get; }

    public ImpuestoIGV(string afectacionCode, decimal? igvRate)
        {
            AfectacionCode = afectacionCode;
            IgvRate = igvRate;
        }

        // Catálogo 07 soportado
        private static readonly HashSet<string> ValidAfectacion = new(StringComparer.Ordinal)
        { "10","20","21","30","31","32","33","34","35","36","40" };

        /// <summary>
        /// Fábrica general con validaciones normativas:
        /// - "10" ⇒ IgvRate obligatorio y solo 0.10 o 0.18
        /// - Otros ⇒ IgvRate debe ser null (se ignora si viene con valor)
        /// </summary>
        public static ImpuestoIGV Create(string afectacionCode, decimal? igvRate)
        {
            afectacionCode = string.IsNullOrWhiteSpace(afectacionCode) ? "" : afectacionCode.Trim();

            if (!ValidAfectacion.Contains(afectacionCode))
                throw new ArgumentException($"Código afectación (Cat. 07) inválido: {afectacionCode}", nameof(afectacionCode));

            if (afectacionCode == "10")
            {
                if (igvRate is null || (igvRate.Value != 0.10m && igvRate.Value != 0.18m))
                    throw new ArgumentException("Para afectación 10 (gravado) la tasa debe ser 0.10 o 0.18.", nameof(igvRate));
            }
            else
            {
                // En EXO/INA/EXP la tasa no aplica
                igvRate = null;
            }

            return new ImpuestoIGV(afectacionCode, igvRate);
        }

        // Atajos comunes (útiles para tests o construcción desde Catálogo de Artículos)
        public static ImpuestoIGV Gravado10() => Create("10", 0.10m);
        public static ImpuestoIGV Gravado18() => Create("10", 0.18m);
        public static ImpuestoIGV Exonerado() => Create("20", null);
        public static ImpuestoIGV ExoneradoGratuito() => Create("21", null);
        public static ImpuestoIGV Inafecto() => Create("30", null);
        public static ImpuestoIGV Exportacion() => Create("40", null);

        /// <summary>Mapeo Cat. 07 → Cat. 05 (TaxScheme/ID).</summary>
        public string TaxSchemeId => AfectacionCode switch
        {
            "10" => "1000", // IGV
            "20" or "21" => "9998", // Exonerado
            "30" or "31" or "32" or "33" or "34" or "35" or "36" => "9997", // Inafecto
            "40" => "9995", // Exportación
            _ => throw new InvalidOperationException($"Afectación no soportada: {AfectacionCode}")
        };

        /// <summary>Tasa en porcentaje para UBL (10.00 / 18.00). Null si no aplica.</summary>
        public decimal? Percent => IgvRate is null ? null : Math.Round(IgvRate.Value * 100m, 2);

        /// <summary>¿Es afectación gravada?</summary>
        public bool EsGravado => AfectacionCode == "10";

        /// <summary>
        /// Resultado de cálculo para la línea (redondeo monetario a 2 decimales).
        /// </summary>
        public readonly record struct Montos(
            decimal UnitPriceSinIgv,
            decimal UnitPriceConIgv,
            decimal BaseImponible,
            decimal Igv,
            decimal ImporteTotal
        );

        /// <summary>
        /// Calcula montos de línea (precio unitario, cantidad, indicador si el precio incluye IGV).
        /// - Gravado(10):
        ///   * priceIncludesIgv=false → base = precio; igv = base*rate; total = base+igv
        ///   * priceIncludesIgv=true  → base = precio/(1+rate); igv = total-base
        /// - EXO/INA/EXP: IGV=0; base=precio; total=base
        /// </summary>
        public Montos CalcularMontos(decimal unitPrice, decimal quantity, bool priceIncludesIgv)
        {
            if (unitPrice < 0) throw new ArgumentOutOfRangeException(nameof(unitPrice), "El precio unitario no puede ser negativo.");
            if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "La cantidad debe ser > 0.");

            decimal unitSin, unitCon, baseImp, igv, total;

            if (EsGravado)
            {
                var rate = IgvRate!.Value; // validado en Create
                if (priceIncludesIgv)
                {
                    unitCon = Round2(unitPrice);
                    unitSin = Math.Round(unitPrice / (1m + rate), 6); // más precisión antes de totalizar
                }
                else
                {
                    unitSin = Round2(unitPrice);
                    unitCon = Math.Round(unitPrice * (1m + rate), 6);
                }

                baseImp = Round2(unitSin * quantity);
                total   = Round2(unitCon * quantity);
                igv     = Round2(total - baseImp);
            }
            else
            {
                unitSin = unitPrice;
                unitCon = unitPrice;
                baseImp = Round2(unitPrice * quantity);
                igv     = 0m;
                total   = baseImp;
            }

            return new(
                UnitPriceSinIgv: Round2(unitSin),
                UnitPriceConIgv: Round2(unitCon),
                BaseImponible:   baseImp,
                Igv:             igv,
                ImporteTotal:    total
            );
        }

        private static decimal Round2(decimal v)
            => Math.Round(v, 2, MidpointRounding.AwayFromZero);
    }
}
