using System;
using System.Collections.Generic;
using SharedKernel.Exceptions;

namespace ComprobantesElectronicosBC.Domain.Exceptions
{
	/// <summary>
	/// Excepción lanzada cuando una fecha es inválida o no cumple con las reglas del comprobante electrónico.
	/// </summary>
	public class FechaInvalidaException : DomainException
	{
		public const string DefaultCode = "FECHA_INVALIDA";

		public FechaInvalidaException(
			string message,
			IReadOnlyDictionary<string, object?>? metadata = null)
			: base(DefaultCode, message, metadata) { }

		public FechaInvalidaException(
			string code,
			string message,
			IReadOnlyDictionary<string, object?>? metadata = null)
			: base(string.IsNullOrWhiteSpace(code) ? DefaultCode : code, message, metadata) { }
	}
}
