using System;
using System.Diagnostics;
using SharedKernel.ValueObjects;

namespace ConfiguracionSistemaBC.Domain.ValueObjects
{
    /// <summary>
    /// Modo de captura/presentación de precios para la empresa (VO local del BC de Configuración).
    /// Solo dos estados:
    ///  - IncluyeIGV  → el campo editable es el Precio de Venta (con IGV).
    ///  - SinIGV      → el campo editable es el Valor de Venta (sin IGV).
    ///
    /// Reglas:
    /// - Es inmutable y se compara por valor (record).
    /// - Provee helpers para obtener el trío (VV, IGV, PV) a partir del campo editable y la tasa.
    /// - Los cálculos usan Dinero (respeta decimales de la moneda y redondeo AwayFromZero).
    /// - La tasa de IGV debe estar en el rango [0, 1] (ej.: 0.18).
    /// </summary>
    [DebuggerDisplay("{Codigo}")]
    public sealed record ModoPrecio
    {
        /// <summary>Código interno persistible: "INCLUYE_IGV" o "SIN_IGV".</summary>
        public string Codigo { get; }

        private ModoPrecio(string codigo) => Codigo = codigo;

        /// <summary>Modo: el precio ingresado/mostrado incluye IGV.</summary>
        public static readonly ModoPrecio IncluyeIGV = new("INCLUYE_IGV");

        /// <summary>Modo: el precio ingresado/mostrado NO incluye IGV.</summary>
        public static readonly ModoPrecio SinIGV = new("SIN_IGV");

        /// <summary>Indica si el modo actual captura precio con IGV.</summary>
        public bool EsConIGV => ReferenceEquals(this, IncluyeIGV) || Codigo == "INCLUYE_IGV";

        /// <summary>Indica si el modo actual captura precio sin IGV.</summary>
        public bool EsSinIGV => ReferenceEquals(this, SinIGV) || Codigo == "SIN_IGV";

        /// <summary>
        /// Crea el VO a partir de un código persistido (case-insensitive).
        /// Acepta "INCLUYE_IGV" o "SIN_IGV"; lanza para cualquier otro valor.
        /// </summary>
        public static ModoPrecio From(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo)) throw new ArgumentException("Código inválido.", nameof(codigo));
            var c = codigo.Trim().ToUpperInvariant();

            return c switch
            {
                "INCLUYE_IGV" => IncluyeIGV,
                "SIN_IGV"     => SinIGV,
                _ => throw new ArgumentOutOfRangeException(nameof(codigo), $"Código desconocido: {codigo}")
            };
        }

        /// <summary>
        /// Intenta crear el VO a partir de un código persistido (case-insensitive).
        /// </summary>
        public static bool TryFrom(string codigo, out ModoPrecio? modo)
        {
            modo = null;
            if (string.IsNullOrWhiteSpace(codigo)) return false;

            var c = codigo.Trim().ToUpperInvariant();
            if (c == "INCLUYE_IGV") { modo = IncluyeIGV; return true; }
            if (c == "SIN_IGV")     { modo = SinIGV;     return true; }
            return false;
        }

        /// <summary>
        /// Calcula el trío (ValorVenta, IGV, PrecioVenta) a partir del CAMPO EDITABLE
        /// según el modo actual y una tasa de IGV (ej.: 0.18).
        /// - Si EsConIGV: 'editable' es PrecioVenta → VV = PV/(1+t), IGV = VV*t, PV = editable.
        /// - Si EsSinIGV: 'editable' es ValorVenta  → PV = VV*(1+t), IGV = VV*t, VV = editable.
        /// </summary>
        /// <param name="editable">Dinero en la moneda de la empresa.</param>
        /// <param name="tasaIGV">Tasa en [0,1], p.ej. 0.18 para 18%.</param>
        public (Dinero ValorVenta, Dinero IGV, Dinero PrecioVenta) CalcularDesdeEditable(Dinero editable, decimal tasaIGV)
        {
            ValidarTasa(tasaIGV);

            if (EsConIGV)
            {
                // editable = Precio con IGV
                var pv = editable;
                var vv = pv.Dividir(1m + tasaIGV);
                var igv = vv.Multiplicar(tasaIGV);
                return (vv, igv, pv);
            }
            else
            {
                // editable = Valor sin IGV
                var vv = editable;
                var igv = vv.Multiplicar(tasaIGV);
                var pv = vv.Multiplicar(1m + tasaIGV);
                return (vv, igv, pv);
            }
        }

        /// <summary>
        /// Convierte un Precio con IGV (PV) a (VV, IGV, PV).
        /// Útil cuando el usuario cambia el modo pero ya tenía PV.
        /// </summary>
        public (Dinero ValorVenta, Dinero IGV, Dinero PrecioVenta) DesdePrecioConIGV(Dinero precioConIGV, decimal tasaIGV)
        {
            ValidarTasa(tasaIGV);
            var vv = precioConIGV.Dividir(1m + tasaIGV);
            var igv = vv.Multiplicar(tasaIGV);
            return (vv, igv, precioConIGV);
        }

        /// <summary>
        /// Convierte un Valor sin IGV (VV) a (VV, IGV, PV).
        /// Útil cuando el usuario cambia el modo pero ya tenía VV.
        /// </summary>
        public (Dinero ValorVenta, Dinero IGV, Dinero PrecioVenta) DesdeValorSinIGV(Dinero valorSinIGV, decimal tasaIGV)
        {
            ValidarTasa(tasaIGV);
            var igv = valorSinIGV.Multiplicar(tasaIGV);
            var pv  = valorSinIGV.Multiplicar(1m + tasaIGV);
            return (valorSinIGV, igv, pv);
        }

        private static void ValidarTasa(decimal tasaIGV)
        {
            if (tasaIGV < 0m || tasaIGV > 1m)
                throw new ArgumentOutOfRangeException(nameof(tasaIGV), "La tasa de IGV debe estar entre 0 y 1 (ej.: 0.18).");
        }

        public override string ToString() => Codigo;
    }
}
