using System;
using NUnit.Framework;
using ListaPreciosBC.Domain.Policies;

namespace ListaPreciosBC.Tests.Domain.Tests.BusinessRulesTests.PoliciesTests
{
    [TestFixture]
    public class PuedeEstablecerPeriodoVigenciaPolicyTests
    {
        [Test]
        public void Validar_DeberiaRetornarTrue_CuandoDesdeEsMenorQueHasta()
        {
            var desde = new DateTime(2025, 1, 1);
            var hasta = new DateTime(2025, 12, 31);
            var resultado = PuedeEstablecerPeriodoVigenciaPolicy.Validar(desde, hasta);
                Assert.That(resultado, Is.True);
        }

        [Test]
        public void Validar_DeberiaRetornarTrue_CuandoDesdeEsIgualQueHasta()
        {
            var fecha = new DateTime(2025, 5, 5);
            var resultado = PuedeEstablecerPeriodoVigenciaPolicy.Validar(fecha, fecha);
                Assert.That(resultado, Is.True);
        }

        [Test]
        public void Validar_DeberiaRetornarTrue_CuandoHastaEsNull()
        {
            var desde = new DateTime(2025, 1, 1);
            DateTime? hasta = null;
            var resultado = PuedeEstablecerPeriodoVigenciaPolicy.Validar(desde, hasta);
                Assert.That(resultado, Is.True);
        }

        [Test]
        public void Validar_DeberiaRetornarFalse_CuandoHastaEsMenorQueDesde()
        {
            var desde = new DateTime(2025, 12, 31);
            var hasta = new DateTime(2025, 1, 1);
            var resultado = PuedeEstablecerPeriodoVigenciaPolicy.Validar(desde, hasta);
                Assert.That(resultado, Is.False);
        }
    }
}
