using System;
using System.Collections.Generic;
using SharedKernel.Exceptions;

namespace ComprobantesElectronicosBC.Domain.Exceptions
{
	/// <summary>
	/// Excepción lanzada cuando el documento de identidad es inválido o no cumple con el formato/regla esperada.
	/// </summary>
	public class DocumentoIdentidadInvalidoException : DomainException
	{
		public const string DefaultCode = "DOCUMENTO_IDENTIDAD_INVALIDO";

		public DocumentoIdentidadInvalidoException(
			string message,
			IReadOnlyDictionary<string, object?>? metadata = null)
			: base(DefaultCode, message, metadata) { }

		public DocumentoIdentidadInvalidoException(
			string code,
			string message,
			IReadOnlyDictionary<string, object?>? metadata = null)
			: base(string.IsNullOrWhiteSpace(code) ? DefaultCode : code, message, metadata) { }
	}
}
