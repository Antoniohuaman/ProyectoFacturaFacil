using System;
using NUnit.Framework;
using SharedKernel.ValueObjects;
using System.Collections.Generic;

namespace SharedKernel.Tests.ValueObjects
{
    [TestFixture]
    public class EmailTests
    {
        [Test]
        public void IdentificarEmailInvalidoQueNoFalla()
        {
            var casos = new[] {
                "usuario@",
                "@dominio.com",
                "usuario@dominio",
                "usuario@dominio.c",
                "usuario@@dominio.com",
                "usuario dominio.com"
            };
            foreach (var caso in casos)
            {
                try
                {
                    Email.Create(caso);
                    TestContext.WriteLine($"NO FALLA: '{caso}'");
                }
                catch (ArgumentException)
                {
                    // OK, lanza excepción
                }
            }
            Assert.Pass("Verifica la salida en la consola de test para ver cuál no falla");
        }
        [Test]
        public void CrearEmail_Valido_NoLanzaExcepcion()
        {
            var email = Email.Create("usuario@dominio.com");
            Assert.That(email.Value, Is.EqualTo("usuario@dominio.com"));
            Assert.That(email.LocalPart, Is.EqualTo("usuario"));
            Assert.That(email.Domain, Is.EqualTo("dominio.com"));
        }

        [TestCase("usuario@dominio.com")]
        [TestCase("USUARIO@DOMINIO.COM")]
        [TestCase("usuario@dominio.com.pe")]
        [TestCase("usuario@sub.dominio.com")]
        [TestCase("usuario@xn--exmple-cua.com")] // IDN punycode
        public void CrearEmail_Valido_Variantes(string input)
        {
            var email = Email.Create(input);
            Assert.That(email.Value.Contains("@"));
        }

        [Test]
        public void CrearEmail_ConEspaciosYEnvolturas_Normaliza()
        {
            var email = Email.Create("  <usuario@dominio.com>  ");
            Assert.That(email.Value, Is.EqualTo("usuario@dominio.com"));
        }

        [Test]
        public void CrearEmail_Vacio_LanzaExcepcion()
        {
            Assert.Throws<ArgumentException>(() => Email.Create(""));
            Assert.Throws<ArgumentException>(() => Email.Create("   "));
        }

        [Test]
        public void CrearEmail_FormatoInvalido_LanzaExcepcion()
        {
            Assert.Throws<ArgumentException>(() => Email.Create("usuario@"));
            Assert.Throws<ArgumentException>(() => Email.Create("@dominio.com"));
            Assert.Throws<ArgumentException>(() => Email.Create("usuario@dominio")); // TLD < 2
            Assert.Throws<ArgumentException>(() => Email.Create("usuario@dominio.c")); // TLD < 2
            Assert.Throws<ArgumentException>(() => Email.Create("usuario@@dominio.com"));
            Assert.Throws<ArgumentException>(() => Email.Create("usuario dominio.com"));
        }

        [Test]
        public void CrearEmail_ConNombreParaMostrar_LanzaExcepcion()
        {
            Assert.Throws<ArgumentException>(() => Email.Create("Usuario <usuario@dominio.com>"));
        }

        [Test]
        public void CrearEmail_LocalMuyLargo_LanzaExcepcion()
        {
            var local = new string('a', 65);
            Assert.Throws<ArgumentException>(() => Email.Create($"{local}@dominio.com"));
        }

        [Test]
        public void CrearEmail_DominioMuyLargo_LanzaExcepcion()
        {
            var domain = new string('a', 256);
            Assert.Throws<ArgumentException>(() => Email.Create($"usuario@{domain}"));
        }

        [Test]
        public void CrearEmail_TotalMuyLargo_LanzaExcepcion()
        {
            var local = new string('a', 64);
            var domain = new string('b', 190) + ".com";
            Assert.Throws<ArgumentException>(() => Email.Create($"{local}@{domain}"));
        }

        [Test]
        public void CrearEmail_DominioConEtiquetaInvalida_LanzaExcepcion()
        {
            Assert.Throws<ArgumentException>(() => Email.Create("usuario@-dominio.com"));
            Assert.Throws<ArgumentException>(() => Email.Create("usuario@dominio-.com"));
            Assert.Throws<ArgumentException>(() => Email.Create("usuario@dominio..com"));
            Assert.Throws<ArgumentException>(() => Email.Create("usuario@dominio.c_m"));
        }

        [Test]
        public void TryCreateEmail_Valido_DevuelveTrueYObjeto()
        {
            var result = Email.TryCreate("usuario@dominio.com", out var email);
            Assert.That(result, Is.True);
            Assert.That(email, Is.Not.Null);
            Assert.That(email!.Value, Is.EqualTo("usuario@dominio.com"));
        }

        [Test]
        public void TryCreateEmail_Invalido_DevuelveFalseYNull()
        {
            var result = Email.TryCreate("usuario@", out var email);
            Assert.That(result, Is.False);
            Assert.That(email, Is.Null);
        }

        [Test]
        public void ParseList_Obligatoria_Valida()
        {
            var lista = Email.ParseList("a@b.com, b@c.com; c@d.com");
            Assert.That(lista.Count, Is.EqualTo(3));
            Assert.That(lista[0].Value, Is.EqualTo("a@b.com"));
            Assert.That(lista[1].Value, Is.EqualTo("b@c.com"));
            Assert.That(lista[2].Value, Is.EqualTo("c@d.com"));
        }

        [Test]
        public void ParseList_Obligatoria_Vacia_LanzaExcepcion()
        {
            Assert.Throws<ArgumentException>(() => Email.ParseList("   "));
        }

        [Test]
        public void ParseListOrEmpty_Valida()
        {
            var lista = Email.ParseListOrEmpty("a@b.com; b@c.com");
            Assert.That(lista.Count, Is.EqualTo(2));
        }

        [Test]
        public void ParseListOrEmpty_Vacia_DevuelveListaVacia()
        {
            var lista = Email.ParseListOrEmpty("");
            Assert.That(lista, Is.Empty);
        }

        [Test]
        public void ParseList_MaxDestinatarios_LanzaExcepcion()
        {
            var emails = string.Join(",", new[]{"a@b.com","b@c.com","c@d.com","d@e.com","e@f.com","f@g.com"});
            Assert.Throws<ArgumentException>(() => Email.ParseList(emails));
        }

        [Test]
        public void TryParseListOrEmpty_Valida()
        {
            var result = Email.TryParseListOrEmpty("a@b.com, b@c.com", out var lista);
            Assert.That(result, Is.True);
            Assert.That(lista.Count, Is.EqualTo(2));
        }

        [Test]
        public void TryParseListOrEmpty_Invalida_DevuelveFalseYListaVacia()
        {
            var result = Email.TryParseListOrEmpty("a@b.com, b@c", out var lista);
            Assert.That(result, Is.False);
            Assert.That(lista, Is.Empty);
        }

        [Test]
        public void IgualdadPorValor()
        {
            var e1 = Email.Create("usuario@dominio.com");
            var e2 = Email.Create("usuario@dominio.com");
            Assert.That(e1, Is.EqualTo(e2));
            Assert.That(e1.GetHashCode(), Is.EqualTo(e2.GetHashCode()));
        }
    }
}
