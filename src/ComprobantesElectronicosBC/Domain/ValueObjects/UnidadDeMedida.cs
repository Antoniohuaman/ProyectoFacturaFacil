using System;
using System.Text.RegularExpressions;

namespace ComprobantesElectronicosBC.Domain.ValueObjects
{
    /// <summary>
    /// VO de Unidad de Medida para líneas de comprobante.
    /// - Código normativo UN/ECE Rec 20 (lo que UBL pone en unitCode): p.ej. NIU, E48, KGM, LTR, MTR, ZZ.
    /// - Nombre/etiqueta opcional para UI/PDF (“Unidad”, “Kilogramo”, …).
    /// - Este BC **no define** unidades; solo referencia la que viene del producto.
    /// </summary>
    public sealed record UnidadDeMedida
    {
        /// <summary>
        /// Código UN/ECE Rec 20 (mayúsculas, 2..3 alfanum). Ej.: NIU, E48, KGM, LTR, ZZ.
        /// </summary>
        public string Codigo { get; }

        /// <summary>
        /// Etiqueta legible (opcional) para UI/PDF. Ej.: "Unidad", "Kilogramo".
        /// </summary>
        public string? Nombre { get; }

        private UnidadDeMedida(string codigo, string? nombre)
        {
            Codigo = codigo;
            Nombre = nombre;
        }

        /// <summary>
        /// Crea una unidad válida. Normaliza a MAYÚSCULAS y recorta espacios.
        /// Valida forma del código (2..3 alfanum) según uso típico en UBL/Rec 20.
        /// </summary>
        public static UnidadDeMedida Create(string codigo, string? nombre = null)
        {
            if (codigo is null) throw new ArgumentNullException(nameof(codigo));

            var c = codigo.Trim().ToUpperInvariant();
            // Rec 20 y UBL usan códigos cortos alfanum (p.ej. NIU, E48, KGM, ZZ). Restringimos a 2..3.
            if (!Regex.IsMatch(c, "^[A-Z0-9]{2,3}$"))
                throw new ArgumentException("El código de unidad debe ser alfanumérico de 2 a 3 caracteres (UN/ECE Rec 20).", nameof(codigo));

            string? n = string.IsNullOrWhiteSpace(nombre) ? null : nombre.Trim();
            if (n is { Length: > 60 })
                throw new ArgumentException("El nombre de la unidad no debe exceder 60 caracteres.", nameof(nombre));

            return new UnidadDeMedida(c, n);
        }

        /// <summary>
        /// Intenta crear sin lanzar excepciones.
        /// </summary>
        public static bool TryCreate(string? codigo, string? nombre, out UnidadDeMedida? um)
        {
            try { um = Create(codigo!, nombre); return true; }
            catch { um = null; return false; }
        }

        // --------- Atajos comunes (útiles en tests/UI) ---------

        /// <summary>NIU = unidad (conteo de ítems) – el más común en Perú.</summary>
        public static UnidadDeMedida NIU(string? nombre = "Unidad") => Create("NIU", nombre);

        /// <summary>E48 = unidad de servicio (muy usada en servicios).</summary>
        public static UnidadDeMedida E48(string? nombre = "Servicio") => Create("E48", nombre);

        /// <summary>KGM = kilogramo.</summary>
        public static UnidadDeMedida KGM(string? nombre = "Kilogramo") => Create("KGM", nombre);

        /// <summary>LTR = litro.</summary>
        public static UnidadDeMedida LTR(string? nombre = "Litro") => Create("LTR", nombre);

        /// <summary>MTR = metro.</summary>
        public static UnidadDeMedida MTR(string? nombre = "Metro") => Create("MTR", nombre);

        /// <summary>ZZ = unidad definida mutuamente (casos especiales).</summary>
        public static UnidadDeMedida ZZ(string? nombre = "Unidad definida") => Create("ZZ", nombre);

        // --------- Ayudas para serializar a UBL ---------

        /// <summary>Valor que va en cbc:InvoicedQuantity@unitCode.</summary>
        public string UblUnitCode => Codigo;

        /// <summary>Metadatos sugeridos por UBL para unitCode (opcionales pero correctos).</summary>
        public const string UblUnitCodeListID = "UN/ECE rec 20";
        public const string UblUnitCodeListAgencyName = "United Nations Economic Commission for Europe";

        public override string ToString() => Nombre is null ? Codigo : $"{Codigo} ({Nombre})";
    }
}
