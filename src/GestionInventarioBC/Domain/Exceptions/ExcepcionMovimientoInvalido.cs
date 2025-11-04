using SharedKernel.Exceptions;

namespace GestionInventarioBC.Domain.Exceptions
{
	public sealed class ExcepcionMovimientoInvalido : BusinessRuleException
	{
		public ExcepcionMovimientoInvalido(string mensaje)
			: base("MOVIMIENTO_INVALIDO", mensaje) { }
	}
}

