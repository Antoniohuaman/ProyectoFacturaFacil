using System;
using SharedKernel.ValueObjects;

namespace IndicadoresNegocioBC.Domain.ValueObjects
{
	/// <summary>
	/// Value Object para el destinatario de la notificación.
	/// Permite uno o ambos medios: correo y/o teléfono.
	/// </summary>
	public sealed class DestinatarioNotificacion : IEquatable<DestinatarioNotificacion>
	{
		public Email? Email { get; }
		public Telefono? Telefono { get; }

		public DestinatarioNotificacion(Email? email, Telefono? telefono)
		{
			if (email is null && (telefono is null || telefono.EsVacio))
				throw new ArgumentException("Debe especificar al menos un medio de contacto: email o teléfono.");
			Email = email;
			Telefono = (telefono is not null && !telefono.EsVacio) ? telefono : null;
		}

		public bool TieneEmail => Email is not null;
		public bool TieneTelefono => Telefono is not null;

		public override string ToString()
		{
			if (TieneEmail && TieneTelefono)
				return $"{Email} / {Telefono!.UnirParaMostrar()}";
			if (TieneEmail)
				return Email!.ToString();
			if (TieneTelefono)
				return Telefono!.UnirParaMostrar();
			return string.Empty;
		}

		public override bool Equals(object? obj) => Equals(obj as DestinatarioNotificacion);

		public bool Equals(DestinatarioNotificacion? other)
			=> other is not null && Equals(Email, other.Email) && Equals(Telefono, other.Telefono);

		public override int GetHashCode()
			=> HashCode.Combine(Email, Telefono);

		public static bool operator ==(DestinatarioNotificacion? left, DestinatarioNotificacion? right)
			=> left is null ? right is null : left.Equals(right);

		public static bool operator !=(DestinatarioNotificacion? left, DestinatarioNotificacion? right) => !(left == right);
	}
}
