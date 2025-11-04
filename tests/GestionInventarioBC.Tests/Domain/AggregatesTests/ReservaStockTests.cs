using System;
using NUnit.Framework;
using GestionInventarioBC.Domain.Aggregates;
using GestionInventarioBC.Domain.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Tests.Aggregates
{
	[TestFixture]
	public class ReservaStockTests
	{
		private EmpresaId E => EmpresaId.From("20123456789");
		private EstablecimientoId S => EstablecimientoId.From(Guid.NewGuid());
		private AlmacenId A => AlmacenId.New();
		private Sku SKU() => Sku.Crear("SKU-RES");

		[Test]
		public void Crear_inicia_Pendiente_y_setea_vencimiento_opcional()
		{
			var vence = DateTimeOffset.UtcNow.AddDays(1);
			var r = ReservaStock.Crear(E, S, A, SKU(), new CantidadStock(2m), vence);
			Assert.That(r.Estado, Is.EqualTo(EstadoReserva.Pendiente));
			Assert.That(r.VenceEn, Is.EqualTo(vence).Within(TimeSpan.FromSeconds(1)));
		}

		[Test]
		public void Confirmar_Liberar_Vencer_Cancelar_respetan_maquina_de_estados()
		{
			var r = ReservaStock.Crear(E, S, A, SKU(), new CantidadStock(1m), null);
			r.Confirmar();
			Assert.That(r.Estado, Is.EqualTo(EstadoReserva.Confirmada));
			Assert.That(() => r.Cancelar(), Throws.TypeOf<BusinessRuleException>());

			// Nueva reserva para probar liberar y vencer
			var r2 = ReservaStock.Crear(E, S, A, SKU(), new CantidadStock(1m), null);
			r2.Liberar();
			Assert.That(r2.Estado, Is.EqualTo(EstadoReserva.Liberada));

			var r3 = ReservaStock.Crear(E, S, A, SKU(), new CantidadStock(1m), null);
			r3.Vencer();
			Assert.That(r3.Estado, Is.EqualTo(EstadoReserva.Vencida));

			var r4 = ReservaStock.Crear(E, S, A, SKU(), new CantidadStock(1m), null);
			r4.Cancelar();
			Assert.That(r4.Estado, Is.EqualTo(EstadoReserva.Cancelada));
			r4.Cancelar(); // idempotente
		}
	}
}

