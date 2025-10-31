using System;
using System.Text.RegularExpressions;
using SharedKernel.ValueObjects; // EmpresaId, CategoriaId

namespace ConfiguracionSistemaBC.Domain.Entities
{
    // Si ya tienes este enum en otro archivo, elimina esta definición.
    public enum EstadoCategoria
    {
        Habilitado = 1,
        Deshabilitado = 2
    }

    /// <summary>
    /// Entity (Aggregate Root) de Categoría. Identidad: <see cref="CategoriaId"/>.
    /// </summary>
    public sealed class Categoria
    {
        private const int NombreMaxLength = 100;
        private static readonly Regex ColorHexRegex =
            new(@"^#([0-9A-Fa-f]{6})$", RegexOptions.Compiled);

        // --- Requerido por EF/serialización; inicializa propiedades non-null para evitar CS8618 ---
        private Categoria() { }
        public CategoriaId Id { get; private set; } = default!;
        public EmpresaId EmpresaId { get; private set; } = default!;
        /// <summary>Nombre normalizado (Trim + mayúsculas).</summary>
        public string Nombre { get; private set; } = null!;
        public string? Descripcion { get; private set; }
        /// <summary>Color en formato HEX (#RRGGBB), opcional.</summary>
        public string? ColorHex { get; private set; }
        public EstadoCategoria Estado { get; private set; }
        public DateTime FechaRegistroUtc { get; private set; }
        public DateTime? FechaUltimaModificacionUtc { get; private set; }

        private Categoria(CategoriaId id, EmpresaId empresaId, string nombreNormalizado, string? descripcion, string? colorHex)
        {
            Id = id;
            EmpresaId = empresaId;
            Nombre = nombreNormalizado;
            Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim();
            ColorHex = colorHex;
            Estado = EstadoCategoria.Habilitado;
            FechaRegistroUtc = DateTime.UtcNow;
            FechaUltimaModificacionUtc = null;
        }

        // ----------------- Fábrica -----------------
        public static Categoria Crear(EmpresaId empresaId, string nombre, string? descripcion = null, string? colorHex = null)
        {
            var nombreNorm = NormalizarNombre(nombre);
            ValidarColorHexOpcional(colorHex);
            return new Categoria(CategoriaId.New(), empresaId, nombreNorm, descripcion, colorHex);
        }

        // ----------------- Comportamientos -----------------
        public void Renombrar(string nuevoNombre)
        {
            var nuevoNorm = NormalizarNombre(nuevoNombre);
            if (nuevoNorm == Nombre)
                throw new InvalidOperationException("El nuevo nombre es igual al actual.");
            Nombre = nuevoNorm;
            ToqueModificacion();
        }

        public void CambiarDescripcion(string? nuevaDescripcion)
        {
            var nueva = string.IsNullOrWhiteSpace(nuevaDescripcion) ? null : nuevaDescripcion.Trim();
            if (nueva == Descripcion)
                throw new InvalidOperationException("La nueva descripción es igual a la actual.");
            Descripcion = nueva;
            ToqueModificacion();
        }

        public void CambiarColor(string? nuevoColorHex)
        {
            ValidarColorHexOpcional(nuevoColorHex);
            if (string.Equals(nuevoColorHex, ColorHex, StringComparison.Ordinal))
                throw new InvalidOperationException("El nuevo color es igual al actual.");
            ColorHex = nuevoColorHex;
            ToqueModificacion();
        }

        public void Habilitar()
        {
            if (Estado == EstadoCategoria.Habilitado)
                throw new InvalidOperationException("La categoría ya está habilitada.");
            Estado = EstadoCategoria.Habilitado;
            ToqueModificacion();
        }

        public void Deshabilitar()
        {
            if (Estado == EstadoCategoria.Deshabilitado)
                throw new InvalidOperationException("La categoría ya está deshabilitada.");
            Estado = EstadoCategoria.Deshabilitado;
            ToqueModificacion();
        }

        // ----------------- Helpers -----------------
        private static string NormalizarNombre(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                throw new ArgumentException("El nombre de la categoría es obligatorio.", nameof(valor));

            var trimmed = valor.Trim();
            if (trimmed.Length > NombreMaxLength)
                throw new ArgumentException($"El nombre de la categoría no puede exceder {NombreMaxLength} caracteres.", nameof(valor));

            return trimmed.ToUpperInvariant();
        }

        private static void ValidarColorHexOpcional(string? colorHex)
        {
            if (colorHex is null) return;
            if (!ColorHexRegex.IsMatch(colorHex))
                throw new ArgumentException("El color debe tener el formato HEX #RRGGBB.", nameof(colorHex));
        }

        private void ToqueModificacion() => FechaUltimaModificacionUtc = DateTime.UtcNow;
    }
}
