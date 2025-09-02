using NUnit.Framework;
using SharedKernel.ValueObjects;
using System;

namespace SharedKernel.Tests.ValueObjects
{
    [TestFixture]
    public class DomicilioFiscalTests
    {
        [Test]
        public void FromPeru_Completo_Valido()
        {
            var d = DomicilioFiscal.FromPeru(
                linea: "Av. Los Olivos 123 Int. 201",
                ubigeo: "150101",
                departamento: "LIMA",
                provincia: "LIMA",
                distrito: "LIMA",
                addressTypeCode: "0000"
            );

            Assert.That(d.EsPeru, Is.True);
            Assert.That(d.TieneLinea, Is.True);
            Assert.That(d.TieneUbigeo, Is.True);
            Assert.That(d.TieneDivisionTerritorialCompleta, Is.True);
            Assert.That(d.AddressTypeCode, Is.EqualTo("0000"));
            Assert.That(d.DebeSerializarRegistrationAddress, Is.True);
        }

        [Test]
        public void FromPeru_SoloLinea_ValidoYSerializa()
        {
            var d = DomicilioFiscal.FromPeru(linea: "Jr. Uno 456");

            Assert.That(d.TieneLinea, Is.True);
            Assert.That(d.TieneUbigeo, Is.False);
            Assert.That(d.TieneDivisionTerritorialCompleta, Is.False);
            Assert.That(d.DebeSerializarRegistrationAddress, Is.True);
        }

        [Test]
        public void FromPeru_SinNingunDato_NoSerializaRegistrationAddress()
        {
            var d = DomicilioFiscal.FromPeru();

            Assert.That(d.TieneLinea, Is.False);
            Assert.That(d.TieneUbigeo, Is.False);
            Assert.That(d.TieneDivisionTerritorialCompleta, Is.False);
            Assert.That(d.AddressTypeCode, Is.Null);
            Assert.That(d.DebeSerializarRegistrationAddress, Is.False);
        }

        [Test]
        public void FromPeru_LineaConTab_LanzaExcepcion()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                DomicilioFiscal.FromPeru(linea: "Calle\t123"));
            Assert.That(ex!.ParamName, Is.EqualTo("linea"));
        }

        [Test]
        public void FromPeru_LineaCorta_LanzaExcepcion()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                DomicilioFiscal.FromPeru(linea: "ab"));
            Assert.That(ex!.ParamName, Is.EqualTo("linea"));
        }

        [Test]
        public void FromPeru_DivisionParcial_LanzaExcepcion()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                DomicilioFiscal.FromPeru(
                    linea: "Av. A 123",
                    departamento: "LIMA",
                    provincia: "LIMA",
                    distrito: ""   // falta distrito
                ));
            Assert.That(ex!.ParamName, Is.EqualTo("distrito"));
        }

        [Test]
        public void FromPeru_UbigeoInvalido_LanzaExcepcion()
        {
            var ex1 = Assert.Throws<ArgumentOutOfRangeException>(() =>
                DomicilioFiscal.FromPeru(linea: "Av. A 123", ubigeo: "15010X"));
            Assert.That(ex1!.ParamName, Is.EqualTo("ubigeo"));

            var ex2 = Assert.Throws<ArgumentOutOfRangeException>(() =>
                DomicilioFiscal.FromPeru(linea: "Av. A 123", ubigeo: "000000"));
            Assert.That(ex2!.ParamName, Is.EqualTo("ubigeo"));
        }

        [Test]
        public void FromPeru_AddressTypeCodeInvalido_LanzaExcepcion()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                DomicilioFiscal.FromPeru(linea: "Av. A 123", addressTypeCode: "12"));
            Assert.That(ex!.ParamName, Is.EqualTo("addressTypeCode"));
        }

        [Test]
        public void From_NoPeru_LimpiaCamposPeru()
        {
            var d = DomicilioFiscal.From(
                paisCodigoIso: "US",
                linea: "123 Any St",
                ubigeo: "150101",
                departamento: "LIMA",
                provincia: "LIMA",
                distrito: "LIMA",
                addressTypeCode: "0000"
            );

            Assert.That(d.EsPeru, Is.False);
            Assert.That(d.Ubigeo, Is.Null);
            Assert.That(d.Departamento, Is.Null);
            Assert.That(d.Provincia, Is.Null);
            Assert.That(d.Distrito, Is.Null);
            Assert.That(d.AddressTypeCode, Is.Null);
            Assert.That(d.DebeSerializarRegistrationAddress, Is.True); // por línea
        }

        [Test]
        public void TryFromPeru_Valido_TrueYObjeto()
        {
            var ok = DomicilioFiscal.TryFromPeru(
                linea: "Av. B 456",
                ubigeo: "150101",
                departamento: "LIMA",
                provincia: "LIMA",
                distrito: "LIMA",
                addressTypeCode: "0000",
                out var d
            );

            Assert.That(ok, Is.True);
            Assert.That(d, Is.Not.Null);
            Assert.That(d!.Linea, Is.EqualTo("Av. B 456"));
        }

        [Test]
        public void Equality_PorValor()
        {
            var a = DomicilioFiscal.FromPeru("Av. C 789", "150101", "LIMA", "LIMA", "LIMA", "0000");
            var b = DomicilioFiscal.FromPeru("Av. C 789", "150101", "LIMA", "LIMA", "LIMA", "0000");

            Assert.That(a.Equals(b), Is.True);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }
    }
}
