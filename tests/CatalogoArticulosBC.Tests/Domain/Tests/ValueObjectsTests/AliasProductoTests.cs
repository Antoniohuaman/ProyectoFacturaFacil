#nullable enable
using CatalogoArticulosBC.Domain.ValueObjects;
using NUnit.Framework;

namespace CatalogoArticulosBC.Tests.Domain.Tests.ValueObjectsTests
{
    [TestFixture]
    public class AliasProductoTests
    {
        [Test]
        public void Desde_NormalizaTrimYEspaciosInternos()
        {
            var sut = AliasProducto.Desde("  Polito    talla  M  ");
            Assert.That(sut.Valor, Is.EqualTo("Polito talla M"));
        }

        [Test]
        public void Equals_IgnoraMayusculasMinusculas()
        {
            var a = AliasProducto.Desde("Mi Alias");
            var b = AliasProducto.Desde("mi alias");
            Assert.That(a.Equals(b), Is.True);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void Desde_LanzaSiVacioOSoloEspacios()
        {
            Assert.That(() => AliasProducto.Desde("   "), Throws.Exception);
        }

        [Test]
        public void Desde_LanzaSiExcedeMaxLen()
        {
            var largo = new string('x', 121);
            Assert.That(() => AliasProducto.Desde(largo), Throws.Exception);
        }

        [Test]
        public void ToString_DevuelveElValorNormalizado()
        {
            var sut = AliasProducto.Desde("  A  B  ");
            Assert.That(sut.ToString(), Is.EqualTo("A B"));
        }
    }
}
