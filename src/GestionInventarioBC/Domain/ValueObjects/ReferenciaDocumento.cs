using SharedKernel.Exceptions;

namespace GestionInventarioBC.Domain.ValueObjects
{
	/// <summary>
	/// Referencia a un documento externo (tipo y número), ambos obligatorios (trim).
	/// </summary>
	public sealed record ReferenciaDocumento
	{
		public string Tipo { get; }
		public string Numero { get; }

		private ReferenciaDocumento(string tipo, string numero)
		{
			Tipo = tipo;
			Numero = numero;
		}

		public static ReferenciaDocumento Crear(string? tipo, string? numero)
		{
			if (string.IsNullOrWhiteSpace(tipo))
				throw new BusinessRuleException("El tipo de documento es obligatorio.");
			if (string.IsNullOrWhiteSpace(numero))
				throw new BusinessRuleException("El número de documento es obligatorio.");
			return new ReferenciaDocumento(tipo.Trim(), numero.Trim());
		}

		public override string ToString() => $"{Tipo}:{Numero}";
	}
}

