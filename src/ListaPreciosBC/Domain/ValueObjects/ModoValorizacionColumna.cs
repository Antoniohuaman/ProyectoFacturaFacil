using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ListaPreciosBC.Domain.ValueObjects
{
    /// <summary>
    /// Modo de valorización de una columna de precios:
    /// - Fijo       : un valor por columna.
    /// - PorVolumen : matriz de tramos por cantidad.
    ///
    /// Es un Value Object "smart-enum": instancias inmutables de conjunto finito,
    /// igualdad por valor y orden natural (Fijo=0, PorVolumen=1).
    /// </summary>
    [DebuggerDisplay("{Nombre} ({Codigo})")]
    public sealed class ModoValorizacionColumna :
        IEquatable<ModoValorizacionColumna>, IComparable<ModoValorizacionColumna>
    {
        private readonly byte _orden; // para CompareTo y hash

        /// <summary>Código corto para persistencia/CSV (F|V).</summary>
        public string Codigo { get; }
        /// <summary>Nombre legible: "Fijo" | "PorVolumen".</summary>
        public string Nombre { get; }

        private ModoValorizacionColumna(byte orden, string codigo, string nombre)
        {
            _orden = orden;
            Codigo = codigo;
            Nombre = nombre;
        }

        // --------- Instancias públicas (únicas) ----------
        public static ModoValorizacionColumna Fijo       { get; } = new(0, "F",  "Fijo");
        public static ModoValorizacionColumna PorVolumen { get; } = new(1, "V",  "PorVolumen");

        /// <summary>Conjunto de todos los modos, en orden natural.</summary>
        public static IReadOnlyList<ModoValorizacionColumna> Todos { get; } =
            new[] { Fijo, PorVolumen };

        // --------- Fábricas / Parse ----------
        /// <summary>
        /// Crea desde texto. Acepta:
        /// - Nombres: "Fijo", "PorVolumen", "Por Volumen", "POR_VOLUMEN", "por-volumen"
        /// - Códigos: "F", "V", "PV"
        /// </summary>
        public static ModoValorizacionColumna Crear(string texto)
        {
            if (texto is null) throw new ArgumentNullException(nameof(texto));
            var key = Normalizar(texto);
            if (key is "f" or "fijo")
                return Fijo;
            if (key is "v" or "pv" or "porvolumen")
                return PorVolumen;

            throw new ArgumentOutOfRangeException(nameof(texto),
                "Modo inválido. Use 'Fijo' o 'PorVolumen' (códigos: F | V | PV).");
        }

        public static bool TryCrear(string? texto, out ModoValorizacionColumna? modo)
        {
            modo = null;
            if (texto is null) return false;
            var key = Normalizar(texto);
            if (key is "f" or "fijo") { modo = Fijo; return true; }
            if (key is "v" or "pv" or "porvolumen") { modo = PorVolumen; return true; }
            return false;
        }

        /// <summary>Crea directamente desde código ('F'|'V'|'PV').</summary>
        public static ModoValorizacionColumna DesdeCodigo(string codigo) => Crear(codigo);

        public static bool TryDesdeCodigo(string? codigo, out ModoValorizacionColumna? modo)
            => TryCrear(codigo, out modo);

        // --------- Helpers ---------
        public bool EsFijo => this == Fijo;
        public bool EsPorVolumen => this == PorVolumen;

        private static string Normalizar(string s)
        {
            // lower + quitar espacios, guiones y underscores
            Span<char> buf = stackalloc char[s.Length];
            int len = 0;
            foreach (var ch in s)
            {
                if (char.IsWhiteSpace(ch) || ch == '-' || ch == '_') continue;
                buf[len++] = char.ToLowerInvariant(ch);
            }
            return new string(buf.Slice(0, len));
        }

        // --------- Igualdad / Orden ----------
        public bool Equals(ModoValorizacionColumna? other)
            => other is not null && _orden == other._orden; // conjunto finito

        public override bool Equals(object? obj) => Equals(obj as ModoValorizacionColumna);

        public override int GetHashCode() => _orden.GetHashCode();

        public int CompareTo(ModoValorizacionColumna? other)
            => other is null ? 1 : _orden.CompareTo(other._orden);

        public static bool operator ==(ModoValorizacionColumna? a, ModoValorizacionColumna? b)
            => a is null ? b is null : a.Equals(b);

        public static bool operator !=(ModoValorizacionColumna? a, ModoValorizacionColumna? b)
            => !(a == b);

        public override string ToString() => Nombre;
    }
}