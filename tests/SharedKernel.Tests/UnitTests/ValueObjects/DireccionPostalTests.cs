using System;
using NUnit.Framework;
using SharedKernel.ValueObjects;

namespace SharedKernel.Tests.UnitTests.ValueObjects
{
    [TestFixture]
    public class DireccionPostalTests
    {
        // -----------------------------
        // Creación válida
        // -----------------------------
        [Test]
        public void FromPeru_Minimo_LineaGuion_DefaultATC0000_OK()
        {
            var dir = DireccionPostal.FromPeru(linea: "-");

            Assert.Multiple(() =>
            {
                Assert.That(dir.EsPeru, Is.True);
                Assert.That(dir.Linea, Is.EqualTo("-"));
                Assert.That(dir.EsLineaGuion, Is.True);
                Assert.That(dir.Ubigeo, Is.EqualTo(string.Empty));
                Assert.That(dir.Departamento, Is.EqualTo(string.Empty));
                Assert.That(dir.Provincia, Is.EqualTo(string.Empty));
                Assert.That(dir.Distrito, Is.EqualTo(string.Empty));
                Assert.That(dir.AddressTypeCode, Is.EqualTo("0000"));
                Assert.That(dir.TieneUbigeo, Is.False);
                Assert.That(dir.TieneDivisionTerritorialCompleta, Is.False);
            });
        }

        [Test]
        public void FromPeru_Completo_ConUbigeoYDivision_OK()
        {
            var dir = DireccionPostal.FromPeru(
                linea: "Jr. Los Olivos 123",
                ubigeo: "150101",
                departamento: "Lima",
                provincia: "Lima",
                distrito: "Lima",
                addressTypeCode: "0001",
                urbanizacion: "San Isidro",
                referencia: "Frente al parque"
            );

            Assert.Multiple(() =>
            {
                Assert.That(dir.EsPeru, Is.True);
                Assert.That(dir.Ubigeo, Is.EqualTo("150101"));
                Assert.That(dir.Departamento, Is.EqualTo("Lima"));
                Assert.That(dir.Provincia, Is.EqualTo("Lima"));
                Assert.That(dir.Distrito, Is.EqualTo("Lima"));
                Assert.That(dir.AddressTypeCode, Is.EqualTo("0001"));
                Assert.That(dir.Urbanizacion, Is.EqualTo("San Isidro"));
                Assert.That(dir.Referencia, Is.EqualTo("Frente al parque"));
                Assert.That(dir.TieneUbigeo, Is.True);
                Assert.That(dir.TieneDivisionTerritorialCompleta, Is.True);
            });
        }

        [Test]
        public void From_Extranjero_IgnoraCamposPeru_OK()
        {
            var dir = DireccionPostal.From(
                paisCodigoIso: "US",
                linea: "221B Baker Street",
                ubigeo: "150101",
                departamento: "Lima",
                provincia: "Lima",
                distrito: "Lima",
                addressTypeCode: "0001"
            );

            Assert.Multiple(() =>
            {
                Assert.That(dir.EsPeru, Is.False);
                Assert.That(dir.Linea, Is.EqualTo("221B Baker Street"));
                Assert.That(dir.Ubigeo, Is.EqualTo(string.Empty));
                Assert.That(dir.Departamento, Is.EqualTo(string.Empty));
                Assert.That(dir.Provincia, Is.EqualTo(string.Empty));
                Assert.That(dir.Distrito, Is.EqualTo(string.Empty));
                Assert.That(dir.AddressTypeCode, Is.EqualTo(string.Empty));
            });
        }

