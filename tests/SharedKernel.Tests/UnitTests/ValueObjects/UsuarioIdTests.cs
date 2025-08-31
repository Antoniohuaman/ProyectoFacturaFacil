using System;
using NUnit.Framework;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace SharedKernel.Tests.ValueObjects
{
    [TestFixture]
    public class UsuarioIdTests
    {
        [Test]
        public void New_GeneraValorNoVacio()
        {
            var id = UsuarioId.New();

            Assert.That(id.IsEmpty, Is.False);
            Assert.That(Guid.TryParse(id.ToString(), out var g), Is.True);
            Assert.That(g, Is.EqualTo(id.Value));
        }

        [Test]
        public void From_ConGuidValido_RetornaInstanciaConEseValor()
        {
            var guid = Guid.NewGuid();

            var id = UsuarioId.From(guid);

            Assert.That((Guid)id, Is.EqualTo(guid));
            Assert.That(id.Value, Is.EqualTo(guid));
            Assert.That(id.IsEmpty, Is.False);
        }

        [Test]
        public void From_ConGuidEmpty_LanzaBusinessRuleException()
        {
            Assert.That(() => UsuarioId.From(Guid.Empty),
                Throws.TypeOf<BusinessRuleException>());
        }

        [Test]
        public void TryFrom_ConGuidValido_TrueYEntregaInstancia()
        {
            var guid = Guid.NewGuid();

            var ok = UsuarioId.TryFrom(guid, out var id);

            Assert.That(ok, Is.True);
            Assert.That(id.IsEmpty, Is.False);
            Assert.That(id.Value, Is.EqualTo(guid));
        }

        [Test]
        public void TryFrom_ConGuidEmpty_FalseYEntregaDefault()
        {
            var ok = UsuarioId.TryFrom(Guid.Empty, out var id);

            Assert.That(ok, Is.False);
            Assert.That(id.IsEmpty, Is.True);
            Assert.That((Guid)id, Is.EqualTo(Guid.Empty));
        }

        [Test]
        public void FromString_ConCadenaValida_RetornaInstancia()
        {
            var guid = Guid.NewGuid();
            var s = guid.ToString();

            var id = UsuarioId.FromString(s);

            Assert.That(id.Value, Is.EqualTo(guid));
            Assert.That(id.IsEmpty, Is.False);
        }

        [Test]
        public void FromString_ConNuloOVacio_LanzaBusinessRuleException()
        {
            Assert.That(() => UsuarioId.FromString(null!), Throws.TypeOf<BusinessRuleException>());
            Assert.That(() => UsuarioId.FromString(""), Throws.TypeOf<BusinessRuleException>());
            Assert.That(() => UsuarioId.FromString("   "), Throws.TypeOf<BusinessRuleException>());
        }

        [Test]
        public void FromString_ConFormatoInvalido_LanzaBusinessRuleException()
        {
            Assert.That(() => UsuarioId.FromString("no-es-un-guid"),
                Throws.TypeOf<BusinessRuleException>());
        }

        [Test]
        public void TryParse_ConCadenaValida_TrueYEntregaInstancia()
        {
            var guid = Guid.NewGuid();
            var ok = UsuarioId.TryParse(guid.ToString(), out var id);

            Assert.That(ok, Is.True);
            Assert.That(id.IsEmpty, Is.False);
            Assert.That(id.Value, Is.EqualTo(guid));
        }

        [Test]
        public void TryParse_ConCadenaInvalida_FalseYEntregaDefault()
        {
            var ok = UsuarioId.TryParse("xxxx", out var id);

            Assert.That(ok, Is.False);
            Assert.That(id.IsEmpty, Is.True);
        }

        [Test]
        public void Conversion_Explicita_AyDesdeGuid_Funciona()
        {
            var guid = Guid.NewGuid();

            UsuarioId idDesdeGuid = (UsuarioId)guid;
            Guid guidDesdeId = (Guid)idDesdeGuid;

            Assert.That(idDesdeGuid.Value, Is.EqualTo(guid));
            Assert.That(guidDesdeId, Is.EqualTo(guid));
        }

        [Test]
        public void Igualdad_DosIdsConMismoGuid_SonIguales()
        {
            var guid = Guid.NewGuid();

            var a = UsuarioId.From(guid);
            var b = UsuarioId.From(guid);
            var c = UsuarioId.New();

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
            Assert.That(a, Is.Not.EqualTo(c));
        }

        [Test]
        public void Empty_Static_EsGuidEmptyYSeReportaComoVacio()
        {
            var empty = UsuarioId.Empty;

            Assert.That(empty.IsEmpty, Is.True);
            Assert.That((Guid)empty, Is.EqualTo(Guid.Empty));
        }
    }
}
