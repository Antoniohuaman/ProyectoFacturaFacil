using SharedKernel.Exceptions;

namespace GestionInventarioBC.Domain.Exceptions
{
	public sealed class ExcepcionAlmacenInactivo : BusinessRuleException
	{
		public ExcepcionAlmacenInactivo(string? detalle = null)
			: base("ALMACEN_INACTIVO", detalle ?? "El almacén no se encuentra activo/operativo.") { }
	}
}

