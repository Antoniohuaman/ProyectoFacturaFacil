#nullable enable
using System;
using ListaPreciosBC.Domain.Aggregates;
using ListaPreciosBC.Domain.ValueObjects;
using SharedKernel.ValueObjects; // Sku, AfectacionImpuesto

namespace ListaPreciosBC.Domain.Services
{
    /// <summary>
    /// Servicio de dominio puro para resolver el precio de un SKU.
    /// Orquesta: columna (o base), fijo/volumen, afectación y neto/bruto.
    /// </summary>
    public sealed class ResolverPrecioService
    {
        /// <summary>
        /// Resuelve el precio para un SKU dado usando el agregado <see cref="PrecioProducto"/>.
        /// Si no se indica columna, intenta usar la Base (P1) o la de la plantilla (si se provee).
        /// Devuelve un DTO con neto, bruto, origen y tramo (si aplica).
        /// </summary>
        /// <param name="producto">Agregado de precios por SKU.</param>
        /// <param name="columnaSolicitada">Columna objetivo (P1..P10) o null para usar Base.</param>
        /// <param name="cantidad">Cantidad solicitada (≥ 1).</param>
        /// <param name="fecha">Fecha de vigencia a evaluar.</param>
        /// <param name="afectacion">Afectación de impuesto (puede ser null → no grava).</param>
        /// <param name="tasaImpuestoFraccion">Tasa fraccional (p.ej. 0.18 para IGV 18%).</param>
        /// <param name="plantillaOpcional">
        /// Plantilla de columnas para determinar la Base si no se indicó columna.
        /// Si es null, se asume Base = P1.
        /// </param>
        /// <param name="mostrarConImpuestos">
        /// Si <c>true</c>, el campo <see cref="PrecioCalculadoDto.Mostrar"/> devuelve BRUTO; si no, NETO.
        /// </param>
        public PrecioCalculadoDto? Resolver(
            PrecioProducto producto,
            IdentificadorColumnaPrecio? columnaSolicitada,
            int cantidad,
            DateTimeOffset fecha,
            AfectacionImpuesto? afectacion,
            decimal tasaImpuestoFraccion,
            PlantillaColumnasPrecio? plantillaOpcional = null,
            bool mostrarConImpuestos = true)
        {
            if (producto is null) throw new ArgumentNullException(nameof(producto));
            if (cantidad < 1) return null;

            // 1) Determinar columna objetivo
            var baseNum = ObtenerNumeroColumnaBase(plantillaOpcional);
            var objetivo = columnaSolicitada ?? IdentificadorColumnaPrecio.DesdeNumero(baseNum);

            // 2) Intentar resolver en la columna objetivo
            var res = producto.ObtenerPrecioVigente(objetivo, fecha, cantidad);

            // 2b) Caso borde: si no hay en columna objetivo, fallback a Base (si no era ya la base)
            if (res is null && objetivo.Numero != baseNum)
            {
                var colBase = IdentificadorColumnaPrecio.DesdeNumero(baseNum);
                res = producto.ObtenerPrecioVigente(colBase, fecha, cantidad);
                if (res is not null) objetivo = colBase; // reflejar que aplicó Base
            }

            if (res is null) return null;

            // 3) Calcular neto/bruto según afectación + flag IncluyeImpuesto del ValorPrecio
            var valor = res.Valor;
            var neto  = valor.Neto(afectacion!, tasaImpuestoFraccion);
            var bruto = valor.Bruto(afectacion!, tasaImpuestoFraccion);

            // 4) Si el origen fue por volumen, averiguar tramo aplicado para el DTO
            int? tramoDesde = null, tramoHasta = null;
            if (res.Origen == PrecioResueltoOrigen.PorVolumen)
            {
                var key = objetivo.Numero;
                if (producto.MatricesVolumen.TryGetValue(key, out var matriz))
                {
                    var tramo = matriz.ObtenerTramo(cantidad);
                    if (tramo is not null)
                    {
                        tramoDesde = tramo.MinCantidad;
                        tramoHasta = tramo.MaxCantidad;
                    }
                }
            }

            return new PrecioCalculadoDto(
                producto.Sku,
                objetivo,
                valor,
                neto,
                bruto,
                mostrarConImpuestos ? bruto : neto,
                res.Origen,
                cantidad,
                tramoDesde,
                tramoHasta,
                afectacion
            );
        }

        private static byte ObtenerNumeroColumnaBase(PlantillaColumnasPrecio? plantilla)
        {
            // Si tu Plantilla expone un método o propiedad para base, úsalo aquí.
            // Ejemplos posibles (ajusta a tu implementación real):
            //  - return plantilla!.IdColumnaBase.Numero;
            //  - return plantilla!.Base().Id.Numero;
            // Aquí, por defecto, asumimos P1 si no hay plantilla.
            if (plantilla is null) return 1;

            // Fallbacks comunes (descomenta el que tengas):
            // return plantilla.IdColumnaBase.Numero;
            // return plantilla.ObtenerColumnaBase().Numero;

            // Si aún no tienes API pública, por ahora:
            return 1; // ← AJUSTA cuando expongas la columna Base desde PlantillaColumnasPrecio
        }
    }

    /// <summary>DTO de salida del resolvedor de precio.</summary>
    public sealed record PrecioCalculadoDto(
        Sku Sku,
        IdentificadorColumnaPrecio ColumnaAplicada,
        ValorPrecio ValorOriginal,
        Dinero Neto,
        Dinero Bruto,
        Dinero Mostrar,
        PrecioResueltoOrigen Origen,
        int Cantidad,
        int? TramoDesde,
        int? TramoHasta,
        AfectacionImpuesto? Afectacion
    );
}
