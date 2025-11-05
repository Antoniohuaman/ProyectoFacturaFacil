using System;
using NUnit.Framework;
using GestionInventarioBC.Domain.Aggregates;
using GestionInventarioBC.Domain.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Tests.Aggregates
{
	[TestFixture]
	public class TransferenciaInventarioTests
	{
		private EmpresaId E => EmpresaId.From("20123456789");
		private EstablecimientoId ES() => EstablecimientoId.From(Guid.NewGuid());
		private AlmacenId A() => AlmacenId.New();
		private ProductoId P() => ProductoId.New();

		[Test]
		public void Crear_valida_origen_y_destino_distintos()
		{
			var est = ES(); var alm = A();
			Assert.That(() => TransferenciaInventario.Crear(E, est, alm, est, alm, P(), new CantidadStock(1m)),
				Throws.TypeOf<BusinessRuleException>());
		}

		[Test]
		public void Confirmar_y_Cancelar_respetan_reglas()
		{
			var t = TransferenciaInventario.Crear(E, ES(), A(), ES(), A(), P(), new CantidadStock(2m));
			t.Confirmar();
			Assert.That(t.Estado, Is.EqualTo(EstadoTransferencia.Confirmada));
			Assert.That(() => t.Cancelar(), Throws.TypeOf<BusinessRuleException>());

			var t2 = TransferenciaInventario.Crear(E, ES(), A(), ES(), A(), P(), new CantidadStock(1m));
			t2.Cancelar();
			Assert.That(t2.Estado, Is.EqualTo(EstadoTransferencia.Cancelada));
			t2.Cancelar(); // idempotente
		}
	}
}

