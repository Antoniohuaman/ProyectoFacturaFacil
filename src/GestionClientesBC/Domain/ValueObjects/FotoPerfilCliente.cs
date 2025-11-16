// src/GestionClientesBC/Domain/ValueObjects/FotoPerfilCliente.cs
using System;

namespace GestionClientesBC.Domain.ValueObjects
{
    /// <summary>
    /// Metadatos simples de la foto/avatar principal del cliente.
    /// No almacena el binario, solo referencia (nombre/URL).
    /// </summary>
    public sealed class FotoPerfilCliente : IEquatable<FotoPerfilCliente>
    {
        public string? NombreArchivo { get; }
        public string? UrlPublica { get; }

        public bool TieneFoto => NombreArchivo is not null || UrlPublica is not null;

        public static FotoPerfilCliente Vacio { get; } = new FotoPerfilCliente(null, null);

        private FotoPerfilCliente(string? nombreArchivo, string? urlPublica)
        {
            NombreArchivo = Normalize(nombreArchivo, 255);
            UrlPublica = Normalize(urlPublica, 1024);
        }

        private static string? Normalize(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var trimmed = value.Trim();
            return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
        }

        public static FotoPerfilCliente Create(string? nombreArchivo, string? urlPublica)
        {
            if (string.IsNullOrWhiteSpace(nombreArchivo) && string.IsNullOrWhiteSpace(urlPublica))
                return Vacio;

            return new FotoPerfilCliente(nombreArchivo, urlPublica);
        }

        public bool Equals(FotoPerfilCliente? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return NombreArchivo == other.NombreArchivo &&
                   UrlPublica == other.UrlPublica;
        }

        public override bool Equals(object? obj) => Equals(obj as FotoPerfilCliente);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(NombreArchivo);
            hash.Add(UrlPublica);
            return hash.ToHashCode();
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(NombreArchivo))
                return NombreArchivo;

            return UrlPublica ?? string.Empty;
        }
    }
}
