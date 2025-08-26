using System;
using NUnit.Framework;
using ConfiguracionSistemaBC.Domain.ValueObjects;
using SharedKernel.Exceptions;

namespace ConfiguracionSistemaBC.Tests.Domain.ValueObjects
{
    [TestFixture]
    public class EstablecimientoIdTests
    {
        [Test]
        public void Desde_ok_hace_trim_y_conserva_caso()
        {
            var id = EstablecimientoId.Desde("  suc-01  ");

            Assert.That(id, Is.Not.Null);
            Assert.That(id.Valor, Is.EqualTo("suc-01")); // solo trim; no upper
            Assert.That(id.ToString(), Is.EqualTo("suc-01"));
        }

        [Test]
        public void Desde_vacio_lanza()
        {
            Assert.That(() => _ = EstablecimientoId.Desde(""),
                Throws.TypeOf<BusinessRuleException>().With.Message.Contains("obligatorio"));
        }

        [Test]
        public void Desde_espacios_lanza()
        {
            Assert.That(() => _ = EstablecimientoId.Desde("   "),
                Throws.TypeOf<BusinessRuleException>().With.Message.Contains("obligatorio"));
        }

        [Test]
        public void Desde_null_lanza()
        {
            Assert.That(() => _ = EstablecimientoId.Desde(null!),
                Throws.TypeOf<BusinessRuleException>().With.Message.Contains("obligatorio"));
        }

        [Test]
        public void Igualdad_por_valor()
        {
            var a = EstablecimientoId.Desde("SUC-01");
            var b = new EstablecimientoId("SUC-01");   // mismo valor
            var c = EstablecimientoId.Desde("SUC-02"); // distinto

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
            Assert.That(a, Is.Not.EqualTo(c));
        }

        [Test]
        public void ToString_devuelve_el_valor()
        {
            var id = EstablecimientoId.Desde("TIENDA-1");
            Assert.That(id.ToString(), Is.EqualTo("TIENDA-1"));
        }
    }
}
