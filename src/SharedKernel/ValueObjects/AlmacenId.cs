using System;
using SharedKernel.Exceptions;

namespace SharedKernel.ValueObjects
{
	/// <summary>
	/// Identidad opaca de Almacén.
	/// - Wrapper sobre Guid (no Empty).
	/// - No confundir con códigos visibles; este ID es técnico e inmutable.
	/// </summary>
	public sealed record AlmacenId
	{
		/// <summary>Guid canónico del identificador.</summary>
		public Guid Value { get; }

		private AlmacenId(Guid value) => Value = value;

		/// <summary>Crea un AlmacenId desde un Guid (no Empty).</summary>
		public static AlmacenId From(Guid guid)
		{
			if (guid == Guid.Empty)
				throw new BusinessRuleException("AlmacenId no puede ser Guid.Empty.");
			return new AlmacenId(guid);
		}

		/// <summary>Genera un nuevo AlmacenId.</summary>
		public static AlmacenId New() => new AlmacenId(Guid.NewGuid());

		/// <summary>Crea desde cadena (Guid válido).</summary>
		public static AlmacenId FromString(string? input)
		{
			if (string.IsNullOrWhiteSpace(input))
				throw new BusinessRuleException("AlmacenId es obligatorio.");
			if (!Guid.TryParse(input.Trim(), out var g) || g == Guid.Empty)
				throw new BusinessRuleException("AlmacenId inválido.");
			return new AlmacenId(g);
		}

		/// <summary>Intenta parsear sin lanzar excepción.</summary>
		public static bool TryParse(string? input, out AlmacenId? id)
		{
			id = null;
			if (string.IsNullOrWhiteSpace(input)) return false;
			if (!Guid.TryParse(input.Trim(), out var g) || g == Guid.Empty) return false;
			id = new AlmacenId(g);
			return true;
		}

		/// <summary>Indica si el Guid interno es vacío.</summary>
		public bool IsEmpty => Value == Guid.Empty;

		public override string ToString() => Value.ToString("D");

		// Conversiones ergonómicas
		public static explicit operator AlmacenId(Guid g) => From(g);
		public static implicit operator Guid(AlmacenId id) => id.Value;

		/// <summary>Comparación explícita por identidad.</summary>
		public bool EsMismoQue(AlmacenId otra) => otra is not null && Value == otra.Value;
	}
}

