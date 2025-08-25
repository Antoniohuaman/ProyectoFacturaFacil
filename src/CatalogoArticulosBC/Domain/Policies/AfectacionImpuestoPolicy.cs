using CatalogoArticulosBC.Domain.Aggregates;

namespace CatalogoArticulosBC.Domain.Policies
{
	   /// <summary>
	   /// Policy para validar si un producto está afectado por impuesto según su categoría.
	   /// </summary>
	   public class AfectacionImpuestoPolicy
	   {
		   /// <summary>
		   /// Devuelve true si la categoría del producto es "Gravado".
		   /// </summary>
		   public bool EsAfectadoPorImpuesto(ProductoSimple producto)
		   {
			   return producto.Categoria.Nombre == "GRAVADO";
		   }
	   }
}
