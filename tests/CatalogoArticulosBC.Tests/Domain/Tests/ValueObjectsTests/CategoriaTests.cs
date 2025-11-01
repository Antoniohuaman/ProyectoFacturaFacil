using System;
using NUnit.Framework;
using SharedKernel.ValueObjects;

namespace CatalogoArticulosBC.Domain.Tests.ValueObjects
{
    [TestFixture]
    public class CategoriaIdTests
    {
        [Test]
        public void New_Genera_Id_NoVacio()
        {
            var id = CategoriaId.New();
            Assert.That(id.Value, Is.Not.EqualTo(Guid.Empty));
            Assert.That(id.ToString(), Is.EqualTo(id.Value.ToString()));
        }

        [Test]
        public void FromString_Valida_Formato()
        {
            var g = Guid.NewGuid();
            var id = CategoriaId.FromString(g.ToString());
            Assert.That((Guid)id, Is.EqualTo(g));
        }

        [Test]
        public void TryParse_RetornaFalse_Para_CadenaInvalida()
        {
            var ok = CategoriaId.TryParse("not-a-guid", out var _);
            Assert.That(ok, Is.False);
        }

        [Test]
        public void From_Empty_Lanza()
        {
            TestDelegate act = () => _ = CategoriaId.From(Guid.Empty);
            Assert.That(act, Throws.TypeOf<ArgumentException>());
        }
    }
}
