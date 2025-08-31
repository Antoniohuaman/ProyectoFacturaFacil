using System;
using NUnit.Framework;
using SharedKernel.ValueObjects;
using SharedKernel.Exceptions;

namespace SharedKernel.Tests.ValueObjects
{
    [TestFixture]
    public class EstablecimientoIdTests
    {
        [Test]
        public void New_GeneraGuidNoVacio_ConFormatoD()
        {
            var id = EstablecimientoId.New();
            Assert.That(id.IsEmpty, Is.False);
            Assert.That(id.Value, Is.Not.EqualTo(Guid.Empty));
            Assert.That(Guid.TryParse(id.ToString(), out _), Is.True); // formato "D"
        }

        [Test]
        public void From_ConGuidEmpty_LanzaBusinessRuleException()
        {
            Assert.That(() => EstablecimientoId.From(Guid.Empty),
                Throws.TypeOf<BusinessRuleException>());
        }

        [Test]
        public void FromString_Valido_CreaId()
        {
            var g = Guid.NewGuid().ToString("D");
            var id = EstablecimientoId.FromString(g);
            Assert.That(id.Value.ToString("D"), Is.EqualTo(g));
        }

        [Test]
        public void FromString_Invalido_LanzaExcepcion()
        {
            Assert.That(() => EstablecimientoId.FromString(""),
                Throws.TypeOf<BusinessRuleException>());
            Assert.That(() => EstablecimientoId.FromString("no-guid"),
                Throws.TypeOf<BusinessRuleException>());
        }

        [Test]
        public void TryParse_NoLanzaYDevuelveFalseCuandoNoValido()
        {
            var ok = EstablecimientoId.TryParse(Guid.NewGuid().ToString("D"), out var idOk);
            Assert.That(ok, Is.True);
            Assert.That(idOk, Is.Not.Null);

            var bad = EstablecimientoId.TryParse("xxx", out var idBad);
            Assert.That(bad, Is.False);
            Assert.That(idBad, Is.Null);
        }

        [Test]
        public void IgualdadPorGuid_EsCorrecta()
        {
            var g = Guid.NewGuid();
            var a = EstablecimientoId.From(g);
            var b = EstablecimientoId.From(g);
            var c = EstablecimientoId.New();

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.EsMismoQue(b), Is.True);
            Assert.That(a, Is.Not.EqualTo(c));
            Assert.That(a.EsMismoQue(c), Is.False);
        }

        [Test]
        public void Conversiones_ExplicitaEImplicita_Funcionan()
        {
            var g = Guid.NewGuid();
            var id = (EstablecimientoId)g; // explícita Guid -> EstablecimientoId
            Guid back = id;                // implícita EstablecimientoId -> Guid

            Assert.That(back, Is.EqualTo(g));
        }
    }
}
