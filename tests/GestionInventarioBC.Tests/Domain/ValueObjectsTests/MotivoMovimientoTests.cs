using NUnit.Framework;
using GestionInventarioBC.Domain.ValueObjects;

namespace GestionInventarioBC.Tests.ValueObjects
{
	[TestFixture]
	public class MotivoMovimientoTests
	{
		[Test]
		public void Enum_ValoresEsperados()
		{
			Assert.That((int)MotivoMovimiento.Desconocido, Is.EqualTo(0));
			Assert.That((int)MotivoMovimiento.Compra, Is.EqualTo(1));
			Assert.That((int)MotivoMovimiento.Venta, Is.EqualTo(2));
			Assert.That((int)MotivoMovimiento.DevolucionCompra, Is.EqualTo(3));
			Assert.That((int)MotivoMovimiento.DevolucionVenta, Is.EqualTo(4));
			Assert.That((int)MotivoMovimiento.Ajuste, Is.EqualTo(5));
			Assert.That((int)MotivoMovimiento.Transferencia, Is.EqualTo(6));
			Assert.That((int)MotivoMovimiento.Produccion, Is.EqualTo(7));
		}
	}
}

