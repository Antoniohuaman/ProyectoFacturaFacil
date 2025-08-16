using System;
using System.Diagnostics;

namespace ConfiguracionSistemaBC.Domain.ValueObjects
{
    /// <summary>
    /// Value Object para dirección de correo electrónico corporativo.
    /// - Opcional (puede no existir ningún correo o varios).
    /// - Incluye flag <see cref="EsVisible"/> para controlar su exposición (p.ej. en PDFs).
    /// - Inmutable y con igualdad por valor.
    ///
    /// Validación pragmática (sin RFC completo):
    /// - Formato: local@dominio
    /// - Sin espacios, longitud total ≤ 254, local ≤ 64
    /// - Local: letras, dígitos y símbolos permitidos .!#$%&'*+/=?^_`{|}~ (punto no al inicio/fin ni consecutivo)
    /// - Dominio: al menos dos labels separados por ".", labels 1–63 chars [A-Za-z0-9-] sin guiones al inicio/fin
    /// - TLD (último label): solo letras, longitud 2–24
    /// </summary>
    [DebuggerDisplay("{Direccion} (Visible={EsVisible})")]
    public sealed class EmailEmpresa
    {
        /// <summary>Dirección canónica (minúsculas, sin espacios).</summary>
        public string Direccion { get; }

        /// <summary>Dominio (parte derecha de la dirección).</summary>
        public string Dominio { get; }

        /// <summary>Marca si este correo puede mostrarse en documentos/salidas públicas.</summary>
        public bool EsVisible { get; }

        private EmailEmpresa(string direccionCanonica, string dominioCanonico, bool esVisible)
        {
            Direccion = direccionCanonica;
            Dominio   = dominioCanonico;
            EsVisible = esVisible;
        }

        // ------------------------ Fábricas ------------------------

        /// <summary>
        /// Crea un <see cref="EmailEmpresa"/> validando el formato. Normaliza a minúsculas.
        /// </summary>
        /// <param name="direccion">Texto ingresado por el usuario.</param>
        /// <param name="esVisible">Flag de visibilidad (default: true).</param>
        public static EmailEmpresa From(string direccion, bool esVisible = true)
        {
            if (direccion is null) throw new ArgumentNullException(nameof(direccion));

            var s = direccion.Trim().ToLowerInvariant();
            if (!EsCorreoValido(s, out var dominio))
                throw new ArgumentOutOfRangeException(nameof(direccion), "Correo electrónico inválido.");

            return new EmailEmpresa(s, dominio!, esVisible);
        }

        /// <summary>
        /// Intenta crear un <see cref="EmailEmpresa"/>; devuelve false si la dirección no es válida.
        /// </summary>
        public static bool TryFrom(string? direccion, out EmailEmpresa? email, bool esVisible = true)
        {
            email = null;
            if (string.IsNullOrWhiteSpace(direccion)) return false;

            var s = direccion.Trim().ToLowerInvariant();
            if (!EsCorreoValido(s, out var dominio)) return false;

            email = new EmailEmpresa(s, dominio!, esVisible);
            return true;
        }

        /// <summary>
        /// Devuelve una nueva instancia con la misma dirección pero distinta visibilidad.
        /// </summary>
        public EmailEmpresa ConVisibilidad(bool esVisible) => new(Direccion, Dominio, esVisible);

        // ------------------------ Igualdad por valor ------------------------

        public override bool Equals(object? obj)
        {
            if (obj is not EmailEmpresa other) return false;
            // La identidad del VO incluye dirección y política de visibilidad
            return string.Equals(Direccion, other.Direccion, StringComparison.Ordinal)
                && EsVisible == other.EsVisible;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + Direccion.GetHashCode(StringComparison.Ordinal);
                hash = hash * 31 + EsVisible.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(EmailEmpresa? left, EmailEmpresa? right)
            => left is null ? right is null : left.Equals(right);

        public static bool operator !=(EmailEmpresa? left, EmailEmpresa? right) => !(left == right);

        public override string ToString() => Direccion;

        /// <summary>Conversión implícita a string (retorna la dirección).</summary>
        public static implicit operator string(EmailEmpresa value) => value.Direccion;

        /// <summary>Conversión explícita desde string (valida y normaliza). Visibilidad por defecto: true.</summary>
        public static explicit operator EmailEmpresa(string value) => From(value);

        // ------------------------ Validación interna ------------------------

        /// <summary>
        /// Valida la dirección y extrae el dominio canónico (minúsculas). No soporta local-part entre comillas.
        /// </summary>
        private static bool EsCorreoValido(string s, out string? dominio)
        {
            dominio = null;

            // Longitud y espacios
            if (s.Length == 0 || s.Length > 254) return false;
            if (s.Contains(' ')) return false;

            int at = s.IndexOf('@');
            if (at <= 0 || at >= s.Length - 1) return false;

            var local = s.AsSpan(0, at);
            var dom   = s.AsSpan(at + 1);

            if (local.Length == 0 || local.Length > 64) return false;
            if (!ValidaLocal(local)) return false;
            if (!ValidaDominio(dom)) return false;

            dominio = dom.ToString();
            return true;
        }

        private static bool ValidaLocal(ReadOnlySpan<char> local)
        {
            // No puede empezar/terminar con '.'
            if (local[0] == '.' || local[^1] == '.') return false;

            bool prevDot = false;
            for (int i = 0; i < local.Length; i++)
            {
                char c = local[i];
                if (c == '.')
                {
                    if (prevDot) return false; // ".." no permitido
                    prevDot = true;
                    continue;
                }
                prevDot = false;

                if (!(EsLetra(c) || EsDigito(c) || EsSimboloLocalPermitido(c)))
                    return false;
            }
            return true;
        }

        private static bool ValidaDominio(ReadOnlySpan<char> dom)
        {
            // Debe tener al menos un punto (dos labels mínimo)
            int lastDot = dom.LastIndexOf('.');
            if (lastDot <= 0 || lastDot >= dom.Length - 1) return false;

            // Validar labels
            int start = 0;
            while (start < dom.Length)
            {
                int dot = dom.Slice(start).IndexOf('.');
                int len = dot < 0 ? dom.Length - start : dot;
                if (len < 1 || len > 63) return false;

                var label = dom.Slice(start, len);
                // No guion al inicio/fin, solo alfanumérico/guion
                if (label[0] == '-' || label[^1] == '-') return false;
                for (int i = 0; i < label.Length; i++)
                {
                    char c = label[i];
                    if (!(EsLetra(c) || EsDigito(c) || c == '-')) return false;
                }

                if (dot < 0) break;
                start += len + 1; // avanzar tras el punto
            }

            // TLD: solo letras, 2–24
            var tld = dom.Slice(lastDot + 1);
            if (tld.Length < 2 || tld.Length > 24) return false;
            for (int i = 0; i < tld.Length; i++)
            {
                if (!EsLetra(tld[i])) return false;
            }

            return true;
        }

        private static bool EsLetra(char c)
            => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');

        private static bool EsDigito(char c)
            => c >= '0' && c <= '9';

        private static bool EsSimboloLocalPermitido(char c)
            => c is '!' or '#' or '$' or '%' or '&' or '\'' or '*' or '+' or '-' or '/'
                 or '=' or '?' or '^' or '_' or '`' or '{' or '|' or '}' or '~';
    }
}