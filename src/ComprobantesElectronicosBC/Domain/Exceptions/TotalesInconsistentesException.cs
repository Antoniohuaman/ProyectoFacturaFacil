using System;
using System.Collections.Generic;
using SharedKernel.Exceptions;

namespace ComprobantesElectronicosBC.Domain.Exceptions
{
	/// <summary>
	/// Excepción lanzada cuando los totales del comprobante electrónico son inconsistentes (suma de líneas, impuestos, etc.).
	/// </summary>
	public class TotalesInconsistentesException : DomainException
	{
		public const string DefaultCode = "TOTALES_INCONSISTENTES";

		public TotalesInconsistentesException(
			string message,
			IReadOnlyDictionary<string, object?>? metadata = null)
			: base(DefaultCode, message, metadata) { }

		public TotalesInconsistentesException(
			string code,
			string message,
			IReadOnlyDictionary<string, object?>? metadata = null)
			: base(string.IsNullOrWhiteSpace(code) ? DefaultCode : code, message, metadata) { }
	}
}
