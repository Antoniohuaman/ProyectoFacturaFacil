using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Domain.ValueObjects
{
	/// <summary>
	/// Costo unitario como wrapper semántico de Dinero.
	/// </summary>
	public sealed record CostoUnitario
	{
		public Dinero Valor { get; }

		private CostoUnitario(Dinero valor)
		{
			Valor = valor;
		}

		public static CostoUnitario DesdeDinero(Dinero dinero) => new(dinero);

		public override string ToString() => Valor.ToString();
	}
}

