using System;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace ComprobantesElectronicosBC.Domain.ValueObjects
{
	/// <summary>
	/// ValueObject para representar el número de orden de compra externa (opcional).
	/// </summary>
	public sealed class NumeroOrdenCompra : IEquatable<NumeroOrdenCompra>
	{
		private static readonly Regex SerieCorrelativoRegex = new(@"^([a-zA-Z0-9]+)\s*-\s*([a-zA-Z0-9]+)$", RegexOptions.Compiled);

		/// <summary>
		/// Valor canónico del número de orden de compra.
		/// </summary>
		public string Valor { get; }

		[JsonConstructor]
		private NumeroOrdenCompra(string valor)
		{
			Valor = valor;
		}

		/// <summary>
		/// Crea un nuevo ValueObject a partir de un string. Lanza excepción si el valor es inválido.
		/// </summary>
		public static NumeroOrdenCompra Create(string input)
		{
			if (string.IsNullOrWhiteSpace(input))
				throw new ArgumentException("El número de orden de compra no puede ser nulo o vacío.", nameof(input));

			var valorCanonico = Canonicalize(input);
			return new NumeroOrdenCompra(valorCanonico);
		}

		/// <summary>
		/// Crea un ValueObject solo si el input no es nulo ni vacío; si lo es, retorna null.
		/// </summary>
		public static NumeroOrdenCompra? FromOptional(string? input)
		{
			if (string.IsNullOrWhiteSpace(input))
				return null;
			return Create(input);
		}

		private static string Canonicalize(string input)
		{
			var trimmed = input.Trim();
			// Si tiene guion, procesar como serie-correlativo
			var match = SerieCorrelativoRegex.Match(trimmed);
			if (match.Success)
			{
				var serie = match.Groups[1].Value.Trim().ToUpperInvariant();
				var correlativo = match.Groups[2].Value.Trim();
				// Si correlativo es numérico y <= 8 dígitos, pad a 8
				if (correlativo.Length <= 8 && long.TryParse(correlativo, out var num))
				{
					correlativo = num.ToString("D8");
				}
				// Si correlativo es texto o más de 8 dígitos, dejar como está
				return $"{serie}-{correlativo}";
			}
			// Si no tiene guion, solo upper y trim
			return trimmed.ToUpperInvariant();
		}

		public override string ToString() => Valor;

		public override bool Equals(object? obj) => Equals(obj as NumeroOrdenCompra);

		public bool Equals(NumeroOrdenCompra? other)
		{
			if (ReferenceEquals(this, other)) return true;
			if (other is null) return false;
			return Valor == other.Valor;
		}

		public override int GetHashCode() => Valor.GetHashCode(StringComparison.Ordinal);

		public static bool operator ==(NumeroOrdenCompra? left, NumeroOrdenCompra? right)
			=> Equals(left, right);

		public static bool operator !=(NumeroOrdenCompra? left, NumeroOrdenCompra? right)
			=> !Equals(left, right);
	}
}
