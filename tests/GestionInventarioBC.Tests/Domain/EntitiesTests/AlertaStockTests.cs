using NUnit.Framework;
using GestionInventarioBC.Domain.Entities;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Tests.Entities
{
	[TestFixture]
	public class AlertaStockTests
	{
		[Test]
		public void Crear_establece_campos_y_fecha()
		{
			var productoId = ProductoId.New();
			var alerta = AlertaStock.Crear(productoId, disponible: 2m, minimo: 5m, observacion: "Debajo del mínimo");
			Assert.That(alerta.ProductoId, Is.EqualTo(productoId));
			Assert.That(alerta.Disponible, Is.EqualTo(2m));
			Assert.That(alerta.Minimo, Is.EqualTo(5m));
			Assert.That(alerta.Observacion, Is.EqualTo("Debajo del mínimo"));
			Assert.That(alerta.Fecha, Is.Not.EqualTo(default));
		}
	}
}

