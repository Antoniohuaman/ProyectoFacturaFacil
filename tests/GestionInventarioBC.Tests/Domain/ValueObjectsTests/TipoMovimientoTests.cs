using NUnit.Framework;
using GestionInventarioBC.Domain.ValueObjects;

namespace GestionInventarioBC.Tests.ValueObjects
{
	[TestFixture]
	public class TipoMovimientoTests
	{
		[Test]
		public void Enum_ValoresExistentes()
		{
			Assert.That((int)TipoMovimiento.Ingreso, Is.EqualTo(0));
			Assert.That((int)TipoMovimiento.Egreso, Is.EqualTo(1));
			Assert.That((int)TipoMovimiento.AjustePositivo, Is.EqualTo(2));
			Assert.That((int)TipoMovimiento.AjusteNegativo, Is.EqualTo(3));
			Assert.That((int)TipoMovimiento.TransferenciaEntrada, Is.EqualTo(4));
			Assert.That((int)TipoMovimiento.TransferenciaSalida, Is.EqualTo(5));
		}
	}
}

