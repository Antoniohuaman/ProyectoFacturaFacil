using NUnit.Framework;
using GestionInventarioBC.Domain.ValueObjects;
using SharedKernel.Exceptions;

namespace GestionInventarioBC.Tests.ValueObjects
{
	[TestFixture]
	public class CantidadStockTests
	{
		[Test]
		public void Crear_NoPermiteNegativos()
		{
			Assert.That(() => new CantidadStock(-0.000001m), Throws.TypeOf<BusinessRuleException>());
			Assert.That(new CantidadStock(0m).Value, Is.EqualTo(0m));
		}

		[Test]
		public void Sumar_y_Restar_respetan_no_negatividad()
		{
			var a = new CantidadStock(10m);
			var b = new CantidadStock(3.25m);
			var c = a + b;
			Assert.That(c.Value, Is.EqualTo(13.25m));

			var d = c - b;
			Assert.That(d.Value, Is.EqualTo(10m));

			Assert.That(() => b - c, Throws.TypeOf<BusinessRuleException>());
		}

		[Test]
		public void Redondeo_a_6_decimales()
		{
			var x = new CantidadStock(1.1234567m);
			Assert.That(x.Value, Is.EqualTo(1.123457m));
		}
	}
}

