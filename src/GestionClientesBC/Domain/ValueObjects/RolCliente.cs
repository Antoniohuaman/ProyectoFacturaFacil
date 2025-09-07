using System;
using SharedKernel.Exceptions; // BusinessRuleException

namespace GestionClientesBC.Domain.ValueObjects
{
    /// <summary>
    /// Rol de cliente (segmentación comercial): Mayorista, Minorista, Distribuidor, Revendedor, SinDefinir.
    /// - Value Object con identidad por "Código" (3 letras).
    /// - Inmutable, comparación por valor (Código).
    /// - Fábricas desde código o nombre, case-insensitive.
    /// - Pensado para usarse junto a un "Tipo de cliente" (cliente/proveedor) que es otro concepto.
    /// </summary>
    public sealed class RolCliente : IEquatable<RolCliente>
    {
        // Códigos canónicos (3 letras). Útiles para persistencia y switches.
        public const string COD_SIN = "SIN";
        public const string COD_MAY = "MAY";
        public const string COD_MIN = "MIN";
        public const string COD_DIS = "DIS";
        public const string COD_REV = "REV";

    public string Codigo { get; }  // p.ej. "MAY"
    public string Nombre { get; }  // p.ej. "Mayorista"

    // Instancias conocidas (singletons)
    public static readonly RolCliente SinDefinir   = new RolCliente(COD_SIN, "Sin definir");
    public static readonly RolCliente Mayorista    = new RolCliente(COD_MAY, "Mayorista");
    public static readonly RolCliente Minorista    = new RolCliente(COD_MIN, "Minorista");
    public static readonly RolCliente Distribuidor = new RolCliente(COD_DIS, "Distribuidor");
    public static readonly RolCliente Revendedor   = new RolCliente(COD_REV, "Revendedor");

        // Para EF Core
        private RolCliente() { Codigo = null!; Nombre = null!; }

        private RolCliente(string codigo, string nombre)
        {
            Codigo = codigo;
            Nombre = nombre;
        }

        /// <summary>
        /// Obtiene la instancia conocida a partir de su código (case-insensitive).
        /// </summary>
        public static RolCliente DesdeCodigo(string? codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                throw new BusinessRuleException("El código de RolCliente no puede estar vacío.");

            switch (codigo.Trim().ToUpperInvariant())
            {
                case COD_SIN: return SinDefinir;
                case COD_MAY: return Mayorista;
                case COD_MIN: return Minorista;
                case COD_DIS: return Distribuidor;
                case COD_REV: return Revendedor;
                default:
                    throw new BusinessRuleException($"Código de RolCliente inválido: '{codigo}'. Valores: {COD_SIN},{COD_MAY},{COD_MIN},{COD_DIS},{COD_REV}.");
            }
        }

        /// <summary>
        /// Obtiene la instancia conocida desde su nombre (case-insensitive).
        /// </summary>
        public static RolCliente DesdeNombre(string? nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new BusinessRuleException("El nombre de RolCliente no puede estar vacío.");

            var n = nombre.Trim().ToLowerInvariant();
            return n switch
            {
                "sin definir"  => SinDefinir,
                "mayorista"    => Mayorista,
                "minorista"    => Minorista,
                "distribuidor" => Distribuidor,
                "revendedor"   => Revendedor,
                _ => throw new BusinessRuleException($"Nombre de RolCliente inválido: '{nombre}'.")
            };
        }

    public override string ToString() => Nombre;

        #region Igualdad por valor
    public bool Equals(RolCliente? other) => other is not null && Codigo == other.Codigo;
    public override bool Equals(object? obj) => obj is RolCliente t && Equals(t);
    public override int GetHashCode() => Codigo.GetHashCode(StringComparison.Ordinal);
        #endregion
    }
}
