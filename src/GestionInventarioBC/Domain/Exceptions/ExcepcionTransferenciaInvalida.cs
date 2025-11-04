using SharedKernel.Exceptions;

namespace GestionInventarioBC.Domain.Exceptions
{
	public sealed class ExcepcionTransferenciaInvalida : BusinessRuleException
	{
		public ExcepcionTransferenciaInvalida(string mensaje)
			: base("TRANSFERENCIA_INVALIDA", mensaje) { }
	}
}

