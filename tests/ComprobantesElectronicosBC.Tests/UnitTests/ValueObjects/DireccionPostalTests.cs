using System;
using NUnit.Framework;
using ComprobantesElectronicosBC.Domain.ValueObjects;

namespace ComprobantesElectronicosBC.Tests.UnitTests.ValueObjects
{
    public class DireccionPostalTests
    {
        [Test]
        public void Create_RecortaCamposYValidaUbigeoYLinea()
        {
            var dir = DireccionPostal.Create(
                ubigeo: " 150101 ",
                direccion: "  Av. Perú 123  ",
                departamento: "  Lima ",
                provincia: " Lima ",
                distrito: "  San Miguel "
            );

            Assert.Multiple(() =>
            {
                Assert.That(dir.Ubigeo, Is.EqualTo("150101"));
                Assert.That(dir.Direccion, Is.EqualTo("Av. Perú 123"));
                Assert.That(dir.Departamento, Is.EqualTo("Lima"));
                Assert.That(dir.Provincia, Is.EqualTo("Lima"));
                Assert.That(dir.Distrito, Is.EqualTo("San Miguel"));
            });
        }

        [Test]
        public void Create_LanzaSiUbigeoNoTiene6Digitos()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                DireccionPostal.Create("15010", "Av. 1"));
            Assert.That(ex!.Message, Does.Contain("ubigeo").IgnoreCase);
        }

        [Test]
        public void Create_LanzaSiLineaVaciaOWhitespace()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                DireccionPostal.Create("150101", " "));
            Assert.That(ex!.Message, Does.Contain("dirección").IgnoreCase);
        }

        // ---- FromCliente ----
        [Test]
        public void FromCliente_Ruc20_RequiereDireccionRealYUbigeo()
        {
            // RUC válido: 20100070970 (SUNAT)
            var docRuc20 = DocumentoIdentidad.Create(tipo: "6", numero: "20100070970");

            var ex1 = Assert.Throws<ArgumentException>(() =>
                DireccionPostal.FromCliente(docRuc20, ubigeo: "150101", direccion: "-"));
            Assert.That(ex1!.Message, Does.Contain("dirección").IgnoreCase);

            var ex2 = Assert.Throws<ArgumentException>(() =>
                DireccionPostal.FromCliente(docRuc20, ubigeo: null, direccion: "Av. Perú 123"));
            Assert.That(ex2!.Message, Does.Contain("ubigeo").IgnoreCase);

            var ok = DireccionPostal.FromCliente(docRuc20, "150101", "Av. Perú 123", "Lima", "Lima", "San Miguel");
            Assert.Multiple(() =>
            {
                Assert.That(ok.Ubigeo, Is.EqualTo("150101"));
                Assert.That(ok.Direccion, Is.EqualTo("Av. Perú 123"));
            });
        }

        [Test]
        public void FromCliente_Ruc10_PermiteGuionAutocompletado()
        {
            // RUC válido: 10467793549 (ejemplo válido)
            var docRuc10 = DocumentoIdentidad.Create("6", "10467793549");
            var dir = DireccionPostal.FromCliente(docRuc10, ubigeo: null, direccion: null);

            Assert.Multiple(() =>
            {
                Assert.That(dir.Ubigeo, Is.EqualTo(string.Empty));
                Assert.That(dir.Direccion, Is.EqualTo("-"));
                Assert.That(dir.Departamento, Is.Null);
                Assert.That(dir.Provincia, Is.Null);
                Assert.That(dir.Distrito, Is.Null);
            });
        }

        [Test]
        public void FromCliente_Dni_PermiteGuionAutocompletado()
        {
            var docDni = DocumentoIdentidad.Create("1", "87654321");
            var dir = DireccionPostal.FromCliente(docDni, ubigeo: null, direccion: null);

            Assert.That(dir.Direccion, Is.EqualTo("-"));
            Assert.That(dir.Ubigeo, Is.EqualTo(string.Empty));
        }

        [Test]
        public void Equality_MismoContenido_EsIgual()
        {
            var a = DireccionPostal.Create("150101", "Av. Perú 123", "Lima", "Lima", "San Miguel");
            var b = DireccionPostal.Create("150101", "Av. Perú 123", "Lima", "Lima", "San Miguel");
            Assert.That(a, Is.EqualTo(b));
        }

        [Test]
        public void ToString_RetornaLinea()
        {
            var dir = DireccionPostal.Create("150101", "Av. Perú 123");
            Assert.That(dir.ToString(), Is.EqualTo("Av. Perú 123"));
        }
    }
}
