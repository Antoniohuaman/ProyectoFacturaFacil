using System;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace ConfiguracionSistemaBC.Domain.ValueObjects
{
    /// <summary>
    /// Value Object para el <b>Pie de página</b> (texto/HTML corto que se imprime en los comprobantes).
    ///
    /// Características:
    /// - Opcional (puede ser vacío).
    /// - Longitud prudente: máx. 2000 caracteres (HTML).
    /// - Saneado básico de HTML para evitar contenido peligroso:
    ///   * Elimina etiquetas peligrosas (<script>, <iframe>, <object>, <embed>, <link>, <meta>, <style>, etc.)
    ///   * Elimina atributos que inician con "on..." (onclick, onload, …).
    ///   * Bloquea href/src con "javascript:" o "data:".
    ///   * Quita cualquier etiqueta que no esté en la <see cref="AllowedTags"/>.
    /// - Se conservan los espacios/caso que ingresa el usuario (no remaqueta el texto).
    /// - La lógica de “mostrar si no es vacío” vive fuera; aquí solo se modela el valor.
    /// </summary>
    [DebuggerDisplay("HtmlLen={Html.Length}, EsVacio={EsVacio}")]
    public sealed class PieDePagina
    {
        /// <summary>Longitud máxima permitida del HTML.</summary>
        public const int MaxLongitudHtml = 2000;

        /// <summary>Conjunto de etiquetas HTML permitidas en el pie (minimiza riesgos).</summary>
        public static readonly string[] AllowedTags =
        {
            "p","br","strong","b","em","i","u","small","span",
            "ul","ol","li",
            "table","thead","tbody","tr","th","td",
            "a" // solo con href seguro
        };

        /// <summary>Instancia vacía (sin contenido).</summary>
        public static readonly PieDePagina Vacio = new(string.Empty);

        /// <summary>Contenido HTML saneado (puede ser cadena vacía).</summary>
        public string Html { get; }

        /// <summary>Indica si no hay contenido (cadena vacía).</summary>
        public bool EsVacio => Html.Length == 0;

        private PieDePagina(string htmlSaneado)
        {
            Html = htmlSaneado;
        }

        // --------------------------- Fábricas ---------------------------

        /// <summary>
        /// Crea desde HTML del editor. Si es null/solo espacios, devuelve <see cref="Vacio"/>.
        /// Valida longitud y sanea el contenido.
        /// </summary>
        public static PieDePagina FromHtml(string? html)
        {
            if (string.IsNullOrWhiteSpace(html)) return Vacio;

            if (html.Length > MaxLongitudHtml)
                throw new ArgumentOutOfRangeException(nameof(html),
                    $"El pie de página excede el máximo permitido de {MaxLongitudHtml} caracteres.");

            var limpio = SanitizarHtml(html);
            return limpio.Length == 0 ? Vacio : new PieDePagina(limpio);
        }

        /// <summary>
        /// Crea desde texto plano (convierte saltos de línea a &lt;br&gt; y escapa HTML).
        /// </summary>
        public static PieDePagina FromTextoPlano(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return Vacio;

            var encoded = HtmlEncode(texto);
            // Convertir \r\n / \n a <br>
            encoded = encoded.Replace("\r\n", "<br>").Replace("\n", "<br>");
            return FromHtml(encoded);
        }

        /// <summary>
        /// Intenta crear desde HTML, devolviendo false si falla la validación.
        /// </summary>
        public static bool TryFromHtml(string? html, out PieDePagina? pie)
        {
            pie = null;
            if (string.IsNullOrWhiteSpace(html)) { pie = Vacio; return true; }
            if (html.Length > MaxLongitudHtml) return false;

            var limpio = SanitizarHtml(html);
            pie = limpio.Length == 0 ? Vacio : new PieDePagina(limpio);
            return true;
        }

        /// <summary>
        /// Retorna una nueva instancia con el HTML actualizado (misma validación/saneado).
        /// </summary>
        public PieDePagina Actualizar(string? htmlNuevo) => FromHtml(htmlNuevo);

        /// <summary>
        /// Obtiene una vista previa en texto plano (sin etiquetas), truncada a la longitud indicada.
        /// </summary>
        public string TextoPlanoPreview(int maxCaracteres = 160)
        {
            if (EsVacio) return string.Empty;
            var plain = StripHtml(Html);
            if (plain.Length <= maxCaracteres) return plain;
            return plain.Substring(0, maxCaracteres);
        }

        // --------------------------- Igualdad por valor ---------------------------

        public override bool Equals(object? obj)
            => obj is PieDePagina other && string.Equals(Html, other.Html, StringComparison.Ordinal);

        public override int GetHashCode() => Html.GetHashCode(StringComparison.Ordinal);

        public static bool operator ==(PieDePagina? left, PieDePagina? right)
            => left is null ? right is null : left.Equals(right);

        public static bool operator !=(PieDePagina? left, PieDePagina? right) => !(left == right);

        public override string ToString() => Html;

        // --------------------------- Saneado/Helpers ---------------------------

        private static string SanitizarHtml(string html)
        {
            // 1) Quitar comentarios
            string s = Regex.Replace(html, @"<!--.*?-->", string.Empty, RegexOptions.Singleline);

            // 2) Eliminar etiquetas y contenido peligrosos
            string[] peligrosas = { "script","iframe","object","embed","link","meta","style","base","form","input","button" };
            foreach (var tag in peligrosas)
            {
                // <tag ...>...</tag>
                s = Regex.Replace(s, $@"<\s*{tag}\b.*?>.*?<\s*/\s*{tag}\s*>", string.Empty,
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);
                // <tag .../>
                s = Regex.Replace(s, $@"<\s*{tag}\b.*?/>", string.Empty,
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);
            }

            // 3) Eliminar atributos on* (onclick, onload, ...)
            s = Regex.Replace(s, @"\s+on\w+\s*=\s*(""[^""]*""|'[^']*'|[^\s>]+)", string.Empty,
                RegexOptions.IgnoreCase);

            // 4) Neutralizar href/src peligrosos (javascript:, data:)
            s = Regex.Replace(s, @"\s(href|src)\s*=\s*(""|')\s*(javascript:|data:)[^""']*(""|')", string.Empty,
                RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"\s(href|src)\s*=\s*(javascript:|data:)[^\s>]+", string.Empty,
                RegexOptions.IgnoreCase);

            // 5) Eliminar etiquetas que NO están en la whitelist
            var allow = string.Join("|", AllowedTags);
            s = Regex.Replace(s, $@"<(?!\/?(?:{allow})\b)[^>]*>", string.Empty,
                RegexOptions.IgnoreCase);

            // 6) (Opcional) Forzar rel para target=_blank en enlaces
            s = Regex.Replace(s,
                @"<\s*a\b([^>]*?)>",
                m =>
                {
                    var attrs = m.Groups[1].Value;
                    // Si tiene target=_blank y no tiene rel, agregamos uno seguro
                    bool targetBlank = Regex.IsMatch(attrs, @"\btarget\s*=\s*(""|')?_blank\1?", RegexOptions.IgnoreCase);
                    bool tieneRel = Regex.IsMatch(attrs, @"\brel\s*=", RegexOptions.IgnoreCase);
                    if (targetBlank && !tieneRel)
                        return $"<a{attrs} rel=\"noopener noreferrer\">";
                    return $"<a{attrs}>";
                },
                RegexOptions.IgnoreCase);

            // 7) Trim final
            s = s.Trim();

            return s;
        }

        private static string HtmlEncode(string text)
        {
            var sb = new StringBuilder(text.Length + 16);
            foreach (var c in text)
            {
                sb.Append(c switch
                {
                    '&' => "&amp;",
                    '<' => "&lt;",
                    '>' => "&gt;",
                    '"' => "&quot;",
                    '\'' => "&#39;",
                    _ => c.ToString()
                });
            }
            return sb.ToString();
        }

        private static string StripHtml(string html)
        {
            if (string.IsNullOrEmpty(html)) return string.Empty;
            // Quitar etiquetas
            var s = Regex.Replace(html, "<.*?>", string.Empty, RegexOptions.Singleline);
            // Decodificación mínima de entidades (lo común)
            s = s.Replace("&nbsp;", " ")
                 .Replace("&amp;", "&")
                 .Replace("&lt;", "<")
                 .Replace("&gt;", ">")
                 .Replace("&quot;", "\"")
                 .Replace("&#39;", "'");
            return s.Trim();
        }
    }
}