using System;
using NUnit.Framework;
using ConfiguracionSistemaBC.Domain.ValueObjects;

namespace ConfiguracionSistemaBC.Tests.UnitTests.ValueObjects
{
    [TestFixture]
    public class EmailEmpresaTests
    {
        [Test]
        public void From_Valido_NormalizaMinusculas_Y_SetVisibilidad()
        {
            var e1 = EmailEmpresa.From("Ventas@MiDominio.COM"); // visible por defecto
            Assert.That(e1.Direccion, Is.EqualTo("ventas@midominio.com"));
            Assert.That(e1.Dominio, Is.EqualTo("midominio.com"));
            Assert.That(e1.EsVisible, Is.True);

            var e2 = EmailEmpresa.From("Contabilidad@empresa-peru.com.pe", esVisible: false);
            Assert.That(e2.Direccion, Is.EqualTo("contabilidad@empresa-peru.com.pe"));
            Assert.That(e2.Dominio, Is.EqualTo("empresa-peru.com.pe"));
            Assert.That(e2.EsVisible, Is.False);
        }

        [Test]
        public void From_LocalConSimbolosPermitidos_EsValido()
        {
            var e = EmailEmpresa.From("nombre.apellido+tag-test/ok!_ok@sub-domain.empresa.co");
            Assert.That(e.Direccion, Is.EqualTo("nombre.apellido+tag-test/ok!_ok@sub-domain.empresa.co"));
            Assert.That(e.Dominio, Is.EqualTo("sub-domain.empresa.co"));
        }

        [Test]
        public void From_LocalNoPuedeEmpezarONiTerminarConPunto_NiTenerPuntosConsecutivos()
        {
            Assert.That(() => EmailEmpresa.From(".abc@dominio.com"), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => EmailEmpresa.From("abc.@dominio.com"), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => EmailEmpresa.From("a..b@dominio.com"), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void From_DominioDebeTenerAlMenosUnPunto_Y_TLDSoloLetras()
        {
            Assert.That(() => EmailEmpresa.From("user@dominio"), Throws.TypeOf<ArgumentOutOfRangeException>());

            // TLD de 1 char -> inválido
            Assert.That(() => EmailEmpresa.From("user@dominio.c"), Throws.TypeOf<ArgumentOutOfRangeException>());

            // TLD con dígito -> inválido
            Assert.That(() => EmailEmpresa.From("user@dominio.com1"), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void From_DominioLabels_SinGuionAlInicioFin_Y_LongitudValida()
        {
            Assert.That(() => EmailEmpresa.From("user@-dominio.com"), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => EmailEmpresa.From("user@dominio-.com"), Throws.TypeOf<ArgumentOutOfRangeException>());

            // Label de 64 chars -> inválido (máx 63)
            var label64 = new string('a', 64);
            Assert.That(() => EmailEmpresa.From($"user@{label64}.com"), Throws.TypeOf<ArgumentOutOfRangeException>());

            // Label de 63 chars -> válido
            var label63 = new string('a', 63);
            var ok = EmailEmpresa.From($"user@{label63}.com");
            Assert.That(ok.Dominio, Is.EqualTo($"{label63}.com"));
        }

        [Test]
        public void From_NoSePermitenEspacios_Y_LongitudTotalMax254()
        {
            Assert.That(() => EmailEmpresa.From("user name@dominio.com"), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => EmailEmpresa.From("user@dominio .com"), Throws.TypeOf<ArgumentOutOfRangeException>());

            // Construir un correo > 254 chars
            var local = new string('a', 64);
            var midLabel = new string('b', 120);
            var tld = "com";
            var demasiadoLargo = $"{local}@{midLabel}.{midLabel}.{tld}";

            Assert.That(demasiadoLargo.Length, Is.GreaterThan(254));
            Assert.That(() => EmailEmpresa.From(demasiadoLargo), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void From_LocalMax64_SiExcedeEsInvalido()
        {
            var local65 = new string('x', 65);
            Assert.That(() => EmailEmpresa.From($"{local65}@dominio.com"), Throws.TypeOf<ArgumentOutOfRangeException>());

            var local64 = new string('y', 64);
            var ok = EmailEmpresa.From($"{local64}@dominio.com");
            Assert.That(ok.Direccion, Is.EqualTo($"{local64}@dominio.com"));
        }

        [Test]
        public void TryFrom_ComportamientoEsperado()
        {
            Assert.That(EmailEmpresa.TryFrom("ventas@midominio.com", out var e1), Is.True);
            Assert.That(e1!.Direccion, Is.EqualTo("ventas@midominio.com"));
            Assert.That(e1.EsVisible, Is.True);

            Assert.That(EmailEmpresa.TryFrom("ventas@midominio.com", out var e2, esVisible: false), Is.True);
            Assert.That(e2!.EsVisible, Is.False);

            Assert.That(EmailEmpresa.TryFrom("in valido@dominio.com", out _), Is.False);
            Assert.That(EmailEmpresa.TryFrom("user@dominio", out _), Is.False);
            Assert.That(EmailEmpresa.TryFrom(null, out _), Is.False);
            Assert.That(EmailEmpresa.TryFrom("", out _), Is.False);
        }

        [Test]
        public void ConVisibilidad_DevuelveNuevaInstanciaConMismaDireccionYCambiaFlag()
        {
            var e1 = EmailEmpresa.From("contabilidad@empresa.com", esVisible: false);
            var e2 = e1.ConVisibilidad(true);

            Assert.That(e1.Direccion, Is.EqualTo(e2.Direccion));
            Assert.That(e1.EsVisible, Is.False);
            Assert.That(e2.EsVisible, Is.True);
            Assert.That(e1, Is.Not.EqualTo(e2)); // igualdad incluye visibilidad
        }

        [Test]
        public void IgualdadPorValor_IncluyeDireccionCanonicaYVisibilidad()
        {
            var a = EmailEmpresa.From("Soporte@Empresa.COM", esVisible: true);
            var b = EmailEmpresa.From("soporte@empresa.com", esVisible: true);
            var c = EmailEmpresa.From("soporte@empresa.com", esVisible: false);

            Assert.That(a.Equals(b), Is.True);
            Assert.That(a == b, Is.True);
            Assert.That(a != b, Is.False);
            Assert.That(a.Equals(c), Is.False);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void ToString_E_ImplícitoYExplícito()
        {
            var e = EmailEmpresa.From("ventas@empresa.com", esVisible: false);

            Assert.That(e.ToString(), Is.EqualTo("ventas@empresa.com"));

            string s = e; // implícito a string
            Assert.That(s, Is.EqualTo("ventas@empresa.com"));

            var e2 = (EmailEmpresa)"ADMIN@EMPRESA.COM"; // explícito desde string, visible:true por defecto
            Assert.That(e2.Direccion, Is.EqualTo("admin@empresa.com"));
            Assert.That(e2.EsVisible, Is.True);
        }
    }
}