using NUnit.Framework;
using GestionInventarioBC.Domain.Entities;
using GestionInventarioBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Tests.Entities
{
	[TestFixture]
	public class LineaMovimientoTests
	{
		[Test]
		public void Crear_ok_con_productoId_y_cantidad()
		{
			var productoId = ProductoId.New();
			var linea = LineaMovimiento.Crear(productoId, new CantidadStock(2.5m));
			Assert.That(linea.ProductoId, Is.EqualTo(productoId));
			Assert.That(linea.Cantidad.Value, Is.EqualTo(2.5m));
			Assert.That(linea.CostoUnitario, Is.Null);
		}

		[Test]
		public void Crear_con_costo_unitario()
		{
			var productoId = ProductoId.New();
			var cu = CostoUnitario.DesdeDinero(Dinero.Create(10m, Moneda.PEN()));
			var linea = LineaMovimiento.Crear(productoId, new CantidadStock(1m), cu);
			Assert.That(linea.CostoUnitario, Is.Not.Null);
			Assert.That(linea.CostoUnitario!.Valor.Monto, Is.EqualTo(10m));
		}
	}
}

