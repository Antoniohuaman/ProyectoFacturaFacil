using System;
using NUnit.Framework;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace SharedKernel.Tests.ValueObjects
{
    [TestFixture]
    public class NombrePersonaTests
    {
        [Test]
        public void Crear_ConDatosValidos_NormalizaYRetornaInstancia()
        {
            var np = NombrePersona.Crear("  José   Luis  ", "  O’Connor   López  ");

            Assert.That(np.Nombres, Is.EqualTo("José Luis"));
            Assert.That(np.Apellidos, Is.EqualTo("O’Connor López"));
            Assert.That(np.Completo, Is.EqualTo("José Luis O’Connor López"));
            Assert.That(np.ToString(), Is.EqualTo(np.Completo));
        }

        [Test]
        public void Crear_PermitePuntoGuionYApostrofos()
        {
            var np = NombrePersona.Crear("St. John", "D'Onofrio-López");

            Assert.That(np.Nombres, Is.EqualTo("St. John"));
            Assert.That(np.Apellidos, Is.EqualTo("D'Onofrio-López"));
        }

        [Test]
        public void Crear_NombresVacios_LanzaBusinessRuleException()
        {
            Assert.That(() => NombrePersona.Crear("", "Perez"),
                Throws.TypeOf<BusinessRuleException>());
            Assert.That(() => NombrePersona.Crear("   ", "Perez"),
                Throws.TypeOf<BusinessRuleException>());
        }

        [Test]
        public void Crear_ApellidosVacios_LanzaBusinessRuleException()
        {
            Assert.That(() => NombrePersona.Crear("Juan", ""),
                Throws.TypeOf<BusinessRuleException>());
        }

        [Test]
        public void Crear_ConCaracteresInvalidos_LanzaBusinessRuleException()
        {
            Assert.That(() => NombrePersona.Crear("Pepe123", "Pérez"),
                Throws.TypeOf<BusinessRuleException>(), "No deben permitirse dígitos en nombres.");
            Assert.That(() => NombrePersona.Crear("Ana@", "García"),
                Throws.TypeOf<BusinessRuleException>(), "No deben permitirse símbolos como '@'.");
        }

        [Test]
        public void Crear_SuperaLongitudMaxima_LanzaBusinessRuleException()
        {
            var nombresLargos = new string('a', 101);
            Assert.That(() => NombrePersona.Crear(nombresLargos, "Pérez"),
                Throws.TypeOf<BusinessRuleException>());

            var apellidosLargos = new string('b', 121);
            Assert.That(() => NombrePersona.Crear("Juan", apellidosLargos),
                Throws.TypeOf<BusinessRuleException>());
        }
    }
}
