// src/GestionClientesBC/Domain/ValueObjects/ObservacionesCliente.cs
using System;

namespace GestionClientesBC.Domain.ValueObjects
{
    /// <summary>
    /// Notas libres asociadas al cliente (información interna).
    /// </summary>
    public sealed class ObservacionesCliente : IEquatable<ObservacionesCliente>
    {
        public string Valor { get; }

        private ObservacionesCliente(string valor)
        {
            Valor = valor;
        }

        public static ObservacionesCliente? Create(string? observaciones)
        {
            if (string.IsNullOrWhiteSpace(observaciones))
                return null;

            var trimmed = observaciones.Trim();
            if (trimmed.Length > 1000)
                trimmed = trimmed[..1000];

            return new ObservacionesCliente(trimmed);
        }

        public bool Equals(ObservacionesCliente? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return string.Equals(Valor, other.Valor, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj) => Equals(obj as ObservacionesCliente);

        public override int GetHashCode() => Valor.GetHashCode(StringComparison.Ordinal);

        public override string ToString() => Valor;
    }
}
