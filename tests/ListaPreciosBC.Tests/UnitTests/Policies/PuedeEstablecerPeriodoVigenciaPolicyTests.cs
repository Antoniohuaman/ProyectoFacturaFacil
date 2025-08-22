using NUnit.Framework;
using ListaPreciosBC.Domain.Policies;
using System;

namespace ListaPreciosBC.Tests.UnitTests.Policies
{
	[TestFixture]
	public class PuedeEstablecerPeriodoVigenciaPolicyTests
	{
		[Test]
		public void Validar_DesdeMenorQueHasta_RetornaTrue()
		{
			var desde = new DateTime(2025, 1, 1);
			var hasta = new DateTime(2025, 12, 31);
			var resultado = PuedeEstablecerPeriodoVigenciaPolicy.Validar(desde, hasta);
			Assert.That(resultado, Is.True);
		}

		[Test]
		public void Validar_DesdeIgualQueHasta_RetornaTrue()
		{
			var desde = new DateTime(2025, 1, 1);
			var hasta = new DateTime(2025, 1, 1);
			var resultado = PuedeEstablecerPeriodoVigenciaPolicy.Validar(desde, hasta);
			Assert.That(resultado, Is.True);
		}

		[Test]
		public void Validar_DesdeMayorQueHasta_RetornaFalse()
		{
			var desde = new DateTime(2025, 12, 31);
			var hasta = new DateTime(2025, 1, 1);
			var resultado = PuedeEstablecerPeriodoVigenciaPolicy.Validar(desde, hasta);
			Assert.That(resultado, Is.False);
		}

		[Test]
		public void Validar_HastaNull_RetornaTrue()
		{
			var desde = new DateTime(2025, 1, 1);
			DateTime? hasta = null;
			var resultado = PuedeEstablecerPeriodoVigenciaPolicy.Validar(desde, hasta);
			Assert.That(resultado, Is.True);
		}
	}
}
