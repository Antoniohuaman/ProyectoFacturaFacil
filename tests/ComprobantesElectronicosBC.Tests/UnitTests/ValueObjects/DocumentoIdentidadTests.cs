using System;
using NUnit.Framework;
using ComprobantesElectronicosBC.Domain.ValueObjects;

namespace ComprobantesElectronicosBC.Tests.UnitTests.ValueObjects
{
    public class DocumentoIdentidadTests
    {
        [Test]
        public void CreateRuc_Ok_NormalizaYClasifica()
        {
            var doc = DocumentoIdentidad.CreateRuc(" 20-1000 70970 ");
            Assert.Multiple(() =>
            {
                Assert.That(doc.Tipo, Is.EqualTo(DocumentoIdentidad.TipoRuc));
                Assert.That(doc.Numero, Is.EqualTo("20100070970")); // solo dígitos
                Assert.That(doc.EsRuc, Is.True);
                Assert.That(doc.EsRuc20, Is.True);
                Assert.That(doc.EsRuc10, Is.False);
                Assert.That(doc.SchemeId, Is.EqualTo("6"));
                Assert.That(doc.ToString(), Is.EqualTo("RUC 20100070970"));
            });
        }

        [Test]
        public void CreateRuc_LargoInvalido_LanzaArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() => DocumentoIdentidad.CreateRuc("123"));
            Assert.That(ex!.ParamName, Is.EqualTo("ruc"));
            Assert.That(ex.Message, Does.Contain("11 dígitos").IgnoreCase);
        }

        [Test]
        public void CreateRuc_DVInvalido_LanzaArgumentException()
        {
            // mismo largo pero DV incorrecto
            var ex = Assert.Throws<ArgumentException>(() => DocumentoIdentidad.CreateRuc("20100070971"));
            Assert.That(ex!.ParamName, Is.EqualTo("ruc"));
            Assert.That(ex.Message, Does.Contain("inválido").IgnoreCase);
        }

        [Test]
        public void CreateDni_Ok_Normaliza()
        {
            var doc = DocumentoIdentidad.CreateDni(" 8765-4321 ");
            Assert.Multiple(() =>
            {
                Assert.That(doc.Tipo, Is.EqualTo(DocumentoIdentidad.TipoDni));
                Assert.That(doc.Numero, Is.EqualTo("87654321"));
                Assert.That(doc.EsDni, Is.True);
                Assert.That(doc.ToString(), Is.EqualTo("DNI 87654321"));
                Assert.That(doc.SchemeId, Is.EqualTo("1"));
            });
        }

        [Test]
        public void CreateDni_LargoInvalido_LanzaArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() => DocumentoIdentidad.CreateDni("1234567"));
            Assert.That(ex!.ParamName, Is.EqualTo("dni"));
            Assert.That(ex.Message, Does.Contain("8 dígitos").IgnoreCase);
        }

        [Test]
        public void Create_TipoNoSoportado_LanzaArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() => DocumentoIdentidad.Create("4", "123"));
            Assert.That(ex!.ParamName, Is.EqualTo("tipo"));
            Assert.That(ex.Message, Does.Contain("no soportado").IgnoreCase);
        }

        [Test]
        public void FromNumeroDetectandoTipo_RucYDni_Ok()
        {
            var ruc = DocumentoIdentidad.FromNumeroDetectandoTipo("20100070970");
            var dni = DocumentoIdentidad.FromNumeroDetectandoTipo("87654321");
            Assert.Multiple(() =>
            {
                Assert.That(ruc.EsRuc, Is.True);
                Assert.That(dni.EsDni, Is.True);
            });
        }

        [Test]
        public void FromNumeroDetectandoTipo_LargoNoValido_LanzaArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() => DocumentoIdentidad.FromNumeroDetectandoTipo("12345"));
            Assert.That(ex!.ParamName, Is.EqualTo("numero"));
            Assert.That(ex.Message, Does.Contain("11 (RUC) o 8 (DNI)").IgnoreCase);
        }

        [Test]
        public void TryCreate_DevuelveTrueFalseSegunValidez()
        {
            var ok = DocumentoIdentidad.TryCreate("6", "20100070970", out var docOk);
            var fail = DocumentoIdentidad.TryCreate("6", "20100070971", out var docFail);
            Assert.Multiple(() =>
            {
                Assert.That(ok, Is.True);
                Assert.That(docOk, Is.Not.Null);
                Assert.That(fail, Is.False);
                Assert.That(docFail, Is.Null);
            });
        }

        [Test]
        public void TryFromNumeroDetectandoTipo_DevuelveTrueFalseSegunValidez()
        {
            var ok = DocumentoIdentidad.TryFromNumeroDetectandoTipo("87654321", out var docOk);
            var fail = DocumentoIdentidad.TryFromNumeroDetectandoTipo("999", out var docFail);
            Assert.Multiple(() =>
            {
                Assert.That(ok, Is.True);
                Assert.That(docOk!.EsDni, Is.True);
                Assert.That(fail, Is.False);
                Assert.That(docFail, Is.Null);
            });
        }
    }
}
