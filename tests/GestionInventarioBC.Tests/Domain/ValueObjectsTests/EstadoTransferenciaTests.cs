using NUnit.Framework;
using GestionInventarioBC.Domain.ValueObjects;

namespace GestionInventarioBC.Tests.ValueObjects
{
	[TestFixture]
	public class EstadoTransferenciaTests
	{
		[Test]
		public void Enum_ValoresEsperados()
		{
			Assert.That((int)EstadoTransferencia.Creada, Is.EqualTo(0));
			Assert.That((int)EstadoTransferencia.Confirmada, Is.EqualTo(1));
			Assert.That((int)EstadoTransferencia.Cancelada, Is.EqualTo(2));
		}
	}
}

