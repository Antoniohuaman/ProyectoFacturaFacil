using System;
using NUnit.Framework;
using ConfiguracionSistemaBC.Domain.ValueObjects;

namespace ConfiguracionSistemaBC.Tests.UnitTests.ValueObjects
{
    [TestFixture]
    public class DireccionPostalTests
    {
        private const string UbigeoLimaCercado = "150101";

        [Test]
        public void From_Valido_CreaYConservaTextosTalCual()
        {
            var linea = "AV. GIRÁLDEZ NRO. 634   URB. CERCADO"; // espacios múltiples
            var dep   = "Lima";
            var prov  = "Lima";
            var dist  = "Lima";

            var dir = DireccionPostal.From(
                linea: linea,
                ubigeo: UbigeoLimaCercado,
                departamento: dep,
                provincia: prov,
                distrito: dist
                // paisCodigoIso y addressTypeCode por defecto
            );

            Assert.That(dir.PaisCodigoIso, Is.EqualTo("PE"));
            Assert.That(dir.AddressTypeCode, Is.EqualTo("0000"));
            Assert.That(dir.Ubigeo, Is.EqualTo(UbigeoLimaCercado));
            Assert.That(dir.Departamento, Is.EqualTo(dep));
            Assert.That(dir.Provincia, Is.EqualTo(prov));
            Assert.That(dir.Distrito, Is.EqualTo(dist));
            Assert.That(dir.Linea, Is.EqualTo(linea)); // preserva espacios y casing
        }

        [Test]
        public void From_Valido_ConCodigoPaisYAddressTypeCodeExplicitos()
        {
            var dir = DireccionPostal.From(
                linea: "MZA. A1 LOTE. 12 URB. CASUARINAS",
                ubigeo: "021809",
                departamento: "ANCASH",
                provincia: "SANTA",
                distrito: "NUEVO CHIMBOTE",
                paisCodigoIso: "PE",
                addressTypeCode: "0000"
            );

            Assert.That(dir.PaisCodigoIso, Is.EqualTo("PE"));
            Assert.That(dir.AddressTypeCode, Is.EqualTo("0000"));
            Assert.That(dir.Ubigeo, Is.EqualTo("021809"));
        }

        [Test]
        public void TryFrom_Valido_RegresaTrueYObjeto()
        {
            var ok = DireccionPostal.TryFrom(
                linea: "AV. GIRÁLDEZ NRO. 634 URB. CERCADO",
                ubigeo: UbigeoLimaCercado,
                departamento: "LIMA",
                provincia: "LIMA",
                distrito: "LIMA",
                out var dir
            );

            Assert.That(ok, Is.True);
            Assert.That(dir, Is.Not.Null);
            Assert.That(dir!.Ubigeo, Is.EqualTo(UbigeoLimaCercado));
            Assert.That(dir.PaisCodigoIso, Is.EqualTo("PE"));
            Assert.That(dir.AddressTypeCode, Is.EqualTo("0000"));
        }

