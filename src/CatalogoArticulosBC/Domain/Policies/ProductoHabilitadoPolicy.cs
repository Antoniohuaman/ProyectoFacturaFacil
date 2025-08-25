using CatalogoArticulosBC.Domain.Aggregates;

namespace CatalogoArticulosBC.Domain.Policies
{
	   /// <summary>
	   /// Policy para validar si el producto está habilitado para la venta.
	   /// </summary>
	   public class ProductoHabilitadoPolicy
	   {
		   /// <summary>
		   /// Devuelve true si el producto está habilitado para la venta.
		   /// </summary>
		   public bool EstaHabilitado(ProductoSimple producto)
		   {
			   return producto.Habilitado;
		   }
	   }
}
