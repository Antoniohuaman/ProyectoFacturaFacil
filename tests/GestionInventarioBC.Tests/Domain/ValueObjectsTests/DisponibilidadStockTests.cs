using NUnit.Framework;
using GestionInventarioBC.Domain.ValueObjects;
using SharedKernel.Exceptions;

namespace GestionInventarioBC.Tests.ValueObjects
{
	[TestFixture]
	public class DisponibilidadStockTests
	{
		[Test]
		public void Disponible_es_derivado_y_no_negativo()
		{
			var disp = DisponibilidadStock.Crear(new CantidadStock(10m), new CantidadStock(3m));
			Assert.That(disp.Disponible.Value, Is.EqualTo(7m));

			// Reservado > Real debe fallar por la resta no permitida en CantidadStock
			Assert.That(() => DisponibilidadStock.Crear(new CantidadStock(1m), new CantidadStock(2m)),
				Throws.TypeOf<BusinessRuleException>());
		}
	}
}

