using System;
using System.Collections.Generic;

namespace ComprobantesElectronicosBC.Domain.ValueObjects
{
    /// <summary>
    /// Value Object para el tipo de comprobante electrónico.
    /// Por ahora soporta:
    /// - "01" = Factura
    /// - "03" = Boleta
    ///
    /// Responsabilidades:
    /// - Encapsula el código SUNAT (InvoiceTypeCode) y su semántica.
    /// - Expone utilidades normativas (si exige RUC, días retroactivos).
    /// - Valida compatibilidad con la convención de serie UI (F*→01, B*→03) sin forzar formato UBL.
    ///
    /// Inmutable. Igualdad por valor.
    /// </summary>
    public sealed class TipoDeComprobante : IEquatable<TipoDeComprobante>
    {
        /// <summary> Código SUNAT UBL: "01" (Factura), "03" (Boleta). </summary>
        public string Codigo { get; }

        /// <summary> Nombre legible: "Factura" o "Boleta". </summary>
        public string Nombre { get; }

        /// <summary> Devuelve true si el tipo es Factura ("01"). </summary>
        public bool EsFactura => Codigo == "01";

        /// <summary> Devuelve true si el tipo es Boleta ("03"). </summary>
        public bool EsBoleta => Codigo == "03";

        /// <summary>
        /// Reglas de identificación del adquirente:
        /// - Factura: requiere RUC (Cat.06 = "6").
        /// - Boleta: permite DNI (Cat.06 = "1") o RUC.
        /// </summary>
        public bool RequiereRucCliente => EsFactura;

        /// <summary>
        /// Límite de emisión retroactiva referencial:
        /// - Factura: 3 días.
        /// - Boleta: 5 días.
        /// (La validación efectiva vive en el VO FechaEmision y/o Specification de Emisión.)
        /// </summary>
        public int MaxDiasRetroactivos => EsFactura ? 3 : 5;

        /// <summary>
        /// Código que se coloca en UBL /Invoice/cbc:InvoiceTypeCode.
        /// Igual al <see cref="Codigo"/>.
        /// </summary>
        public string UblInvoiceTypeCode => Codigo;

        /// <summary>
        /// Prefijo de serie sugerido para la UI:
        /// - Factura: "F"
        /// - Boleta:  "B"
        /// </summary>
        public string SeriePrefijoSugerido => EsFactura ? "F" : "B";

        #region Instancias conocidas (singletons)
        public static readonly TipoDeComprobante Factura = new("01", "Factura");
        public static readonly TipoDeComprobante Boleta  = new("03", "Boleta");
        #endregion

        private static readonly Dictionary<string, TipoDeComprobante> _porCodigo = new(StringComparer.OrdinalIgnoreCase)
        {
            ["01"] = Factura,
            ["03"] = Boleta
        };

        private static readonly Dictionary<string, TipoDeComprobante> _porNombre = new(StringComparer.OrdinalIgnoreCase)
        {
            ["FACTURA"] = Factura,
            ["BOLETA"]  = Boleta
        };

        private TipoDeComprobante(string codigo, string nombre)
        {
            Codigo = codigo;
            Nombre = nombre;
        }

        /// <summary>
        /// Crea un TipoDeComprobante a partir de código ("01"/"03") o nombre ("Factura"/"Boleta").
        /// </summary>
        public static TipoDeComprobante Create(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException("El tipo de comprobante es obligatorio.", nameof(input));

            var norm = input.Trim();

            if (_porCodigo.TryGetValue(norm, out var byCode))
                return byCode;

            if (_porNombre.TryGetValue(norm.ToUpperInvariant(), out var byName))
                return byName;

            throw new ArgumentException("Tipo de comprobante no soportado. Use '01' (Factura) o '03' (Boleta).", nameof(input));
        }

        /// <summary>
        /// Intenta crear a partir de código o nombre. Devuelve false si no es válido.
        /// </summary>
        public static bool TryCreate(string? input, out TipoDeComprobante? tipo)
        {
            tipo = default;
            if (string.IsNullOrWhiteSpace(input)) return false;

            if (_porCodigo.TryGetValue(input.Trim(), out var byCode))
            {
                tipo = byCode;
                return true;
            }

            if (_porNombre.TryGetValue(input.Trim().ToUpperInvariant(), out var byName))
            {
                tipo = byName;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Dada una serie (1..4 alfanumérica), valida la compatibilidad con la convención de UI:
        /// - Si la serie inicia con 'F'/'f' => debe ser Factura ("01").
        /// - Si la serie inicia con 'B'/'b' => debe ser Boleta  ("03").
        /// - En otros prefijos alfanuméricos no impone restricción (la norma UBL no exige prefijo).
        /// Lanza excepción si es incompatible o si la serie es inválida.
        /// </summary>
        public void ValidarCompatibilidadConSerie(string serie)
        {
            if (string.IsNullOrWhiteSpace(serie))
                throw new ArgumentException("La serie es obligatoria.", nameof(serie));

            var s = serie.Trim().ToUpperInvariant();
            if (s.Length is < 1 or > 4)
                throw new ArgumentException("La serie debe tener entre 1 y 4 caracteres alfanuméricos.", nameof(serie));

            foreach (var ch in s)
            {
                var esAlfaNum = (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9');
                if (!esAlfaNum)
                    throw new ArgumentException("La serie solo admite caracteres alfanuméricos (A–Z, 0–9).", nameof(serie));
            }

            if (s.StartsWith('F') && !EsFactura)
                throw new ComprobantesElectronicosBC.Domain.Exceptions.ReglaDeNegocioException("Serie con prefijo 'F' corresponde a Factura (01).");

            if (s.StartsWith('B') && !EsBoleta)
                throw new ComprobantesElectronicosBC.Domain.Exceptions.ReglaDeNegocioException("Serie con prefijo 'B' corresponde a Boleta (03).");
        }

        /// <summary>
        /// Dado un prefijo de serie (F*/B*), infiere el TipoDeComprobante bajo la convención UI.
        /// Si no inicia con F o B, devuelve null (no infiere).
        /// </summary>
        public static TipoDeComprobante? InferirDesdeSerie(string serie)
        {
            if (string.IsNullOrWhiteSpace(serie)) return null;
            var s = serie.Trim().ToUpperInvariant();
            if (s.StartsWith('F')) return Factura;
            if (s.StartsWith('B')) return Boleta;
            return null;
        }

        public override string ToString() => $"{Codigo} – {Nombre}";

        #region Igualdad por valor
        public bool Equals(TipoDeComprobante? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Codigo == other.Codigo && string.Equals(Nombre, other.Nombre, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj) => obj is TipoDeComprobante t && Equals(t);

        public override int GetHashCode() => HashCode.Combine(Codigo, Nombre.ToUpperInvariant());
        #endregion
    }
}
