using System;
using System.Collections.Generic;
using SharedKernel.Exceptions;

namespace ComprobantesElectronicosBC.Domain.Exceptions
{
	/// <summary>
	/// Excepción lanzada cuando faltan campos obligatorios en el comprobante electrónico.
	/// </summary>
	public class CamposObligatoriosFaltantesException : DomainException
	{
		public const string DefaultCode = "CAMPOS_OBLIGATORIOS_FALTANTES";

		public CamposObligatoriosFaltantesException(
			string message,
			IReadOnlyDictionary<string, object?>? metadata = null)
			: base(DefaultCode, message, metadata) { }

		public CamposObligatoriosFaltantesException(
			string code,
			string message,
			IReadOnlyDictionary<string, object?>? metadata = null)
			: base(string.IsNullOrWhiteSpace(code) ? DefaultCode : code, message, metadata) { }
	}
}
