using System;
using NUnit.Framework;
using ConfiguracionSistemaBC.Domain.ValueObjects;
using SharedKernel.Exceptions;

namespace ConfiguracionSistemaBC.Tests.UnitTests.Domain.ValueObjects
{
    [TestFixture]
    public class UsuariosTests
    {
        // -------------------- EmpresaId --------------------

        [Test]
        public void EmpresaId_Desde_ok_y_trim()
        {
            var e = EmpresaId.Desde("  EMP01  ");

            Assert.That(e, Is.Not.Null);
            Assert.That(e.Valor, Is.EqualTo("EMP01"));
            Assert.That(e.ToString(), Is.EqualTo("EMP01"));
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase(null)]
        public void EmpresaId_Desde_vacio_lanza(string input)
        {
            Assert.That(() => _ = EmpresaId.Desde(input!),
                Throws.TypeOf<BusinessRuleException>().With.Message.Contains("obligatorio"));
        }

        [Test]
        public void EmpresaId_es_record_value_equality()
        {
            var a = EmpresaId.Desde("EMP01");
            var b = EmpresaId.Desde("EMP01");
            var c = EmpresaId.Desde("EMP02");

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a, Is.Not.EqualTo(c));
        }


        // -------------------- CorreoElectronico --------------------

        [TestCase("user@dominio.com")]
        [TestCase("USER@DOMINIO.COM")]               // mayúsculas válidas
        [TestCase("nombre.apellido+tag@mail.co")]
        [TestCase("a@b.cc")]
        public void CorreoElectronico_Crear_valido(string email)
        {
            var e = CorreoElectronico.Crear(email);

            Assert.That(e.Valor, Is.EqualTo(email.Trim()));
            Assert.That(e.ToString(), Is.EqualTo(email.Trim()));
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase(null)]
        public void CorreoElectronico_vacio_lanza(string input)
        {
            Assert.That(() => _ = CorreoElectronico.Crear(input!),
                Throws.TypeOf<BusinessRuleException>().With.Message.Contains("obligatorio"));
        }

        [TestCase("invalido")]
        [TestCase("a@b")]
        [TestCase("@dominio.com")]
        [TestCase("user@@dominio.com")]
        [TestCase("user dominio@x.com")]
        [TestCase("user@dominio.")]
        public void CorreoElectronico_formato_invalido_lanza(string email)
        {
            Assert.That(() => _ = CorreoElectronico.Crear(email),
                Throws.TypeOf<BusinessRuleException>().With.Message.Contains("inválido"));
        }

        [Test]
        public void CorreoElectronico_trim()
        {
            var e = CorreoElectronico.Crear("   demo@mail.com   ");
            Assert.That(e.Valor, Is.EqualTo("demo@mail.com"));
        }

        // -------------------- NombrePersona --------------------

        [Test]
        public void NombrePersona_Crear_ok_y_trim_y_completo()
        {
            var n = NombrePersona.Crear("  Juan  ", "  Pérez  ");

            Assert.That(n.Nombres, Is.EqualTo("Juan"));
            Assert.That(n.Apellidos, Is.EqualTo("Pérez"));
            Assert.That(n.Completo, Is.EqualTo("Juan Pérez"));
            Assert.That(n.ToString(), Is.EqualTo("Juan Pérez"));
        }

        [TestCase("", "Pérez", "Nombres")]
        [TestCase("   ", "Pérez", "Nombres")]
        [TestCase(null, "Pérez", "Nombres")]
        [TestCase("Juan", "", "Apellidos")]
        [TestCase("Juan", "   ", "Apellidos")]
        [TestCase("Juan", null, "Apellidos")]
        public void NombrePersona_vacios_lanza(string nombres, string apellidos, string campoEsperado)
        {
            Assert.That(() => _ = NombrePersona.Crear(nombres!, apellidos!),
                Throws.TypeOf<BusinessRuleException>().With.Message.Contains(campoEsperado));
        }

        [Test]
        public void NombrePersona_es_record_value_equality()
        {
            var a = NombrePersona.Crear("Ana", "Ríos");
            var b = new NombrePersona("Ana", "Ríos");
            var c = NombrePersona.Crear("Ana", "Díaz");

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a, Is.Not.EqualTo(c));
        }

        // -------------------- PasswordHash --------------------

        [Test]
        public void PasswordHash_DesdeHash_ok()
        {
            var h = PasswordHash.DesdeHash("HASH-ABC");
            Assert.That(h.Valor, Is.EqualTo("HASH-ABC"));
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase(null)]
        public void PasswordHash_DesdeHash_vacio_lanza(string input)
        {
            Assert.That(() => _ = PasswordHash.DesdeHash(input!),
                Throws.TypeOf<BusinessRuleException>().With.Message.Contains("obligatorio"));
        }

        [Test]
        public void PasswordHash_DesdeTextoPlano_hashea_con_servicio()
        {
            var hasher = new HasherFake();
            var h = PasswordHash.DesdeTextoPlano("secreto", hasher);

            Assert.That(h.Valor, Is.EqualTo("HASH::secreto"));
            Assert.That(hasher.UltimoTextoPlano, Is.EqualTo("secreto"));
            Assert.That(hasher.CantidadLlamadas, Is.EqualTo(1));
        }
    // -------------------- EstablecimientoId --------------------

    [Test]
            public void EstablecimientoId_Desde_ok_y_trim()
            {
                var e = EstablecimientoId.Desde("  EST01  ");

                Assert.That(e, Is.Not.Null);
                Assert.That(e.Valor, Is.EqualTo("EST01"));
                Assert.That(e.ToString(), Is.EqualTo("EST01"));
            }

            [TestCase("")]
            [TestCase("   ")]
            [TestCase(null)]
            public void EstablecimientoId_Desde_vacio_lanza(string input)
            {
                Assert.That(() => _ = EstablecimientoId.Desde(input!),
                    Throws.TypeOf<BusinessRuleException>().With.Message.Contains("obligatorio"));
            }

            [Test]
            public void EstablecimientoId_es_record_value_equality()
            {
                var a = EstablecimientoId.Desde("EST01");
                var b = EstablecimientoId.Desde("EST01");
                var c = EstablecimientoId.Desde("EST02");

                Assert.That(a, Is.EqualTo(b));
                Assert.That(a, Is.Not.EqualTo(c));
            }
        public void PasswordHash_DesdeTextoPlano_vacio_lanza()
        {
            var hasher = new HasherFake();
            Assert.That(() => _ = PasswordHash.DesdeTextoPlano("   ", hasher),
                Throws.TypeOf<BusinessRuleException>().With.Message.Contains("no puede ser vacía"));
        }

        [Test]
        public void PasswordHash_DesdeTextoPlano_con_hasher_nulo_lanza()
        {
            Assert.That(() => _ = PasswordHash.DesdeTextoPlano("x", null!),
                Throws.TypeOf<ArgumentNullException>());
        }

        // ================== Fakes ==================
        private sealed class HasherFake : IPasswordHasher
        {
            public string? UltimoTextoPlano { get; private set; }
            public int CantidadLlamadas { get; private set; }

            public string Hash(string textoPlano)
            {
                CantidadLlamadas++;
                UltimoTextoPlano = textoPlano;
                return $"HASH::{textoPlano}";
            }
        }
    }
}
