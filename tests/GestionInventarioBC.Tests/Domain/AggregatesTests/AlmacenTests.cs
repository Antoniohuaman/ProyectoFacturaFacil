using NUnit.Framework;
using GestionInventarioBC.Domain.Aggregates;
using GestionInventarioBC.Domain.Events;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Tests.Aggregates
{
	[TestFixture]
	public class AlmacenTests
	{
		private EmpresaId E => EmpresaId.From("20123456789");
		private EstablecimientoId S => EstablecimientoId.From(System.Guid.NewGuid());
		private AlmacenId A => AlmacenId.New();

		[Test]
		public void Crear_emite_evento_y_queda_activo()
		{
			var alm = Almacen.Crear(E, S, A, "Principal");
			Assert.That(alm.EmpresaId.Value, Is.EqualTo("20123456789"));
			Assert.That(alm.Nombre, Is.EqualTo("Principal"));
			Assert.That(alm.Activo, Is.True);
			Assert.That(alm.DomainEvents, Has.Exactly(1).InstanceOf<AlmacenCreado>());
		}

		[Test]
		public void ActualizarNombre_cambia_y_emite_evento()
		{
			var alm = Almacen.Crear(E, S, A, "A");
			alm.ActualizarNombre("B");
			Assert.That(alm.Nombre, Is.EqualTo("B"));
			Assert.That(alm.DomainEvents, Has.Some.InstanceOf<AlmacenActualizado>());
		}

		[Test]
		public void Deshabilitar_emite_evento_y_es_idempotente()
		{
			var alm = Almacen.Crear(E, S, A, "A");
			alm.Deshabilitar();
			Assert.That(alm.Activo, Is.False);
			Assert.That(alm.DomainEvents, Has.Some.InstanceOf<AlmacenDeshabilitado>());
			var count = alm.DomainEvents.Count;
			alm.Deshabilitar(); // no duplica
			Assert.That(alm.DomainEvents.Count, Is.EqualTo(count));
		}
	}
}

