using System;
using NUnit.Framework;
using SharedKernel.ValueObjects;

namespace SharedKernel.Tests.UnitTests.ValueObjects
{
    [TestFixture]
    public class ProductoIdTests
    {
        [Test]
        public void New_genera_guid_no_vacio()
        {
            var id = ProductoId.New();
            Assert.That(id.Value, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void From_con_guid_valido_crea_vo()
        {
            var g = Guid.NewGuid();
            var id = ProductoId.From(g);
            Assert.That(id.Value, Is.EqualTo(g));
        }

        [Test]
        public void From_con_guid_empty_lanza_argumentexception()
        {
            Assert.That(() => ProductoId.From(Guid.Empty),
                Throws.TypeOf<ArgumentException>()
                      .With.Message.Contains("Guid.Empty"));
        }

        [Test]
        public void FromString_con_guid_valido_crea_vo()
        {
            var g = Guid.NewGuid().ToString();
            var id = ProductoId.FromString(g);
            Assert.That(id.Value, Is.EqualTo(Guid.Parse(g)));
        }

        [Test]
        public void FromString_nulo_o_vacio_lanza_argumentexception()
        {
            Assert.That(() => ProductoId.FromString(""),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => ProductoId.FromString("   "),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void FromString_formato_invalido_lanza_argumentexception()
        {
            Assert.That(() => ProductoId.FromString("no-guid"),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void TryParse_valido_true_y_devuelve_id()
        {
            var s = Guid.NewGuid().ToString();
            var ok = ProductoId.TryParse(s, out var id);
            Assert.That(ok, Is.True);
            Assert.That(id.Value, Is.EqualTo(Guid.Parse(s)));
        }

        [Test]
        public void TryParse_invalido_false_y_default()
        {
            var ok = ProductoId.TryParse("x", out var id);
            Assert.That(ok, Is.False);
            Assert.That(id.Value, Is.EqualTo(default(Guid)));
        }

        [Test]
        public void Igualdad_por_valor_y_conversiones()
        {
            var g = Guid.NewGuid();
            var a = (ProductoId)g;     // explícita
            Guid b = a;                // implícita
            var c = ProductoId.From(g);

            Assert.That(b, Is.EqualTo(g));
            Assert.That(a, Is.EqualTo(c));
            Assert.That(a.GetHashCode(), Is.EqualTo(c.GetHashCode()));
        }
    }
}
