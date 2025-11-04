using NUnit.Framework;
using GestionInventarioBC.Domain.ValueObjects;
using SharedKernel.Exceptions;

namespace GestionInventarioBC.Tests.ValueObjects
{
	[TestFixture]
	public class StockMinimoTests
	{
		[Test]
		public void NoPermiteNegativos_y_Formato()
		{
			Assert.That(() => new StockMinimo(-1m), Throws.TypeOf<BusinessRuleException>());
			var s = new StockMinimo(5.1234567m);
			Assert.That(s.Value, Is.EqualTo(5.123457m));
			Assert.That(s.ToString(), Is.EqualTo("5.123457"));
		}
	}
}

