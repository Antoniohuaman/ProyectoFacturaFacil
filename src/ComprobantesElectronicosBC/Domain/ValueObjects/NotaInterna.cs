using System;

namespace ComprobantesElectronicosBC.Domain.ValueObjects
{
    /// <summary>
    /// VO "append-only" para apuntes internos del comprobante.
    /// - Inmutable y con igualdad por valor.
    /// - Se usa para historiales: cada nota se agrega; no se edita ni borra.
    /// - Fecha se fija en tiempo real (UTC) al crear la nota.
    /// - Autor (opcional) es una etiqueta corta (p.ej. usuario que registró la nota).
    /// </summary>
    public sealed record NotaInterna
    {
        /// <summary>Contenido de la nota (1..1000). Se recorta; permite saltos de línea.</summary>
        public string Texto { get; }

        /// <summary>Etiqueta corta del autor (0..100). Opcional.</summary>
        public string? Autor { get; }

        /// <summary>Instante de creación en UTC. Se fija al crear; no es editable.</summary>
        public DateTimeOffset CreadaEnUtc { get; }

        private NotaInterna(string texto, string? autor, DateTimeOffset creadaEnUtc)
        {
            Texto = texto;
            Autor = autor;
            CreadaEnUtc = creadaEnUtc;
        }

        /// <summary>
        /// Fábrica principal:
        /// - Valida longitudes (Texto 1..1000, Autor 0..100).
        /// - Normaliza: Trim() a ambos campos; Autor = null si queda vacío.
        /// - Fecha se fija automáticamente con UtcNow salvo que se inyecte (útil para tests).
        /// </summary>
        /// <param name="texto">Contenido de la nota (obligatorio, 1..1000).</param>
        /// <param name="autor">Etiqueta opcional del usuario que registra la nota (0..100).</param>
        /// <param name="ahoraUtc">
        /// (Solo para pruebas) Fecha/hora a usar en lugar de DateTimeOffset.UtcNow. En app real, omitir.
        /// </param>
        public static NotaInterna Create(string texto, string? autor = null, DateTimeOffset? ahoraUtc = null)
        {
            if (texto is null) throw new ArgumentNullException(nameof(texto));
            var t = texto.Trim();
            if (t.Length == 0) throw new ArgumentException("El texto de la nota no puede estar vacío.", nameof(texto));
            if (t.Length > 1000) throw new ArgumentException("El texto de la nota no debe exceder 1000 caracteres.", nameof(texto));

            string? a = string.IsNullOrWhiteSpace(autor) ? null : autor.Trim();
            if (a is not null && a.Length > 100)
                throw new ArgumentException("El autor no debe exceder 100 caracteres.", nameof(autor));

            var ts = ahoraUtc ?? DateTimeOffset.UtcNow;
            return new NotaInterna(t, a, ts);
        }

        public override string ToString()
            => Autor is null ? Texto : $"{Texto} — {Autor}";
    }
}
