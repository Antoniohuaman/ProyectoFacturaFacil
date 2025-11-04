using SharedKernel.Exceptions;

namespace GestionInventarioBC.Domain.Exceptions
{
	public sealed class ExcepcionStockInsuficiente : BusinessRuleException
	{
		public ExcepcionStockInsuficiente(string? detalle = null)
			: base("STOCK_INSUFICIENTE", detalle ?? "No existe stock disponible suficiente para la operación.") { }
	}
}

