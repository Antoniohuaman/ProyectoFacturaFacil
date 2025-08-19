using System;
using System.Diagnostics;

namespace CatalogoArticulosBC.Domain.ValueObjects
{
	/// <summary>
	/// Value Object para la descripción de un producto o servicio.
	/// - Inmutable, validado y ordenado.
	/// - Permite una descripción opcional, con longitud máxima y normalización.
	/// </summary>
	[DebuggerDisplay("{Texto}")]
	public sealed record Descripcion
	{
		/// <summary>Texto de la descripción (opcional, máximo 500 caracteres).</summary>
		public string Texto { get; }

		private const int MAX_LENGTH = 500;

		private Descripcion(string texto)
		{
			if (texto is null)
				throw new ArgumentNullException(nameof(texto), "La descripción no puede ser nula. Usa cadena vacía si es opcional.");

			var norm = texto.Trim();
			if (norm.Length > MAX_LENGTH)
				throw new ArgumentException($"La descripción no puede exceder {MAX_LENGTH} caracteres.", nameof(texto));

			Texto = norm;
		}

		/// <summary>
		/// Crea una descripción validada. Usa cadena vacía si no se requiere descripción.
		/// </summary>
		public static Descripcion From(string texto) => new(texto ?? string.Empty);

		/// <summary>
		/// Try sin excepciones.
		/// </summary>
		public static bool TryFrom(string? texto, out Descripcion? descripcion)
		{
			try { descripcion = new Descripcion(texto ?? string.Empty); return true; }
			catch { descripcion = null; return false; }
		}

		public override string ToString() => Texto;
	}
}
