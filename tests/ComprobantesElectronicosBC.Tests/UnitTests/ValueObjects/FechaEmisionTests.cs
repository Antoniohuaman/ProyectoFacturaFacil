using System;
using NUnit.Framework;
using ComprobantesElectronicosBC.Domain.ValueObjects;

namespace ComprobantesElectronicosBC.Tests.UnitTests.ValueObjects
{
    public class FechaEmisionTests
    {
        // Un "now" fijo para que las aserciones sean determinísticas
        private static readonly DateTime Now = new(2025, 08, 01, 14, 30, 45);
        private static readonly DateOnly Today = DateOnly.FromDateTime(Now);

        [Test]
        public void Hoy_Factura_TomaHoyYHoraDeNow()
        {
            var fe = FechaEmision.Hoy("01", Now);

            Assert.Multiple(() =>
            {
                Assert.That(fe.Fecha, Is.EqualTo(Today));
                Assert.That(fe.Hora, Is.EqualTo(TimeOnly.FromDateTime(Now)));
            });
        }

        [Test]
        public void Hoy_Boleta_TomaHoyYHoraDeNow()
        {
            var fe = FechaEmision.Hoy("03", Now);

            Assert.Multiple(() =>
            {
                Assert.That(fe.Fecha, Is.EqualTo(Today));
                Assert.That(fe.Hora, Is.EqualTo(TimeOnly.FromDateTime(Now)));
            });
        }

        [Test]
        public void Create_Hoy_MismoDia_Valido()
        {
            var fe = FechaEmision.Create(Today, "01", Now);
            Assert.That(fe.Fecha, Is.EqualTo(Today));
        }

        [Test]
        public void Create_FechaFutura_Lanza()
        {
            var future = Today.AddDays(1);
            var ex = Assert.Throws<ArgumentException>(() => FechaEmision.Create(future, "01", Now));
            Assert.That(ex!.Message, Does.Contain("no puede ser futura").IgnoreCase);
        }

        [Test]
        public void Create_Factura_RetroactivoHasta3Dias_PermiteLimiteYRechazaExceso()
        {
            // Límite permitido (3 días atrás)
            var ok = FechaEmision.Create(Today.AddDays(-3), "01", Now);
            Assert.That(ok.Fecha, Is.EqualTo(Today.AddDays(-3)));

            // Exceso (4 días) debe fallar
            var ex = Assert.Throws<ArgumentException>(() =>
                FechaEmision.Create(Today.AddDays(-4), "01", Now));
            Assert.That(ex!.Message, Does.Contain("excede 3 días").IgnoreCase);
        }

        [Test]
        public void Create_Boleta_RetroactivoHasta5Dias_PermiteLimiteYRechazaExceso()
        {
            // Límite permitido (5 días atrás)
            var ok = FechaEmision.Create(Today.AddDays(-5), "03", Now);
            Assert.That(ok.Fecha, Is.EqualTo(Today.AddDays(-5)));

            // Exceso (6 días) debe fallar
            var ex = Assert.Throws<ArgumentException>(() =>
                FechaEmision.Create(Today.AddDays(-6), "03", Now));
            Assert.That(ex!.Message, Does.Contain("excede 5 días").IgnoreCase);
        }

        [Test]
        public void Create_TipoConEspacios_SeTrimeaYValida()
        {
            var fe = FechaEmision.Create(Today, " 03 ", Now);
            Assert.That(fe.Fecha, Is.EqualTo(Today));
        }

        [Test]
        public void Hoy_TipoInvalido_Lanza()
        {
            var ex = Assert.Throws<ArgumentException>(() => FechaEmision.Hoy("99", Now));
            Assert.That(ex!.Message, Does.Contain("Tipo inválido").IgnoreCase);
        }

        [Test]
        public void Create_TipoInvalido_Lanza()
        {
            var ex = Assert.Throws<ArgumentException>(() => FechaEmision.Create(Today, "XX", Now));
            Assert.That(ex!.Message, Does.Contain("Tipo inválido").IgnoreCase);
        }

        [Test]
        public void ToString_FormatoEsperado()
        {
            var fe = FechaEmision.Hoy("01", Now);
            // yyyy-MM-dd HH:mm:ss
            Assert.That(fe.ToString(), Is.EqualTo("2025-08-01 14:30:45"));
        }
    }
}
