// tests/GestionClientesBC.Tests/ValueObjects/MotivoDeshabilitacionClienteTests.cs
using GestionClientesBC.Domain.ValueObjects;
using NUnit.Framework;

namespace GestionClientesBC.Tests.ValueObjects
{
    public class MotivoDeshabilitacionClienteTests
    {
        [Test]
        public void Create_NullOrWhitespace_ReturnsNull()
        {
            Assert.That(MotivoDeshabilitacionCliente.Create(null), Is.Null);
            Assert.That(MotivoDeshabilitacionCliente.Create("   "), Is.Null);
        }

        [Test]
        public void Create_TrimsAndLimitsLength()
        {
            var textoLargo = new string('X', 600);

            var motivo = MotivoDeshabilitacionCliente.Create("  " + textoLargo + "  ")!;

            Assert.That(motivo.Valor.Length, Is.EqualTo(500));
        }

        [Test]
        public void Equality_IsValueBased()
        {
            var a = MotivoDeshabilitacionCliente.Create("Incumplimiento de pagos")!;
            var b = MotivoDeshabilitacionCliente.Create("Incumplimiento de pagos")!;

            Assert.That(a, Is.EqualTo(b));
        }
    }
}
