using System;
using IndicadoresNegocioBC.Domain.ValueObjects;
using NUnit.Framework;

namespace IndicadoresNegocioBC.Tests.UnitTests.ValueObjects
{
    public class SegmentoIndicadorTests
    {
        private static Moneda PEN => new Moneda("PEN");

        private static Establecimiento CrearEst(Guid empresaId, string nombre = "Tienda A")
        {
            return Establecimiento.Crear(
                empresaId: empresaId,
                establecimientoId: Guid.NewGuid(),
                nombre: nombre
            );
        }

        // -------------------- ParaEmpresa --------------------

        [Test]
        public void ParaEmpresa_CreaSegmentoEmpresaCompleta()
        {
            var empresaId = Guid.NewGuid();

            var seg = SegmentoIndicador.ParaEmpresa(empresaId, PEN);

            Assert.Multiple(() =>
            {
                Assert.That(seg.EmpresaId, Is.EqualTo(empresaId));
                Assert.That(seg.Moneda, Is.EqualTo(PEN));
                Assert.That(seg.Establecimiento, Is.Null);
                Assert.That(seg.EsEmpresaCompleta, Is.True);
            });
        }

        [Test]
        public void ParaEmpresa_EmpresaIdVacio_LanzaArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                SegmentoIndicador.ParaEmpresa(Guid.Empty, PEN));
        }

        [Test]
        public void ParaEmpresa_MonedaNula_LanzaArgumentNullException()
        {
            var empresaId = Guid.NewGuid();
            Assert.Throws<ArgumentNullException>(() =>
                SegmentoIndicador.ParaEmpresa(empresaId, null!));
        }

        // -------------------- ParaEstablecimiento --------------------

        [Test]
        public void ParaEstablecimiento_CreaSegmentoConEstablecimiento()
        {
            var empresaId = Guid.NewGuid();
            var est = CrearEst(empresaId);

            var seg = SegmentoIndicador.ParaEstablecimiento(est, PEN);

            Assert.Multiple(() =>
            {
                Assert.That(seg.EmpresaId, Is.EqualTo(empresaId));
                Assert.That(seg.Moneda, Is.EqualTo(PEN));
                Assert.That(seg.Establecimiento, Is.Not.Null);
                Assert.That(seg.Establecimiento!.EmpresaId, Is.EqualTo(empresaId));
                Assert.That(seg.EsEmpresaCompleta, Is.False);
            });
        }

        [Test]
        public void ParaEstablecimiento_Nulo_LanzaArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                SegmentoIndicador.ParaEstablecimiento(null!, PEN));
        }

        // -------------------- ConEstablecimiento / ParaTodaLaEmpresa --------------------

        [Test]
        public void ConEstablecimiento_Valido_CambiaAScopeDeEstablecimiento()
        {
            var empresaId = Guid.NewGuid();
            var baseSeg = SegmentoIndicador.ParaEmpresa(empresaId, PEN);
            var est = CrearEst(empresaId);

            var seg = baseSeg.ConEstablecimiento(est);

            Assert.Multiple(() =>
            {
                Assert.That(seg.EmpresaId, Is.EqualTo(empresaId));
                Assert.That(seg.Moneda, Is.EqualTo(PEN));
                Assert.That(seg.Establecimiento, Is.Not.Null);
                Assert.That(seg.EsEmpresaCompleta, Is.False);
            });
        }

        [Test]
        public void ConEstablecimiento_DeOtraEmpresa_LanzaInvalidOperationException()
        {
            var empresaA = Guid.NewGuid();
            var empresaB = Guid.NewGuid();

            var baseSeg = SegmentoIndicador.ParaEmpresa(empresaA, PEN);
            var estDeOtraEmpresa = CrearEst(empresaB);

            Assert.Throws<InvalidOperationException>(() =>
                baseSeg.ConEstablecimiento(estDeOtraEmpresa));
        }

        [Test]
        public void ParaTodaLaEmpresa_DesdeSegmentoConEstablecimiento_QuitaEstablecimiento()
        {
            var empresaId = Guid.NewGuid();
            var est = CrearEst(empresaId);
            var conEst = SegmentoIndicador.ParaEstablecimiento(est, PEN);

            var seg = conEst.ParaTodaLaEmpresa();

            Assert.Multiple(() =>
            {
                Assert.That(seg.EmpresaId, Is.EqualTo(empresaId));
                Assert.That(seg.Moneda, Is.EqualTo(PEN));
                Assert.That(seg.Establecimiento, Is.Null);
                Assert.That(seg.EsEmpresaCompleta, Is.True);
            });
        }

        // -------------------- ToString --------------------

        [Test]
        public void ToString_MuestraScopeYMoneda()
        {
            var empresaId = Guid.NewGuid();
            var est = CrearEst(empresaId);

            var segEmpresa = SegmentoIndicador.ParaEmpresa(empresaId, PEN);
            var segEst = SegmentoIndicador.ParaEstablecimiento(est, PEN);

            var tEmpresa = segEmpresa.ToString();
            var tEst = segEst.ToString();

            Assert.Multiple(() =>
            {
                Assert.That(tEmpresa, Does.Contain("Empresa"));
                Assert.That(tEmpresa, Does.Contain("PEN"));

                Assert.That(tEst, Does.Contain("Establecimiento:"));
                Assert.That(tEst, Does.Contain(est.EstablecimientoId.ToString()));
                Assert.That(tEst, Does.Contain("PEN"));
            });
        }
    }
}





