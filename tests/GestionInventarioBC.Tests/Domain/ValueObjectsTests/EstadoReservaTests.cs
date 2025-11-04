using NUnit.Framework;
using GestionInventarioBC.Domain.ValueObjects;

namespace GestionInventarioBC.Tests.ValueObjects
{
	[TestFixture]
	public class EstadoReservaTests
	{
		[Test]
		public void Enum_ValoresEsperados()
		{
			Assert.That((int)EstadoReserva.Pendiente, Is.EqualTo(0));
			Assert.That((int)EstadoReserva.Confirmada, Is.EqualTo(1));
			Assert.That((int)EstadoReserva.Liberada, Is.EqualTo(2));
			Assert.That((int)EstadoReserva.Vencida, Is.EqualTo(3));
			Assert.That((int)EstadoReserva.Cancelada, Is.EqualTo(4));
		}
	}
}

