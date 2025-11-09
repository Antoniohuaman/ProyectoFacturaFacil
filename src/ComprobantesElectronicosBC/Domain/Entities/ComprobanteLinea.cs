using System;
using System.Collections.Generic;
using ComprobantesElectronicosBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace ComprobantesElectronicosBC.Domain.Entities
{
    /// <summary>
    /// Entidad de LÍNEA de comprobante (Aggregate: ComprobanteElectronico).
    ///
    /// Responsabilidades:
    /// - Mantener datos “congelados” de la línea (descripción, UM, cantidad, precio, impuesto).
    /// - Calcular base/IGV/total según afectación (gravado/exonerado/inafecto/exportación).
    /// - Aplicar descuento de línea (monto o %) respetando reglas de moneda y límites.
    /// - Respetar redondeos contables (AwayFromZero) y escala por UM.
    ///
    /// Mapeo UBL (referencial):
    /// - <cac:InvoiceLine>
    ///   - <cbc:ID> = NumeroLinea
    ///   - <cbc:InvoicedQuantity unitCode=UM.Codigo> = Cantidad
    ///   - <cac:Item><cbc:Description> = DescripcionProducto.ToUblDescriptions()
    ///   - <cac:Price><cbc:PriceAmount> = UnitPriceSinIgv (unitario SIN IGV)
    ///   - <cac:AllowanceCharge> (línea) = descuento (si aplica)
    ///   - <cac:TaxTotal>/<cac:TaxSubtotal>/<cac:TaxCategory>/<cac:TaxScheme> = desde ImpuestoIGV
    /// </summary>
    public sealed class ComprobanteLinea
    {
        // --------------------- Identidad ---------------------
        /// <summary>Número de línea 1..N (UI/UBL &lt;cbc:ID&gt;).</summary>
        public int NumeroLinea { get; private set; }

        // --------------------- Datos nucleares ----------------
        public DescripcionProducto Descripcion { get; private set; }
        public UnidadDeMedida UM { get; private set; }
        public Cantidad Cantidad { get; private set; }

        /// <summary>Precio unitario declarado por el usuario (con su Moneda).</summary>
        public ImporteMonetario PrecioUnitario { get; private set; }

        /// <summary>Si true, el precio ingresado incluye IGV; si false, NO incluye IGV.</summary>
        public bool PrecioIncluyeIgv { get; private set; }

        /// <summary>Afectación de impuesto (Cat. 07) y tasa.</summary>
        public AfectacionImpuesto AfectacionImpuesto { get; private set; }
        public TasaImpuesto TasaImpuesto { get; private set; }

        /// <summary>Descuento de línea (monto o %). Por defecto: None.</summary>
        public DescuentoLinea Descuento { get; private set; }

        /// <summary>Centro de costo opcional por línea.</summary>
        public CentroDeCosto? CentroDeCosto { get; private set; }

        // --------------------- Montos calculados ----------------
        /// <summary>Unitario SIN IGV (para UBL &lt;PriceAmount&gt;).</summary>
        public ImporteMonetario UnitPriceSinIgv { get; private set; } = null!;

        /// <summary>Unitario CON IGV (solo para mostrar si el usuario trabaja “con IGV”).</summary>
        public ImporteMonetario UnitPriceConIgv { get; private set; } = null!;

        /// <summary>Base imponible ANTES de descuento.</summary>
        public ImporteMonetario BaseAntesDescuento { get; private set; } = null!;

        /// <summary>Monto de descuento aplicado (0 si no hay).</summary>
        public ImporteMonetario DescuentoMonto { get; private set; } = null!;

        /// <summary>Base imponible DESPUÉS de descuento.</summary>
        public ImporteMonetario BaseImponible { get; private set; } = null!;

        /// <summary>IGV calculado sobre la base después del descuento (0 si exo/ina/exp).</summary>
        public ImporteMonetario Igv { get; private set; } = null!;

        /// <summary>Total de la línea (base después de descuento + IGV).</summary>
        public ImporteMonetario ImporteTotal { get; private set; } = null!;

        /// <summary>Conveniencia: moneda única de la línea.</summary>
        public Moneda Moneda => PrecioUnitario.Moneda;

        // --------------------- Construcción -------------------

        private ComprobanteLinea(
            int numeroLinea,
            DescripcionProducto descripcion,
            UnidadDeMedida um,
            Cantidad cantidad,
            ImporteMonetario precioUnitario,
            bool precioIncluyeIgv,
            AfectacionImpuesto afectacionImpuesto,
            TasaImpuesto tasaImpuesto,
            DescuentoLinea? descuento,
            CentroDeCosto? centroDeCosto)
        {
            if (numeroLinea <= 0) throw new ArgumentOutOfRangeException(nameof(numeroLinea));

            // Validación de campos obligatorios con excepción de dominio
            if (descripcion == null || um == null || precioUnitario == null || afectacionImpuesto == null || tasaImpuesto == null)
            {
                var faltantes = new List<string>();
                if (descripcion == null) faltantes.Add(nameof(descripcion));
                if (um == null) faltantes.Add(nameof(um));
                if (precioUnitario == null) faltantes.Add(nameof(precioUnitario));
                if (afectacionImpuesto == null) faltantes.Add(nameof(afectacionImpuesto));
                if (tasaImpuesto == null) faltantes.Add(nameof(tasaImpuesto));
                var metadata = new Dictionary<string, object?> { { "CamposFaltantes", faltantes } };
                throw new Exceptions.CamposObligatoriosFaltantesException(
                    $"Faltan campos obligatorios en la línea: {string.Join(", ", faltantes)}.", metadata);
            }

            NumeroLinea        = numeroLinea;
            Descripcion        = descripcion;
            UM                 = um;
            Cantidad           = cantidad; // validación de escala debajo
            PrecioUnitario     = precioUnitario;
            PrecioIncluyeIgv   = precioIncluyeIgv;
            AfectacionImpuesto = afectacionImpuesto;
            TasaImpuesto       = tasaImpuesto;
            Descuento          = descuento ?? DescuentoLinea.None;
            CentroDeCosto      = centroDeCosto;

            // Inicializa importes en cero con la moneda del precio (evita CS8618)
            InicializarMontosEnCero();

            // Enforzar escala por UM y calcular montos
            EnforcePrecisionPorUM();
            Recalcular();
        }

        /// <summary>Fábrica principal.</summary>
        public static ComprobanteLinea Create(
            int numeroLinea,
            DescripcionProducto descripcion,
            UnidadDeMedida um,
            Cantidad cantidad,
            ImporteMonetario precioUnitario,
            bool precioIncluyeIgv,
            AfectacionImpuesto afectacionImpuesto,
            TasaImpuesto tasaImpuesto,
            DescuentoLinea? descuento = null,
            CentroDeCosto? centroDeCosto = null)
            => new(
                numeroLinea, descripcion, um, cantidad,
                precioUnitario, precioIncluyeIgv, afectacionImpuesto, tasaImpuesto,
                descuento, centroDeCosto);

        // --------------------- Comandos -----------------------

        public void CambiarDescripcion(DescripcionProducto nueva)
        {
            Descripcion = nueva ?? throw new ArgumentNullException(nameof(nueva));
        }

        public void CambiarUnidad(UnidadDeMedida nueva)
        {
            UM = nueva ?? throw new ArgumentNullException(nameof(nueva));
            EnforcePrecisionPorUM();
            Recalcular();
        }

        public void CambiarCantidad(Cantidad nueva)
        {
            Cantidad = nueva;
            EnforcePrecisionPorUM();
            Recalcular();
        }

        public void CambiarPrecio(ImporteMonetario nuevo, bool? incluyeIgv = null)
        {
            CambiarPrecio(nuevo, incluyeIgv, permitirCambioDeMoneda: false);
        }

        public void CambiarPrecio(ImporteMonetario nuevo, bool? incluyeIgv, bool permitirCambioDeMoneda)
        {
            if (nuevo is null) throw new ArgumentNullException(nameof(nuevo));
            if (!Equals(nuevo.Moneda, Moneda) && !permitirCambioDeMoneda)
                throw new ComprobantesElectronicosBC.Domain.Exceptions.ReglaDeNegocioException("No se puede cambiar la moneda dentro de la misma línea.");

            PrecioUnitario = nuevo;
            if (incluyeIgv.HasValue) PrecioIncluyeIgv = incluyeIgv.Value;
            Recalcular();
        }

        public void CambiarImpuesto(AfectacionImpuesto nuevaAfectacion, TasaImpuesto nuevaTasa)
        {
            AfectacionImpuesto = nuevaAfectacion ?? throw new ArgumentNullException(nameof(nuevaAfectacion));
            TasaImpuesto = nuevaTasa ?? throw new ArgumentNullException(nameof(nuevaTasa));
            Recalcular();
        }

        public void CambiarDescuento(DescuentoLinea? nuevo)
        {
            Descuento = nuevo ?? DescuentoLinea.None;
            Recalcular();
        }

        public void CambiarCentroDeCosto(CentroDeCosto? nuevo) => CentroDeCosto = nuevo;

        // --------------------- Cálculo ------------------------

        /// <summary>Calcula unitarios, base/IGV/total y aplica DescuentoLinea (si hay).</summary>
        private void Recalcular()
        {
            var moneda = Moneda;

            // 1) Montos base según afectación y si el precio incluye IGV
            var r = Descuento.Aplicar(
                afectacion: AfectacionImpuesto,
                tasa: TasaImpuesto,
                unitPriceEntrada: PrecioUnitario.Monto,
                cantidad: Cantidad,
                priceIncludesIgv: PrecioIncluyeIgv);

            BaseAntesDescuento = new ImporteMonetario(moneda, r.BaseAntes);
            DescuentoMonto     = new ImporteMonetario(moneda, r.Descuento);
            BaseImponible      = new ImporteMonetario(moneda, r.BaseDespues);
            Igv                = new ImporteMonetario(moneda, r.Igv);
            ImporteTotal       = new ImporteMonetario(moneda, r.Total);

            // Líneas gratuitas (afectación 21,31,32,33,34) no deben impactar bases ni totales del comprobante.
            // Mantienen UnitPrice para referencia pero montos de resumen = 0.
            var cod = AfectacionImpuesto.Codigo;
            if (cod is "21" or "31" or "32" or "33" or "34")
            {
                BaseAntesDescuento = ImporteMonetario.Zero(moneda);
                DescuentoMonto     = ImporteMonetario.Zero(moneda);
                BaseImponible      = ImporteMonetario.Zero(moneda);
                Igv                = ImporteMonetario.Zero(moneda);
                ImporteTotal       = ImporteMonetario.Zero(moneda);
            }

            // 2) UnitPriceSinIgv / UnitPriceConIgv coherentes con afectación y tasa
            decimal unitSin, unitCon;
            if (PrecioIncluyeIgv)
            {
                unitCon = PrecioUnitario.Monto;
                unitSin = AfectacionImpuesto.GravaImpuesto
                    ? Round2(unitCon / (1 + TasaImpuesto.Fraccion))
                    : unitCon;
            }
            else
            {
                unitSin = PrecioUnitario.Monto;
                unitCon = AfectacionImpuesto.GravaImpuesto
                    ? Round2(unitSin * (1 + TasaImpuesto.Fraccion))
                    : unitSin;
            }

            UnitPriceSinIgv = new ImporteMonetario(moneda, unitSin);
            UnitPriceConIgv = new ImporteMonetario(moneda, unitCon);
        }

        // --------------------- Validaciones auxiliares --------

        /// <summary>
        /// Enforcea la escala de <see cref="Cantidad"/> según la UM:
        /// NIU/E48 → 0; KGM/LTR/MTR → 3; otros → 6.
        /// </summary>
        private void EnforcePrecisionPorUM()
        {
            var code = UM.Codigo.ToUpperInvariant();
            var maxScale = code switch
            {
                "NIU" or "E48" => 0,
                "KGM" or "LTR" or "MTR" => 3,
                _ => 6
            };
            Cantidad = Cantidad.EnforceMaxScale(maxScale);
        }

        private void InicializarMontosEnCero()
        {
            var m = PrecioUnitario.Moneda;
            UnitPriceSinIgv    = ImporteMonetario.Zero(m);
            UnitPriceConIgv    = ImporteMonetario.Zero(m);
            BaseAntesDescuento = ImporteMonetario.Zero(m);
            DescuentoMonto     = ImporteMonetario.Zero(m);
            BaseImponible      = ImporteMonetario.Zero(m);
            Igv                = ImporteMonetario.Zero(m);
            ImporteTotal       = ImporteMonetario.Zero(m);
        }

        // --------------------- Consultas útiles ---------------

        public bool EsGravado => AfectacionImpuesto.GravaImpuesto;

        /// <summary>SubTotal sin IGV (ya considerando descuento).</summary>
        public ImporteMonetario SubtotalSinIgv => BaseImponible;

        /// <summary>Total de línea (para sumatoria de cabecera).</summary>
        public ImporteMonetario TotalLinea => ImporteTotal;

        public override string ToString()
            => $"#{NumeroLinea} {Cantidad.Value:0.######} {UM.Codigo} — {Descripcion.Nombre}: {ImporteTotal}";

        // --------------------- Helpers ------------------------
        private static decimal Round2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
    }
}
