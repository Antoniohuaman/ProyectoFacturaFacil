using System;
using System.Linq;
using NUnit.Framework;
using ComprobantesElectronicosBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace ComprobantesElectronicosBC.Tests.UnitTests.ValueObjects
{
    [TestFixture]
    public class EmailTests
    {
        [Test]
        public void Create_NormalizaDominio_Y_RechazaDisplayName()
        {
            // Dominio se normaliza a minúsculas; se recortan envolturas <...>
            var e = Email.Create("   <User@EXAMPLE.COM>   ");
            Assert.Multiple(() =>
            {
                Assert.That(e.Value, Is.EqualTo("User@example.com"));
                Assert.That(e.Domain, Is.EqualTo("example.com"));
                Assert.That(e.LocalPart, Is.EqualTo("User"));
            });

            // Display name no permitido
            Assert.Throws<ArgumentException>(() => Email.Create("Pepito Perez <pepito@example.com>"));
        }

        [Test]
        public void Create_ValidaEtiquetasDominio_Y_TLD()
        {
            // Etiqueta no puede empezar con guion
            Assert.Throws<ArgumentException>(() => Email.Create("a@-bad.com"));

            // TLD de 1 caracter no permitido
            Assert.Throws<ArgumentException>(() => Email.Create("a@dom.c"));
        }

        [Test]
        public void Create_LongitudesInvalidas_Lanzan()
        {
            // Parte local vacía
            Assert.Throws<ArgumentException>(() => Email.Create("@example.com"));

            // Dominio vacío
            Assert.Throws<ArgumentException>(() => Email.Create("user@"));

            // Email total demasiado largo (truco: TLD exagerado)
            var largo = "u@" + new string('a', 250) + ".com";
            Assert.Throws<ArgumentException>(() => Email.Create(largo));
        }

        [Test]
        public void Igualdad_MismoValor_EsTrue()
        {
            var a = Email.Create("A@Example.Com");
            var b = Email.Create("A@example.com");
            Assert.That(a, Is.EqualTo(b));
        }

        // --------- Parseo de listas (UI: 0..5 opcional / 1..5 obligatorio) ---------

        [Test]
        public void ParseListOrEmpty_EntradaVacia_RetornaListaVacia()
        {
            var list = Email.ParseListOrEmpty(null);
            Assert.That(list, Is.Empty);
        }

        [Test]
        public void ParseListOrEmpty_SeparadoresVariados_Y_SinDuplicados()
        {
            var raw = "a@x.com;  b@y.com  c@z.com,  a@x.com\tb@y.com";
            var list = Email.ParseListOrEmpty(raw); // max 5 por defecto

            Assert.Multiple(() =>
            {
                Assert.That(list.Count, Is.EqualTo(3)); // se colapsan duplicados exactos
                Assert.That(list.Select(x => x.Value),
                    Is.EquivalentTo(new[] { "a@x.com", "b@y.com", "c@z.com" }));
            });
        }

        [Test]
        public void ParseList_ExigeAlMenosUno_Y_LimitaMaximoCinco()
        {
            // Debe exigir al menos 1
            Assert.Throws<ArgumentException>(() => Email.ParseList("   "));

            // 6 elementos debe fallar por límite
            var seis = "a@x.com b@y.com c@z.com d@w.com e@v.com f@u.com";
            Assert.Throws<ArgumentException>(() => Email.ParseList(seis));
        }

        [Test]
        public void TryCreate_Y_TryParseListOrEmpty_NoLanzan()
        {
            Assert.Multiple(() =>
            {
                Assert.That(Email.TryCreate("ok@example.com", out var ok), Is.True);
                Assert.That(ok!.Value, Is.EqualTo("ok@example.com"));

                Assert.That(Email.TryCreate("malo@@example.com", out var bad), Is.False);
                Assert.That(bad, Is.Null);

                Assert.That(Email.TryParseListOrEmpty("a@x.com; b@y.com", out var list), Is.True);
                Assert.That(list.Count, Is.EqualTo(2));
            });
        }
    }
}
