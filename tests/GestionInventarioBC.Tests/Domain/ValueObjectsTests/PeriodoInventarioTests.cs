using System;
using NUnit.Framework;
using GestionInventarioBC.Domain.ValueObjects;
using SharedKernel.Exceptions;

namespace GestionInventarioBC.Tests.ValueObjects
{
	[TestFixture]
	public class PeriodoInventarioTests
	{
		[Test]
		public void Crear_valido_e_invalido()
		{
			var desde = new DateOnly(2025, 1, 1);
			var hasta = new DateOnly(2025, 1, 31);
			var p = PeriodoInventario.Crear(desde, hasta);
			Assert.That(p.Desde, Is.EqualTo(desde));
			Assert.That(p.Hasta, Is.EqualTo(hasta));

			Assert.That(() => PeriodoInventario.Crear(new DateOnly(2025, 2, 1), new DateOnly(2025, 1, 31)),
				Throws.TypeOf<BusinessRuleException>());
		}

		[Test]
		public void Mensual_construye_primero_y_ultimo_dia()
		{
			var p = PeriodoInventario.Mensual(2025, 2);
			Assert.That(p.Desde, Is.EqualTo(new DateOnly(2025, 2, 1)));
			Assert.That(p.Hasta, Is.EqualTo(new DateOnly(2025, 2, 28)));
		}

		[Test]
		public void Contiene_responde_correctamente()
		{
			var p = PeriodoInventario.Crear(new DateOnly(2025, 1, 10), new DateOnly(2025, 1, 20));
			Assert.That(p.Contiene(new DateOnly(2025, 1, 9)), Is.False);
			Assert.That(p.Contiene(new DateOnly(2025, 1, 10)), Is.True);
			Assert.That(p.Contiene(new DateOnly(2025, 1, 20)), Is.True);
			Assert.That(p.Contiene(new DateOnly(2025, 1, 21)), Is.False);
		}
	}
}

