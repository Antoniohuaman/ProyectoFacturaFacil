
using System;

namespace IndicadoresNegocioBC.Domain.ValueObjects
{
	/// <summary>
	/// Value Object para el medio de notificación (correo, sms, ambos).
	/// Inmutable, validado y con métodos de fábrica.
	/// </summary>
	public sealed class MedioNotificacion : IEquatable<MedioNotificacion>
	{
		public static readonly MedioNotificacion Correo = new MedioNotificacion("CORREO");
		public static readonly MedioNotificacion Sms = new MedioNotificacion("SMS");
		public static readonly MedioNotificacion Ambos = new MedioNotificacion("AMBOS");

		public string Valor { get; }

		private MedioNotificacion(string valor)
		{
			if (string.IsNullOrWhiteSpace(valor))
				throw new ArgumentException("El medio de notificación es obligatorio.", nameof(valor));
			valor = valor.Trim().ToUpperInvariant();
			if (valor != "CORREO" && valor != "SMS" && valor != "AMBOS")
				throw new ArgumentException($"Medio de notificación inválido: {valor}", nameof(valor));
			Valor = valor;
		}

		public static MedioNotificacion From(string valor) => new MedioNotificacion(valor);

		public bool EsCorreo => Valor == "CORREO";
		public bool EsSms => Valor == "SMS";
		public bool EsAmbos => Valor == "AMBOS";

		public override string ToString() => Valor;

		public override bool Equals(object? obj) => Equals(obj as MedioNotificacion);

		public bool Equals(MedioNotificacion? other) => other is not null && Valor == other.Valor;

		public override int GetHashCode() => Valor.GetHashCode(StringComparison.Ordinal);

		public static bool operator ==(MedioNotificacion? left, MedioNotificacion? right) =>
			left is null ? right is null : left.Equals(right);

		public static bool operator !=(MedioNotificacion? left, MedioNotificacion? right) => !(left == right);
	}
}
