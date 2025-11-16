// tests/GestionClientesBC.Tests/ValueObjects/ObservacionesClienteTests.cs
using GestionClientesBC.Domain.ValueObjects;
using NUnit.Framework;

namespace GestionClientesBC.Tests.ValueObjects
{
    public class ObservacionesClienteTests
    {
        [Test]
        public void Create_NullOrWhitespace_ReturnsNull()
        {
            Assert.That(ObservacionesCliente.Create(null), Is.Null);
            Assert.That(ObservacionesCliente.Create("   "), Is.Null);
        }

        [Test]
        public void Create_TrimsAndLimitsLength()
        {
            var textoLargo = new string('A', 1500);

            var obs = ObservacionesCliente.Create("  " + textoLargo + "  ")!;

            Assert.That(obs.Valor.Length, Is.EqualTo(1000));
        }

        [Test]
        public void Equality_IsValueBased()
        {
            var a = ObservacionesCliente.Create("Cliente con acuerdos especiales")!;
            var b = ObservacionesCliente.Create("Cliente con acuerdos especiales")!;

            Assert.That(a, Is.EqualTo(b));
        }
    }
}
