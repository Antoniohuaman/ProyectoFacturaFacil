using SharedKernel.Exceptions;

namespace GestionInventarioBC.Domain.Exceptions
{
	public sealed class ExcepcionReservaNoEncontrada : BusinessRuleException
	{
		public ExcepcionReservaNoEncontrada(string? detalle = null)
			: base("RESERVA_NO_ENCONTRADA", detalle ?? "La reserva solicitada no existe.") { }
	}
}

