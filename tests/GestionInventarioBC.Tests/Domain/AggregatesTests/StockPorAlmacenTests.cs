using NUnit.Framework;
using GestionInventarioBC.Domain.Aggregates;
using GestionInventarioBC.Domain.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Tests.Aggregates
{
	[TestFixture]
	public class StockPorAlmacenTests
	{
		private EmpresaId E => EmpresaId.From("20123456789");
		private EstablecimientoId S => EstablecimientoId.From(System.Guid.NewGuid());
		private AlmacenId A => AlmacenId.New();
        private ProductoId P() => ProductoId.New();

		[Test]
		public void CrearNuevo_inicia_en_cero()
		{
			var s = StockPorAlmacen.CrearNuevo(E, S, A, P());
			Assert.That(s.Real.Value, Is.EqualTo(0m));
			Assert.That(s.Reservado.Value, Is.EqualTo(0m));
			Assert.That(s.Disponible.Value, Is.EqualTo(0m));
		}

		[Test]
		public void Ingresar_y_Egresar_valida_disponible()
		{
			var s = StockPorAlmacen.CrearNuevo(E, S, A, P());
			s.Ingresar(new CantidadStock(10m));
			Assert.That(s.Real.Value, Is.EqualTo(10m));

			s.Egresar(new CantidadStock(3m));
			Assert.That(s.Real.Value, Is.EqualTo(7m));

			Assert.That(() => s.Egresar(new CantidadStock(8m)), Throws.TypeOf<BusinessRuleException>());
		}

		[Test]
		public void Reservar_y_Liberar_controlan_invariantes()
		{
			var s = StockPorAlmacen.CrearNuevo(E, S, A, P());
			s.Ingresar(new CantidadStock(5m));

			s.Reservar(new CantidadStock(3m));
			Assert.That(s.Reservado.Value, Is.EqualTo(3m));
			Assert.That(s.Disponible.Value, Is.EqualTo(2m));

			Assert.That(() => s.Reservar(new CantidadStock(3m)), Throws.TypeOf<BusinessRuleException>());

			s.LiberarReserva(new CantidadStock(2m));
			Assert.That(s.Reservado.Value, Is.EqualTo(1m));
			Assert.That(() => s.LiberarReserva(new CantidadStock(5m)), Throws.TypeOf<BusinessRuleException>());
		}
	}
}

