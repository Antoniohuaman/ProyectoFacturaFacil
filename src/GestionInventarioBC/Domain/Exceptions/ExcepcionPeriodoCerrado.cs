using SharedKernel.Exceptions;

namespace GestionInventarioBC.Domain.Exceptions
{
	public sealed class ExcepcionPeriodoCerrado : BusinessRuleException
	{
		public ExcepcionPeriodoCerrado(string? detalle = null)
			: base("PERIODO_CERRADO", detalle ?? "El período de inventario está cerrado para la fecha indicada.") { }
	}
}

