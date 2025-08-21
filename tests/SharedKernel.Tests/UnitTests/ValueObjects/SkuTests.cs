using NUnit.Framework;
using SharedKernel.ValueObjects;

namespace SharedKernel.Tests.UnitTests.ValueObjects
{
    [TestFixture]
    public class SkuTests
    {
        [TestCase("abc123",             "ABC123")]
        [TestCase(" Cap-258963 ",       "CAP-258963")]
        [TestCase("A/1.2",              "A/1.2")]
        [TestCase("A   B   C",          "A B C")]  // compacta espacios
        [TestCase("Z9-9/9.9",           "Z9-9/9.9")]
        public void Crea_y_normaliza_correctamente(string entrada, string esperado)
        {
            var sku = Sku.Crear(entrada);
            Assert.That(sku.Valor, Is.EqualTo(esperado));
        }

        [Test]
        public void Rechaza_vacio_o_blancos()
        {
            Assert.That(() => Sku.Crear(""), Throws.ArgumentException);
            Assert.That(() => Sku.Crear("   "), Throws.ArgumentException);
        }

        [Test]
        public void Rechaza_longitud_mayor_a_30()
        {
            var largo31 = new string('A', 31);
            Assert.That(() => Sku.Crear(largo31), Throws.ArgumentException);
        }

        [TestCase("_ABC")]   // no inicia en alfanumérico
        [TestCase("-ABC")]   // no inicia en alfanumérico
        [TestCase("ABC_1")]  // '_' no permitido en set SUNAT an
        [TestCase("ABC@1")]  // '@' no permitido
        public void Rechaza_caracteres_no_permitidos(string entrada)
        {
            Assert.That(() => Sku.Crear(entrada), Throws.ArgumentException);
        }

        [Test]
        public void TryCrear_devuelve_error_explicito()
        {
            var ok = Sku.TryCrear("Cap-258963", out var sku, out var err);
            Assert.That(ok, Is.True);
            Assert.That(sku!.Valor, Is.EqualTo("CAP-258963"));
            Assert.That(err, Is.Null);

            var bad = Sku.TryCrear("ABC_1", out var sku2, out var err2);
            Assert.That(bad, Is.False);
            Assert.That(sku2, Is.Null);
            Assert.That(err2, Does.Contain("sólo puede contener"));
        }

        [Test]
        public void Igualdad_y_hash_por_valor_normalizado()
        {
            var a = Sku.Crear("cap-258963");
            var b = Sku.Crear("CAP-258963");
            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }
    }
}
