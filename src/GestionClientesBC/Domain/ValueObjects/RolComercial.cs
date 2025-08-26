using System;
using SharedKernel.Exceptions; // BusinessRuleException

namespace GestionClientesBC.Domain.ValueObjects
{
    /// <summary>
    /// Rol comercial de un tercero: Cliente, Proveedor o ambos.
    /// Es independiente del "Tipo de cliente" (mayorista/minorista...).
    /// - Inmutable, comparación por valor (máscara 0..3)
    /// - Fábricas desde código y booleanos
    /// - Guards para operaciones (venta/compra)
    /// </summary>
    public sealed class RolComercial : IEquatable<RolComercial>
    {
        // Bits
        private const int BIT_CLIENTE   = 1; // 01
        private const int BIT_PROVEEDOR = 2; // 10

        // Máscara 0..3
        public int Mascara { get; }

        // Códigos para persistencia/simple IO
        // N: ninguno | C: cliente | P: proveedor | CP: cliente-proveedor
        public string Codigo => Mascara switch
        {
            0 => "N",
            BIT_CLIENTE => "C",
            BIT_PROVEEDOR => "P",
            BIT_CLIENTE | BIT_PROVEEDOR => "CP",
            _ => "N"
        };

        public string Nombre => Mascara switch
        {
            0 => "Sin rol",
            BIT_CLIENTE => "Cliente",
            BIT_PROVEEDOR => "Proveedor",
            BIT_CLIENTE | BIT_PROVEEDOR => "Cliente/Proveedor",
            _ => "Sin rol"
        };

        public bool EsCliente   => (Mascara & BIT_CLIENTE) != 0;
        public bool EsProveedor => (Mascara & BIT_PROVEEDOR) != 0;
        public bool EsAmbos     => Mascara == (BIT_CLIENTE | BIT_PROVEEDOR);
        public bool EstaSinRol  => Mascara == 0;

        // Instancias estáticas (cache 0..3)
        public static readonly RolComercial Ninguno          = new RolComercial(0);
        public static readonly RolComercial SoloCliente      = new RolComercial(BIT_CLIENTE);
        public static readonly RolComercial SoloProveedor    = new RolComercial(BIT_PROVEEDOR);
        public static readonly RolComercial ClienteProveedor = new RolComercial(BIT_CLIENTE | BIT_PROVEEDOR);

        // EF Core
        private RolComercial() { Mascara = 0; }
        private RolComercial(int mascara) { Mascara = mascara & 0b11; }

        private static RolComercial DesdeMascara(int m) => m switch
        {
            0 => Ninguno,
            BIT_CLIENTE => SoloCliente,
            BIT_PROVEEDOR => SoloProveedor,
            BIT_CLIENTE | BIT_PROVEEDOR => ClienteProveedor,
            _ => Ninguno
        };

        /// <summary>
        /// Fábrica desde código: "N", "C", "P", "CP" (case-insensitive, ignora espacios).
        /// </summary>
        public static RolComercial DesdeCodigo(string? codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                throw new BusinessRuleException("El código de rol comercial no puede estar vacío.");

            var c = codigo.Trim().ToUpperInvariant();
            return c switch
            {
                "N"  => Ninguno,
                "C"  => SoloCliente,
                "P"  => SoloProveedor,
                "CP" => ClienteProveedor,
                _ => throw new BusinessRuleException($"Código de rol comercial inválido: '{codigo}'. Valores: N, C, P, CP.")
            };
        }

        /// <summary>
        /// Fábrica desde booleanos.
        /// </summary>
        public static RolComercial DesdeBools(bool esCliente, bool esProveedor)
        {
            var m = (esCliente ? BIT_CLIENTE : 0) | (esProveedor ? BIT_PROVEEDOR : 0);
            return DesdeMascara(m);
        }

        /// <summary>
        /// Agrega el rol 'Cliente' (idempotente).
        /// </summary>
        public RolComercial AgregarCliente() => DesdeMascara(Mascara | BIT_CLIENTE);

        /// <summary>
        /// Agrega el rol 'Proveedor' (idempotente).
        /// </summary>
        public RolComercial AgregarProveedor() => DesdeMascara(Mascara | BIT_PROVEEDOR);

        /// <summary>
        /// Quita el rol 'Cliente' (idempotente).
        /// </summary>
        public RolComercial QuitarCliente() => DesdeMascara(Mascara & ~BIT_CLIENTE);

        /// <summary>
        /// Quita el rol 'Proveedor' (idempotente).
        /// </summary>
        public RolComercial QuitarProveedor() => DesdeMascara(Mascara & ~BIT_PROVEEDOR);

        /// <summary>
        /// Guard: asegura que se pueden realizar operaciones de VENTA (requiere rol Cliente).
        /// </summary>
        public void AsegurarPuedeEmitirComprobanteVenta()
        {
            if (!EsCliente)
                throw new BusinessRuleException("El tercero no tiene rol 'Cliente': no puede emitirse comprobante de venta.");
        }

        /// <summary>
        /// Guard: asegura que se pueden registrar COMPRAS (requiere rol Proveedor).
        /// </summary>
        public void AsegurarPuedeRegistrarCompra()
        {
            if (!EsProveedor)
                throw new BusinessRuleException("El tercero no tiene rol 'Proveedor': no puede registrarse una compra.");
        }

        /// <summary>
        /// Guard opcional: al persistir un tercero aseguremos al menos un rol.
        /// </summary>
        public void AsegurarTieneAlMenosUnRol()
        {
            if (EstaSinRol)
                throw new BusinessRuleException("Debe seleccionar al menos un rol: Cliente y/o Proveedor.");
        }

        public override string ToString() => Nombre;

        #region Igualdad por valor
        public bool Equals(RolComercial? other) => other is not null && Mascara == other.Mascara;
        public override bool Equals(object? obj) => obj is RolComercial r && Equals(r);
        public override int GetHashCode() => Mascara.GetHashCode();
        #endregion
    }
}
