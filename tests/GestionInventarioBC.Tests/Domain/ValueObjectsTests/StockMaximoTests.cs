using NUnit.Framework;
using GestionInventarioBC.Domain.ValueObjects;
using SharedKernel.Exceptions;

namespace GestionInventarioBC.Tests.ValueObjects
{
	[TestFixture]
	public class StockMaximoTests
	{
		[Test]
		public void NoPermiteNegativos_y_Formato()
		{
			Assert.That(() => new StockMaximo(-1m), Throws.TypeOf<BusinessRuleException>());
			var s = new StockMaximo(10.1234567m);
			Assert.That(s.Value, Is.EqualTo(10.123457m));
			Assert.That(s.ToString(), Is.EqualTo("10.123457"));
		}
	}
}

