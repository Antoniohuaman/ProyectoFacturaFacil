using SharedKernel.ValueObjects; // Dinero

namespace GestionInventarioBC.Domain.Policies
{
	/// <summary>
	/// Política de valoración de inventario (promedio ponderado simple).
	/// </summary>
	public static class PoliticaValoracionInventario
	{
		/// <summary>
		/// Calcula el costo promedio ponderado resultante tras una entrada.
		/// </summary>
		public static Dinero CostoPromedio(Dinero costoActual, decimal cantidadActual, Dinero costoEntrada, decimal cantidadEntrada)
		{
			if (cantidadActual < 0 || cantidadEntrada < 0)
				throw new System.ArgumentOutOfRangeException("Las cantidades no pueden ser negativas.");
			var totalActual = costoActual * cantidadActual;
			var totalEntrada = costoEntrada * cantidadEntrada;
			var totalCantidad = cantidadActual + cantidadEntrada;
			if (totalCantidad == 0m) return costoEntrada; // si no había stock previo
			return (totalActual + totalEntrada) / totalCantidad;
		}
	}
}

