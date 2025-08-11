using System;
using NUnit.Framework;
using ConfiguracionSistemaBC.Domain.ValueObjects;

namespace ConfiguracionSistemaBC.Tests.UnitTests.ValueObjects
{
    [TestFixture]
    public class PieDePaginaTests
    {
        [Test]
        public void FromHtml_NullOBlanco_RegresaVacio()
        {
            var a = PieDePagina.FromHtml(null);
            var b = PieDePagina.FromHtml("   ");
            Assert.That(a, Is.SameAs(PieDePagina.Vacio));
            Assert.That(b, Is.SameAs(PieDePagina.Vacio));
            Assert.That(a.EsVacio, Is.True);
            Assert.That(a.Html, Is.EqualTo(string.Empty));
        }

        [Test]
        public void FromHtml_LongitudExcede_Maximo_Lanza()
        {
            var html = new string('x', PieDePagina.MaxLongitudHtml + 1);
            Assert.That(() => PieDePagina.FromHtml(html), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void FromHtml_EliminaEtiquetasPeligrosasYAtributosOn_YPreservasPermitidas()
        {
            var htmlInseguro = @"
                <style>body{display:none}</style>
                <script>alert(1)</script>
                <p onclick=""hack()"">Hola <strong>Mundo</strong></p>
                <a href=""javascript:alert(1)"" target=""_blank"">Click</a>
                <a href=""https://seguro.com"" target=""_blank"">Seguro</a>
                <div>texto en div</div>
            ";

            var pie = PieDePagina.FromHtml(htmlInseguro);
            var s = pie.Html;

            // Quitó style/script y 'onclick'
            Assert.That(s, Does.Not.Contain("<style"));
            Assert.That(s, Does.Not.Contain("<script"));
            Assert.That(s, Does.Not.Contain("onclick"));

            // Permitidas <p>, <strong>, <a>
            Assert.That(s, Does.Contain("<p>Hola <strong>Mundo</strong></p>"));

            // Enlace inseguro sin href javascript:
            Assert.That(s, Does.Not.Contain("javascript:"));
            // Mantiene tag <a> pero sin href peligroso (puede tener atributos extra)
            Assert.That(s, Does.Contain("<a target=\"_blank\" rel=\"noopener noreferrer\">Click</a>"));

            // Enlace seguro conserva href y añade rel si target=_blank
            Assert.That(s, Does.Contain("href=\"https://seguro.com\""));
            Assert.That(s, Does.Contain("target=\"_blank\""));
            Assert.That(s, Does.Contain("rel=\"noopener noreferrer\""));

            // Etiquetas no permitidas (div) se quitan, pero conserva el contenido
            Assert.That(s, Does.Not.Contain("<div"));
            Assert.That(s, Does.Contain("texto en div"));
        }

        [Test]
        public void FromHtml_SiRelYaExiste_NoDuplicaRel()
        {
            var html = "<a href='https://ejemplo.com' target='_blank' rel='nofollow'>Link</a>";
            var pie = PieDePagina.FromHtml(html);
            var s = pie.Html;

            // No debe agregar un segundo 'rel'
            int countRel = s.Split(new[] { "rel=" }, StringSplitOptions.None).Length - 1;
            Assert.That(countRel, Is.EqualTo(1));
            // Puede estar con comillas simples o dobles
            Assert.That(s.Contains("rel=\"nofollow\"") || s.Contains("rel='nofollow'"), "El atributo rel debe estar presente con comillas simples o dobles");
        }

        [Test]
        public void FromTextoPlano_EncodeaHtml_Y_ConvierteSaltosABr()
        {
            var texto = "Hola <b>mundo</b>\nLinea 2 & extra";
            var pie = PieDePagina.FromTextoPlano(texto);

            // Debe estar encodeado, no como tag real
            Assert.That(pie.Html, Does.Contain("&lt;b&gt;mundo&lt;/b&gt;"));

            // Salto de línea → <br>
            Assert.That(pie.Html, Does.Contain("<br>Linea 2"));

            // Debe existir &amp; por el '&'
            Assert.That(pie.Html, Does.Contain("&amp;"));
        }

        [Test]
        public void TryFromHtml_ComportamientoEsperado()
        {
            Assert.That(PieDePagina.TryFromHtml(null, out var p1), Is.True);
            Assert.That(p1, Is.SameAs(PieDePagina.Vacio));

            var okHtml = "<p>OK</p>";
            Assert.That(PieDePagina.TryFromHtml(okHtml, out var p2), Is.True);
            Assert.That(p2!.Html, Is.EqualTo("<p>OK</p>"));

            var largo = new string('x', PieDePagina.MaxLongitudHtml + 1);
            Assert.That(PieDePagina.TryFromHtml(largo, out _), Is.False);
        }

        [Test]
        public void Actualizar_DevuelveNuevaInstancia_Saneada()
        {
            var p1 = PieDePagina.FromHtml("<p>Hola</p>");
            var p2 = p1.Actualizar("<p onclick='x'>Hola</p>");
            Assert.That(p2.Html, Is.EqualTo("<p>Hola</p>"));
            Assert.That(ReferenceEquals(p1, p2), Is.False); // inmutabilidad
        }

        [Test]
        public void TextoPlanoPreview_QuitaEtiquetasYTrunca()
        {
            var pie = PieDePagina.FromHtml("<p>Hola <strong>Mundo</strong> &amp; amigos</p>");
            var preview = pie.TextoPlanoPreview(10);

            // Sin etiquetas, con entidades decodificadas
            Assert.That(preview, Does.Not.Contain("<"));
            Assert.That(!preview.Contains("&")); // &amp; -> &
            // Truncado (10 chars)
            Assert.That(preview.Length, Is.LessThanOrEqualTo(10));
        }

        [Test]
        public void IgualdadPorValor_OperadoresYHashCode()
        {
            var a = PieDePagina.FromHtml("<p>Hola <strong>Mundo</strong></p>");
            var b = PieDePagina.FromHtml("<p onclick='x'>Hola <strong>Mundo</strong></p>"); // se sanea igual que a
            var c = PieDePagina.FromHtml("<p>Otro</p>");

            Assert.That(a.Equals(b), Is.True);
            Assert.That(a == b, Is.True);
            Assert.That(a != b, Is.False);
            Assert.That(a.Equals(c), Is.False);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void ToString_RetornaHtml()
        {
            var p = PieDePagina.FromHtml("<p>Footer</p>");
            Assert.That(p.ToString(), Is.EqualTo("<p>Footer</p>"));
        }
    }
}