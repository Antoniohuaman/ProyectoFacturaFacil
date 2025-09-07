using System;
using SharedKernel.Exceptions; // BusinessRuleException

namespace GestionClientesBC.Domain.ValueObjects
{
    /// <summary>
    /// Tipo de cliente: Cliente, Proveedor o ambos.
    /// Es independiente del "segmento" (mayorista/minorista...).
    /// - Inmutable, comparación por valor (máscara 0..3)
    /// - Fábricas desde código y booleanos
    /// - Guards para operaciones (venta/compra)
    /// </summary>
    public sealed class TipoCliente : IEquatable<TipoCliente>
    {
        // Bits
        private const int BIT_CLIENTE   = 1; // 01
        private const int BIT_PROVEEDOR = 2; // 10

    // Máscara 1, 2, 3
    public int Mascara { get; }

        // Códigos para persistencia/simple IO
        // C: cliente | P: proveedor | CP: cliente-proveedor

        public string Codigo => Mascara switch
        {
            BIT_CLIENTE => "C",
            BIT_PROVEEDOR => "P",
            BIT_CLIENTE | BIT_PROVEEDOR => "CP",
            _ => throw new InvalidOperationException("TipoCliente inválido: solo se permiten Cliente, Proveedor o Cliente/Proveedor.")
        };

        public string Nombre => Mascara switch
        {
            BIT_CLIENTE => "Cliente",
            BIT_PROVEEDOR => "Proveedor",
            BIT_CLIENTE | BIT_PROVEEDOR => "Cliente/Proveedor",
            _ => throw new InvalidOperationException("TipoCliente inválido: solo se permiten Cliente, Proveedor o Cliente/Proveedor.")
        };


    public bool EsCliente   => (Mascara & BIT_CLIENTE) != 0;
    public bool EsProveedor => (Mascara & BIT_PROVEEDOR) != 0;
    public bool EsAmbos     => Mascara == (BIT_CLIENTE | BIT_PROVEEDOR);


    // Instancias estáticas (solo los tres válidos)
    public static readonly TipoCliente SoloCliente      = new TipoCliente(BIT_CLIENTE);
    public static readonly TipoCliente SoloProveedor    = new TipoCliente(BIT_PROVEEDOR);
    public static readonly TipoCliente ClienteProveedor = new TipoCliente(BIT_CLIENTE | BIT_PROVEEDOR);

    // Alias para el valor por defecto (Cliente)
    public static TipoCliente Cliente => SoloCliente;


        // EF Core
        private TipoCliente() { Mascara = BIT_CLIENTE; } // Default a Cliente para EF Core
        private TipoCliente(int mascara)
        {
            if (mascara != BIT_CLIENTE && mascara != BIT_PROVEEDOR && mascara != (BIT_CLIENTE | BIT_PROVEEDOR))
                throw new InvalidOperationException("TipoCliente inválido: solo se permiten Cliente, Proveedor o Cliente/Proveedor.");
            Mascara = mascara;
        }

        private static TipoCliente DesdeMascara(int m) => m switch
        {
            BIT_CLIENTE => SoloCliente,
            BIT_PROVEEDOR => SoloProveedor,
            BIT_CLIENTE | BIT_PROVEEDOR => ClienteProveedor,
            _ => throw new InvalidOperationException("TipoCliente inválido: solo se permiten Cliente, Proveedor o Cliente/Proveedor.")
        };

        /// <summary>
        /// Fábrica desde código: "N", "C", "P", "CP" (case-insensitive, ignora espacios).
        /// </summary>
    public static TipoCliente DesdeCodigo(string? codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                throw new BusinessRuleException("El código de tipo de cliente no puede estar vacío.");

            var c = codigo.Trim().ToUpperInvariant();
            return c switch
            {
                "C"  => SoloCliente,
                "P"  => SoloProveedor,
                "CP" => ClienteProveedor,
                _ => throw new BusinessRuleException($"Código de tipo de cliente inválido: '{codigo}'. Valores: C, P, CP.")
            };
        }

        /// <summary>
        /// Fábrica desde booleanos.
        /// </summary>
    public static TipoCliente DesdeBools(bool esCliente, bool esProveedor)
        {
            var m = (esCliente ? BIT_CLIENTE : 0) | (esProveedor ? BIT_PROVEEDOR : 0);
            if (m == 0)
                throw new BusinessRuleException("Debe seleccionar al menos un tipo: Cliente y/o Proveedor.");
            return DesdeMascara(m);
        }

        /// <summary>
        /// Agrega el rol 'Cliente' (idempotente).
        /// </summary>
    public TipoCliente AgregarCliente() => DesdeMascara(Mascara | BIT_CLIENTE);

        /// <summary>
        /// Agrega el rol 'Proveedor' (idempotente).
        /// </summary>
    public TipoCliente AgregarProveedor() => DesdeMascara(Mascara | BIT_PROVEEDOR);

        /// <summary>
        /// Quita el rol 'Cliente' (idempotente).
        /// </summary>
    public TipoCliente QuitarCliente() => DesdeMascara(Mascara & ~BIT_CLIENTE);

        /// <summary>
        /// Quita el rol 'Proveedor' (idempotente).
        /// </summary>
    public TipoCliente QuitarProveedor() => DesdeMascara(Mascara & ~BIT_PROVEEDOR);

        /// <summary>
        /// Guard: asegura que se pueden realizar operaciones de VENTA (requiere rol Cliente).
        /// </summary>
    public void AsegurarPuedeEmitirComprobanteVenta()
        {
            if (!EsCliente)
                throw new BusinessRuleException("El tercero no tiene tipo 'Cliente': no puede emitirse comprobante de venta.");
        }

        /// <summary>
        /// Guard: asegura que se pueden registrar COMPRAS (requiere rol Proveedor).
        /// </summary>
    public void AsegurarPuedeRegistrarCompra()
        {
            if (!EsProveedor)
                throw new BusinessRuleException("El tercero no tiene tipo 'Proveedor': no puede registrarse una compra.");
        }



    public override string ToString() => Nombre;

        #region Igualdad por valor
    public bool Equals(TipoCliente? other) => other is not null && Mascara == other.Mascara;
    public override bool Equals(object? obj) => obj is TipoCliente r && Equals(r);
        public override int GetHashCode() => Mascara.GetHashCode();
        #endregion
    }
}
