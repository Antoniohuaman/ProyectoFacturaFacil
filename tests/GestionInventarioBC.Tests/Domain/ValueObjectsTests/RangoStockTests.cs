using NUnit.Framework;
using GestionInventarioBC.Domain.ValueObjects;
using SharedKernel.Exceptions;

namespace GestionInventarioBC.Tests.ValueObjects
{
	[TestFixture]
	public class RangoStockTests
	{
		[Test]
		public void Crear_valida_Min_leq_Max()
		{
			var r = RangoStock.Crear(new StockMinimo(1m), new StockMaximo(10m));
			Assert.That(r.Minimo.Value, Is.EqualTo(1m));
			Assert.That(r.Maximo.Value, Is.EqualTo(10m));

			Assert.That(() => RangoStock.Crear(new StockMinimo(5m), new StockMaximo(4m)),
				Throws.TypeOf<BusinessRuleException>());
		}

		[TestCase(0.5, true)]
		[TestCase(1.0, true)]
		[TestCase(10.0, true)]
		[TestCase(10.1, false)]
		[TestCase(0.0, false)]
		public void DentroDelRango_funciona(decimal cantidad, bool esperado)
		{
			var r = RangoStock.Crear(new StockMinimo(1m), new StockMaximo(10m));
			Assert.That(r.DentroDelRango((decimal)cantidad), Is.EqualTo(esperado));
		}
	}
}

