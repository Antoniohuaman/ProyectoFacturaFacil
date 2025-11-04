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
		public void Crear_ok_con_sku_y_cantidad()
		{
			var sku = Sku.Crear("ABC-123");
			var linea = LineaMovimiento.Crear(sku, new CantidadStock(2.5m));
			Assert.That(linea.Sku.Valor, Is.EqualTo("ABC-123"));
			Assert.That(linea.Cantidad.Value, Is.EqualTo(2.5m));
			Assert.That(linea.CostoUnitario, Is.Null);
		}

		[Test]
		public void Crear_con_costo_unitario()
		{
			var sku = Sku.Crear("PEN-001");
			var cu = CostoUnitario.DesdeDinero(Dinero.Create(10m, Moneda.PEN()));
			var linea = LineaMovimiento.Crear(sku, new CantidadStock(1m), cu);
			Assert.That(linea.CostoUnitario, Is.Not.Null);
			Assert.That(linea.CostoUnitario!.Valor.Monto, Is.EqualTo(10m));
		}
	}
}

