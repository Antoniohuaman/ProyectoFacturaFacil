using CatalogoArticulosBC.Domain.Aggregates;

namespace CatalogoArticulosBC.Domain.Policies
{
	   /// <summary>
	   /// Policy para validar que el producto tenga multimedia asociada.
	   /// </summary>
	   public class MultimediaProductoPolicy
	   {
		   /// <summary>
		   /// Devuelve true si el producto tiene al menos un elemento multimedia asociado.
		   /// </summary>
		   public bool TieneMultimediaValida(ProductoSimple producto)
		   {
			   return producto.Multimedia != null && producto.Multimedia.Any();
		   }
	   }
}