        [Test]
        public void From_PaisInvalido_LanzaOutOfRange()
        {
            Assert.That(() => DireccionPostal.From(
                linea: "Av. Siempre Viva 742",
                ubigeo: UbigeoLimaCercado,
                departamento: "LIMA",
                provincia: "LIMA",
                distrito: "LIMA",
                paisCodigoIso: "US" // inválido para contexto SUNAT
            ), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void From_UbigeoInvalido_LanzaOutOfRange()
        {
            // Longitud incorrecta
            Assert.That(() => DireccionPostal.From(
                linea: "C alle X 123",
                ubigeo: "15010", // 5
                departamento: "LIMA",
                provincia: "LIMA",
                distrito: "LIMA"
            ), Throws.TypeOf<ArgumentOutOfRangeException>());

            // Caracter no dígito
            Assert.That(() => DireccionPostal.From(
                linea: "C alle X 123",
                ubigeo: "15010A",
                departamento: "LIMA",
                provincia: "LIMA",
                distrito: "LIMA"
            ), Throws.TypeOf<ArgumentOutOfRangeException>());

            // 000000 no permitido
            Assert.That(() => DireccionPostal.From(
                linea: "C alle X 123",
                ubigeo: "000000",
                departamento: "LIMA",
                provincia: "LIMA",
                distrito: "LIMA"
            ), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void From_AddressTypeCodeInvalido_LanzaOutOfRange()
        {
            // Longitud != 4
            Assert.That(() => DireccionPostal.From(
                linea: "C alle X 123",
                ubigeo: UbigeoLimaCercado,
                departamento: "LIMA",
                provincia: "LIMA",
                distrito: "LIMA",
                addressTypeCode: "000"
            ), Throws.TypeOf<ArgumentOutOfRangeException>());

            // Con letras
            Assert.That(() => DireccionPostal.From(
                linea: "C alle X 123",
                ubigeo: UbigeoLimaCercado,
                departamento: "LIMA",
                provincia: "LIMA",
                distrito: "LIMA",
                addressTypeCode: "00A0"
            ), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void From_TextosObligatorios_Invalidos_LanzaOutOfRange()
        {
            // Solo espacios
            Assert.That(() => DireccionPostal.From(
                linea: "   ",
                ubigeo: UbigeoLimaCercado,
                departamento: "LIMA",
                provincia: "LIMA",
                distrito: "LIMA"
            ), Throws.TypeOf<ArgumentOutOfRangeException>());

            Assert.That(() => DireccionPostal.From(
                linea: "Av. X 123",
                ubigeo: UbigeoLimaCercado,
                departamento: "   ",
                provincia: "LIMA",
                distrito: "LIMA"
            ), Throws.TypeOf<ArgumentOutOfRangeException>());

            Assert.That(() => DireccionPostal.From(
                linea: "Av. X 123",
                ubigeo: UbigeoLimaCercado,
                departamento: "LIMA",
                provincia: "   ",
                distrito: "LIMA"
            ), Throws.TypeOf<ArgumentOutOfRangeException>());

            Assert.That(() => DireccionPostal.From(
                linea: "Av. X 123",
                ubigeo: UbigeoLimaCercado,
                departamento: "LIMA",
                provincia: "LIMA",
                distrito: "   "
            ), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void From_TextosExcedenLongitud_LanzaOutOfRange()
        {
            var largo81 = new string('X', 81);
            var largo241 = new string('Y', 241);

            // Departamento > 80
            Assert.That(() => DireccionPostal.From(
                linea: "Av. X 123",
                ubigeo: UbigeoLimaCercado,
                departamento: largo81,
                provincia: "LIMA",
                distrito: "LIMA"
            ), Throws.TypeOf<ArgumentOutOfRangeException>());

            // Provincia > 80
            Assert.That(() => DireccionPostal.From(
                linea: "Av. X 123",
                ubigeo: UbigeoLimaCercado,
                departamento: "LIMA",
                provincia: largo81,
                distrito: "LIMA"
            ), Throws.TypeOf<ArgumentOutOfRangeException>());

            // Distrito > 80
            Assert.That(() => DireccionPostal.From(
                linea: "Av. X 123",
                ubigeo: UbigeoLimaCercado,
                departamento: "LIMA",
                provincia: "LIMA",
                distrito: largo81
            ), Throws.TypeOf<ArgumentOutOfRangeException>());

            // Línea > 240
            Assert.That(() => DireccionPostal.From(
                linea: largo241,
                ubigeo: UbigeoLimaCercado,
                departamento: "LIMA",
                provincia: "LIMA",
                distrito: "LIMA"
            ), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void TryFrom_Invalidos_RegresaFalse()
        {
            Assert.That(DireccionPostal.TryFrom(
                linea: "Av. X 123",
                ubigeo: "15010A",
                departamento: "LIMA",
                provincia: "LIMA",
                distrito: "LIMA",
                out _), Is.False);

            Assert.That(DireccionPostal.TryFrom(
                linea: "   ",
                ubigeo: UbigeoLimaCercado,
                departamento: "LIMA",
                provincia: "LIMA",
                distrito: "LIMA",
                out _), Is.False);

            Assert.That(DireccionPostal.TryFrom(
                linea: "Av. X 123",
                ubigeo: UbigeoLimaCercado,
                departamento: new string('X', 81),
                provincia: "LIMA",
                distrito: "LIMA",
                out _), Is.False);

            Assert.That(DireccionPostal.TryFrom(
                linea: new string('Y', 241),
                ubigeo: UbigeoLimaCercado,
                departamento: "LIMA",
                provincia: "LIMA",
                distrito: "LIMA",
                out _), Is.False);
        }

        [Test]
        public void IgualdadPorValor_OperadoresYHashCode()
        {
            var a = DireccionPostal.From(
                linea: "AV. GIRÁLDEZ NRO. 634 URB. CERCADO",
                ubigeo: "120101",
                departamento: "JUNIN",
                provincia: "HUANCAYO",
                distrito: "HUANCAYO",
                addressTypeCode: "0000",
                paisCodigoIso: "PE"
            );

            var b = DireccionPostal.From(
                linea: "AV. GIRÁLDEZ NRO. 634 URB. CERCADO",
                ubigeo: "120101",
                departamento: "JUNIN",
                provincia: "HUANCAYO",
                distrito: "HUANCAYO",
                addressTypeCode: "0000",
                paisCodigoIso: "PE"
            );

            var c = DireccionPostal.From(
                linea: "AV. DIFERENTE 123",
                ubigeo: "120101",
                departamento: "JUNIN",
                provincia: "HUANCAYO",
                distrito: "HUANCAYO"
            );

            Assert.That(a.Equals(b), Is.True);
            Assert.That(a == b, Is.True);
            Assert.That(a != b, Is.False);
            Assert.That(a.Equals(c), Is.False);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void ToString_FormatoContienePartesClaves()
        {
            var dir = DireccionPostal.From(
                linea: "AV. GIRÁLDEZ NRO. 634 URB. CERCADO",
                ubigeo: UbigeoLimaCercado,
                departamento: "LIMA",
                provincia: "LIMA",
                distrito: "LIMA"
            );

            var s = dir.ToString();
            Assert.That(s, Does.Contain("AV. GIRÁLDEZ NRO. 634 URB. CERCADO"));
            Assert.That(s, Does.Contain("LIMA - LIMA - LIMA"));
            Assert.That(s, Does.Contain("(" + UbigeoLimaCercado + ")"));
            Assert.That(s, Does.Contain("PE"));
        }
    }
}