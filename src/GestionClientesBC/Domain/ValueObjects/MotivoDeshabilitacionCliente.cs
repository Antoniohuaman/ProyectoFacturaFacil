// src/GestionClientesBC/Domain/ValueObjects/MotivoDeshabilitacionCliente.cs
using System;

namespace GestionClientesBC.Domain.ValueObjects
{
    /// <summary>
    /// Motivo por el que se deshabilitó un cliente.
    /// Opcional, texto libre con longitud acotada.
    /// </summary>
    public sealed class MotivoDeshabilitacionCliente : IEquatable<MotivoDeshabilitacionCliente>
    {
        public string Valor { get; }

        private MotivoDeshabilitacionCliente(string valor)
        {
            Valor = valor;
        }

        public static MotivoDeshabilitacionCliente? Create(string? motivo)
        {
            if (string.IsNullOrWhiteSpace(motivo))
                return null;

            var trimmed = motivo.Trim();
            if (trimmed.Length > 500)
                trimmed = trimmed[..500];

            return new MotivoDeshabilitacionCliente(trimmed);
        }

        public bool Equals(MotivoDeshabilitacionCliente? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return string.Equals(Valor, other.Valor, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj) => Equals(obj as MotivoDeshabilitacionCliente);

        public override int GetHashCode() => Valor.GetHashCode(StringComparison.Ordinal);

        public override string ToString() => Valor;
    }
}
