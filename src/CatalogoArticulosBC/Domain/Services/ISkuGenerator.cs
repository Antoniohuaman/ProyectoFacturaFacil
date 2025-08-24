using SharedKernel.ValueObjects;

namespace CatalogoArticulosBC.Domain.Services
{
	/// <summary>
	/// Servicio de dominio para generación automática de SKU.
	/// </summary>
	public interface ISkuGenerator
	{
		Sku Generar();
	}
}
