using NUnit.Framework;
using GestionInventarioBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Tests.ValueObjects
{
	[TestFixture]
	public class CostoUnitarioTests
	{
		[Test]
		public void DesdeDinero_ConservaMoneda_y_Formatea()
		{
			var m = Moneda.PEN();
			var dinero = Dinero.Create(12.345m, m);
			var cu = CostoUnitario.DesdeDinero(dinero);

			Assert.That(cu.Valor.Moneda.Codigo, Is.EqualTo("PEN"));
			Assert.That(cu.Valor.Monto, Is.EqualTo(12.35m));
			Assert.That(cu.ToString(), Does.Contain("S/"));
		}
	}
}

