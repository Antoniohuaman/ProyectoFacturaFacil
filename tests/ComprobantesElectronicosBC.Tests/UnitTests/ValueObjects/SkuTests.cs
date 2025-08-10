using System;
using NUnit.Framework;
using ComprobantesElectronicosBC.Domain.ValueObjects;

namespace ComprobantesElectronicosBC.Tests.UnitTests.ValueObjects
{
    [TestFixture]
    public class SkuTests
    {
        // --- Creación y normalización ---

        [Test]
        public void Create_PreservaCasing_YExponeCanonicalEnUpper()
        {
            var sku = Sku.Create("ab-01.X");

            Assert.Multiple(() =>
            {
                Assert.That(sku.Value, Is.EqualTo("ab-01.X"));      // se preserva lo tecleado
                Assert.That(sku.Canonical, Is.EqualTo("AB-01.X"));  // canónico en upper
                Assert.That(sku.ToString(), Is.EqualTo("ab-01.X")); // ToString = Value
            });
        }

        [Test]
        public void Create_AceptaCaracteresPermitidos_YLongitudHasta30()
        {
            // Mínimo válido: primer char alfanumérico
            var s1 = Sku.Create("A");
            Assert.That(s1.Value, Is.EqualTo("A"));

            // Mezcla permitida: letras/números/.-_
            var s2 = Sku.Create("A_B-1.2");
            Assert.That(s2.Canonical, Is.EqualTo("A_B-1.2".ToUpperInvariant()));

            // Límite: 30 chars
            var treinta = new string('a', 30);
            var s3 = Sku.Create(treinta);
            Assert.That(s3.Value, Is.EqualTo(treinta));
        }

        // --- Igualdad y hash (case-insensitive vía Canonical) ---

        [Test]
        public void Equality_IgnoraMayusculasMinusculas_YHashConsistente()
        {
            var a = Sku.Create("abc-001");
            var b = Sku.Create("ABC-001");

            Assert.Multiple(() =>
            {
                Assert.That(a, Is.EqualTo(b));                          // igualdad por valor
                Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode())); // hash consistente
            });
        }

        // --- Validaciones: formato ---

        [Test]
        public void Create_LanzaSiVacio_OEspacios()
        {
            Assert.Throws<ArgumentException>(() => Sku.Create(""));
            Assert.Throws<ArgumentException>(() => Sku.Create("   "));
        }

        [Test]
        public void Create_LanzaSiLongitudMayorA30()
        {
            var treintaYUno = new string('A', 31);
            var ex = Assert.Throws<ArgumentException>(() => Sku.Create(treintaYUno));
            Assert.That(ex!.Message, Does.Contain("exceder 30"));
        }

        [Test]
        public void Create_LanzaSiCaracteresNoPermitidos_EspaciosOBarrasOSimbolos()
        {
            Assert.Throws<ArgumentException>(() => Sku.Create("A B"));   // espacio interno
            Assert.Throws<ArgumentException>(() => Sku.Create("A/B"));   // slash
            Assert.Throws<ArgumentException>(() => Sku.Create("A@B"));   // @
            Assert.Throws<ArgumentException>(() => Sku.Create("ÁBC"));   // acentos
        }

        [Test]
        public void Create_LanzaSiNoIniciaConAlfanumerico()
        {
            Assert.Throws<ArgumentException>(() => Sku.Create(".AB"));
            Assert.Throws<ArgumentException>(() => Sku.Create("-AB"));
            Assert.Throws<ArgumentException>(() => Sku.Create("_AB"));
        }

        [Test]
        public void Create_LanzaSiTerminaEnPuntoGuionOGuionBajo()
        {
            Assert.Throws<ArgumentException>(() => Sku.Create("AB."));
            Assert.Throws<ArgumentException>(() => Sku.Create("AB-"));
            Assert.Throws<ArgumentException>(() => Sku.Create("AB_"));
        }

        // --- TryCreate ---

        [Test]
        public void TryCreate_TrueCuandoValido_FalseCuandoInvalido()
        {
            var ok = Sku.TryCreate("mi-Sku_01", out var s1);
            var no = Sku.TryCreate("   ", out var s2);

            Assert.Multiple(() =>
            {
                Assert.That(ok, Is.True);
                Assert.That(s1, Is.Not.Null);
                Assert.That(s1!.Canonical, Is.EqualTo("MI-SKU_01"));

                Assert.That(no, Is.False);
                Assert.That(s2, Is.Null);
            });
        }

        // --- LooksLikeSkuToken (heurística para búsquedas/validación UI) ---

        [Test]
        public void LooksLikeSkuToken_ValidaTokensBasicos()
        {
            Assert.Multiple(() =>
            {
                Assert.That(Sku.LooksLikeSkuToken("x"), Is.True);
                Assert.That(Sku.LooksLikeSkuToken("ab-01.x"), Is.True);
                Assert.That(Sku.LooksLikeSkuToken("   "), Is.False);   // vacío/espacios
                Assert.That(Sku.LooksLikeSkuToken(".abc"), Is.False);  // no inicia alfanumérico
                Assert.That(Sku.LooksLikeSkuToken("abc*"), Is.False);  // * no permitido
                Assert.That(Sku.LooksLikeSkuToken(new string('a', 31)), Is.False); // > 30
            });
        }
    }
}
