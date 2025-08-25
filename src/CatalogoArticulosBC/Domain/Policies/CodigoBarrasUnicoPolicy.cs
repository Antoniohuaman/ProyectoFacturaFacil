using CatalogoArticulosBC.Domain.Aggregates;

namespace CatalogoArticulosBC.Domain.Policies
{
	   /// <summary>
	   /// Policy para validar unicidad de código de barras en productos.
	   /// </summary>
	   public class CodigoBarrasUnicoPolicy
	   {
		   /// <summary>
		   /// Devuelve true si el código de barras del producto no existe en la lista dada.
		   /// </summary>
		   public bool EsCodigoUnico(ProductoSimple producto, IEnumerable<ProductoSimple> productosExistentes)
		   {
			   return !productosExistentes.Any(p => p.CodigoBarras == producto.CodigoBarras);
		   }
	   }
}
