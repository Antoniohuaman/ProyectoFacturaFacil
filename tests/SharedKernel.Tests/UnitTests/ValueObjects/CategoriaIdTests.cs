using System;
using NUnit.Framework;
using SharedKernel.ValueObjects;

namespace SharedKernel.Tests.ValueObjects
{
    [TestFixture]
    public class CategoriaIdTests
    {
        [Test]
        public void New_NoDebeSerEmpty()
        {
            var id = CategoriaId.New();
            Assert.That((Guid)id, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void From_ConGuidValido_Ok()
        {
            var g = Guid.NewGuid();
            var id = CategoriaId.From(g);
            Assert.That((Guid)id, Is.EqualTo(g));
        }

        [Test]
        public void From_ConGuidEmpty_LanzaArgumentException()
        {
            Assert.That(() => CategoriaId.From(Guid.Empty), Throws.ArgumentException);
        }

        [Test]
        public void FromString_Valido_Ok()
        {
            var g = Guid.NewGuid();
            var id = CategoriaId.FromString(g.ToString());
            Assert.That((Guid)id, Is.EqualTo(g));
        }

        [Test]
        public void FromString_Invalido_LanzaFormatException()
        {
            Assert.That(() => CategoriaId.FromString("no-guid"), Throws.TypeOf<FormatException>());
        }

        [Test]
        public void TryParse_Valido_RetornaTrueYAsigna()
        {
            var g = Guid.NewGuid().ToString();
            var ok = CategoriaId.TryParse(g, out var id);
            Assert.That(ok, Is.True);
            Assert.That((Guid)id, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void TryParse_Invalido_RetornaFalseYSaleDefault()
        {
            var ok = CategoriaId.TryParse("xxx", out var id);
            Assert.That(ok, Is.False);
            Assert.That((Guid)id, Is.EqualTo(default(Guid)));
        }

        [Test]
        public void Igualdad_PorValor()
        {
            var g = Guid.NewGuid();
            var a = CategoriaId.From(g);
            var b = CategoriaId.From(g);
            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void ToString_DevuelveGuidString()
        {
            var g = Guid.NewGuid();
            var id = CategoriaId.From(g);
            Assert.That(id.ToString(), Is.EqualTo(g.ToString()));
        }
    }
}
