// src/GestionClientesBC/Domain/ValueObjects/PaginaWebCliente.cs
using System;

namespace GestionClientesBC.Domain.ValueObjects
{
    /// <summary>
    /// Sitio web principal del cliente.
    /// No valida formato de URL de manera estricta para permitir variantes como "miempresa.pe".
    /// </summary>
    public sealed class PaginaWebCliente : IEquatable<PaginaWebCliente>
    {
        public string Valor { get; }

        private PaginaWebCliente(string valor)
        {
            Valor = valor;
        }

        public static PaginaWebCliente? Create(string? paginaWeb)
        {
            if (string.IsNullOrWhiteSpace(paginaWeb))
                return null;

            var trimmed = paginaWeb.Trim();

            // Límite suave para evitar textos enormes
            if (trimmed.Length > 200)
                trimmed = trimmed[..200];

            return new PaginaWebCliente(trimmed);
        }

        public bool Equals(PaginaWebCliente? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return string.Equals(Valor, other.Valor, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj) => Equals(obj as PaginaWebCliente);

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Valor);

        public override string ToString() => Valor;
    }
}