        // -----------------------------
        // Validación de país
        // -----------------------------
        [TestCase(null)]
        [TestCase("")]
        [TestCase("P")]
        [TestCase("PER")]
        [TestCase("P3")]
        public void From_PaisCodigoIso_Invalido_Lanza(string? iso)
        {
            Assert.That(() => DireccionPostal.From(iso!, "Calle 1"),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [TestCase("pe")]
        [TestCase("Pe")]
        [TestCase("PE")]
        public void EsPeru_EsCaseInsensitive_OK(string iso)
        {
            var dir = DireccionPostal.From(iso, "Calle A");
            Assert.That(dir.EsPeru, Is.True);
        }

        // -----------------------------
        // Ubigeo (condicional)
        // -----------------------------
        [TestCase("12345")]
        [TestCase("1234567")]
        [TestCase("12345a")]
        [TestCase("000000")]
        public void FromPeru_UbigeoInformado_Invalido_Lanza(string ubigeo)
        {
            Assert.That(() => DireccionPostal.FromPeru(
                    linea: "Calle B",
                    ubigeo: ubigeo,
                    departamento: "Lima",
                    provincia: "Lima",
                    distrito: "Lima"),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void FromPeru_UbigeoOmitido_NoValidaUbigeo_OK()
        {
            var dir = DireccionPostal.FromPeru("Av. 28 de Julio");
            Assert.That(dir.Ubigeo, Is.EqualTo(string.Empty));
        }

        // -----------------------------
        // División territorial (conjunta)
        // -----------------------------
        [TestCase("Lima", "", "Lima")]
        [TestCase("", "Lima", "Lima")]
        [TestCase("Lima", "Lima", "")]
        public void FromPeru_AlgunoDeDivisionInformado_PeroNoTodos_Lanza(string dep, string prov, string dist)
        {
            Assert.That(() => DireccionPostal.FromPeru(
                    linea: "Calle C",
                    departamento: dep,
                    provincia: prov,
                    distrito: dist),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void FromPeru_DivisionCompletaSinUbigeo_OK()
        {
            var dir = DireccionPostal.FromPeru(
                linea: "Calle D",
                departamento: "Lima",
                provincia: "Lima",
                distrito: "Lima"
            );

            Assert.Multiple(() =>
            {
                Assert.That(dir.Ubigeo, Is.EqualTo(string.Empty));
                Assert.That(dir.TieneDivisionTerritorialCompleta, Is.True);
            });
        }

        // -----------------------------
        // AddressTypeCode (condicional en PE)
        // -----------------------------
        [TestCase("")]
        [TestCase("0000")]
        [TestCase("0123")]
        public void FromPeru_AddressTypeCode_Valido_OK(string code)
        {
            var dir = DireccionPostal.FromPeru(
                linea: "Calle E",
                addressTypeCode: code
            );

            var esperado = string.IsNullOrEmpty(code) ? "0000" : code;
            Assert.That(dir.AddressTypeCode, Is.EqualTo(esperado));
        }

        [TestCase("12")]
        [TestCase("abcd")]
        [TestCase("12345")]
        public void FromPeru_AddressTypeCode_Invalido_Lanza(string code)
        {
            Assert.That(() => DireccionPostal.FromPeru(
                    linea: "Calle F",
                    addressTypeCode: code),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        // -----------------------------
        // Longitudes y obligatoriedad
        // -----------------------------
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void From_Linea_Obligatoria_LanzaSiVaciaOBlancos(string? linea)
        {
            Assert.That(() => DireccionPostal.From("PE", linea!),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void From_Linea_MayorA240_Lanza()
        {
            var larga = new string('x', 241);
            Assert.That(() => DireccionPostal.From("PE", larga),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void FromPeru_Urbanizacion_MayorA80_Lanza()
        {
            var u = new string('u', 81);
            Assert.That(() => DireccionPostal.FromPeru("Calle G", urbanizacion: u),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void FromPeru_Referencia_MayorA120_Lanza()
        {
            var r = new string('r', 121);
            Assert.That(() => DireccionPostal.FromPeru("Calle H", referencia: r),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        // -----------------------------
        // TryFromPeru
        // -----------------------------
        [Test]
        public void TryFromPeru_Minimo_LineaSolo_TrueYObjetoValido()
        {
            var ok = DireccionPostal.TryFromPeru(
                linea: "Calle I",
                ubigeo: null,
                departamento: null,
                provincia: null,
                distrito: null,
                direccion: out var dir
            );

            Assert.Multiple(() =>
            {
                Assert.That(ok, Is.True);
                Assert.That(dir, Is.Not.Null);
                Assert.That(dir!.Linea, Is.EqualTo("Calle I"));
                Assert.That(dir.Ubigeo, Is.EqualTo(string.Empty));
                Assert.That(dir.TieneDivisionTerritorialCompleta, Is.False);
                Assert.That(dir.AddressTypeCode, Is.EqualTo("0000"));
            });
        }

        [Test]
        public void TryFromPeru_UbigeoInvalido_RetornaFalse()
        {
            var ok = DireccionPostal.TryFromPeru(
                linea: "Calle J",
                ubigeo: "12345a",
                departamento: "Lima",
                provincia: "Lima",
                distrito: "Lima",
                direccion: out var dir
            );

            Assert.Multiple(() =>
            {
                Assert.That(ok, Is.False);
                Assert.That(dir, Is.Null);
            });
        }

        [Test]
        public void TryFromPeru_DivisionIncompleta_RetornaFalse()
        {
            var ok = DireccionPostal.TryFromPeru(
                linea: "Calle K",
                ubigeo: null,
                departamento: "Lima",
                provincia: "",
                distrito: "Lima",
                direccion: out var dir
            );

            Assert.Multiple(() =>
            {
                Assert.That(ok, Is.False);
                Assert.That(dir, Is.Null);
            });
        }

        // -----------------------------
        // Igualdad y hash
        // -----------------------------
        [Test]
        public void Equals_MismosValores_TrueYHashIgual()
        {
            var a = DireccionPostal.FromPeru(
                linea: "Calle L",
                ubigeo: "150101",
                departamento: "Lima",
                provincia: "Lima",
                distrito: "Lima",
                addressTypeCode: "0002",
                urbanizacion: "Urb",
                referencia: "Ref"
            );

            var b = DireccionPostal.FromPeru(
                linea: "Calle L",
                ubigeo: "150101",
                departamento: "Lima",
                provincia: "Lima",
                distrito: "Lima",
                addressTypeCode: "0002",
                urbanizacion: "Urb",
                referencia: "Ref"
            );

            Assert.Multiple(() =>
            {
                Assert.That(a.Equals(b), Is.True);
                Assert.That(b.Equals(a), Is.True);
                Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
            });
        }

        [Test]
        public void Equals_ValoresDiferentes_False()
        {
            var a = DireccionPostal.FromPeru("Calle M");
            var b = DireccionPostal.FromPeru("Calle N");
            Assert.That(a.Equals(b), Is.False);
        }

        [Test]
        public void Equals_ContraNull_False()
        {
            var a = DireccionPostal.FromPeru("Calle O");
            Assert.That(a.Equals(null), Is.False);
        }

        // -----------------------------
        // ToString (formato limpio)
        // -----------------------------
        [Test]
        public void ToString_IncluyePaisYUbigeoCuandoExiste_SinParentesisVacios()
        {
            var a = DireccionPostal.FromPeru(
                linea: "Calle P",
                ubigeo: "150101",
                departamento: "Lima",
                provincia: "Lima",
                distrito: "Lima"
            );

            var s = a.ToString();

            Assert.Multiple(() =>
            {
                Assert.That(s, Does.Contain("Calle P"));
                Assert.That(s, Does.Contain("Lima"));
                Assert.That(s, Does.Contain("(150101)"));
                Assert.That(s, Does.Contain(" PE"));
                Assert.That(s, Does.Not.Contain("()"));
            });
        }

        [Test]
        public void ToString_MinimoConGuion_NoParentesisVacios_YConPais()
        {
            var a = DireccionPostal.FromPeru("-");
            var s = a.ToString();

            Assert.Multiple(() =>
            {
                Assert.That(s, Does.Contain("-"));
                Assert.That(s, Does.Contain(" PE"));
                Assert.That(s, Does.Not.Contain("()"));
            });
        }
    }
}
