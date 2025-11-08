using System;
using System.Collections.Generic;
using System.Linq;
using ComprobantesElectronicosBC.Domain.Entities;
using ComprobantesElectronicosBC.Domain.ValueObjects;

namespace ComprobantesElectronicosBC.Domain.Services
{
    /// <summary>
    /// Servicio de dominio puro para cálculo de totales y prorrateo de descuento global.
    /// No mantiene estado.
    /// </summary>
    public static class ComprobanteTotalesService
    {
        public readonly record struct Totales(decimal SubtotalBase, decimal DescuentoGlobalMonto, decimal IgvTotal, decimal Total);

        public static Totales Calcular(IReadOnlyList<ComprobanteLinea> lineas, DescuentoGlobal descuento)
        {
            if (lineas is null || lineas.Count == 0)
                return new Totales(0m, 0m, 0m, 0m);

            // 1) Base total (después de descuento por línea)
            var baseTotal = lineas.Sum(l => l.BaseImponible.Monto);
            var subtotalBase = Round2(baseTotal);

            // 2) Descuento global
            var descMonto = Round2(descuento.CalcularMontoDescuento(subtotalBase));
            var baseNeta = Round2(subtotalBase - descMonto);

            // 3) IGV total
            decimal igvTotal;
            if (descuento.EsNinguno)
            {
                igvTotal = lineas.Sum(l => l.Igv.Monto);
            }
            else
            {
                igvTotal = 0m;
                for (int i = 0; i < lineas.Count; i++)
                {
                    var linea = lineas[i];
                    decimal share = descuento.Modo switch
                    {
                        DescuentoGlobalModo.Porcentaje => Round6(linea.BaseImponible.Monto * descuento.Valor),
                        DescuentoGlobalModo.Monto      => subtotalBase == 0m ? 0m : Round6(descMonto * (linea.BaseImponible.Monto / subtotalBase)),
                        _                              => 0m
                    };
                    var baseLineaTrasGlobal = Round2(linea.BaseImponible.Monto - share);
                    var igvLinea = linea.AfectacionImpuesto.GravaImpuesto
                        ? Round2(baseLineaTrasGlobal * linea.TasaImpuesto.Fraccion)
                        : 0m;
                    igvTotal += igvLinea;
                }
            }

            var total = Round2(baseNeta + igvTotal);
            return new Totales(subtotalBase, descMonto, igvTotal, total);
        }

        private static decimal Round2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
        private static decimal Round6(decimal v) => Math.Round(v, 6, MidpointRounding.AwayFromZero);
    }
}
