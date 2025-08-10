using System;
using NUnit.Framework;
using ComprobantesElectronicosBC.Domain.ValueObjects;

namespace ComprobantesElectronicosBC.Tests.UnitTests.ValueObjects
{
    public class FechaVencimientoTests
    {
        private static readonly DateOnly Emision = new(2025, 8, 1);

        // ---------- Create / IgualAEmision / DesdeDiasCredito ----------

        [Test]
        public void Create_AceptaVencimientoIgualOPosteriorALaEmision()
        {
            // Igual a emisión
            var f1 = FechaVencimiento.Create(Emision, Emision);
            Assert.That(f1.Value, Is.EqualTo(Emision));

            // Posterior
            var venc = Emision.AddDays(10);
            var f2 = FechaVencimiento.Create(Emision, venc);
            Assert.That(f2.Value, Is.EqualTo(venc));
        }

        [Test]
        public void Create_LanzaSiVencimientoEsAnteriorALaEmision()
        {
            var anterior = Emision.AddDays(-1);
            Assert.Throws<ArgumentException>(() =>
                FechaVencimiento.Create(Emision, anterior));
        }

        [Test]
        public void IgualAEmision_FijaVencimientoEnMismaFecha()
        {
            var f = FechaVencimiento.IgualAEmision(Emision);
            Assert.That(f.Value, Is.EqualTo(Emision));
            Assert.That(f.EsMismoDiaQue(Emision), Is.True);
            Assert.That(f.DiasDesde(Emision), Is.EqualTo(0));
        }

        [Test]
        public void DesdeDiasCredito_CalculaVencimientoSumandoDias()
        {
            var f = FechaVencimiento.DesdeDiasCredito(Emision, 30);
            Assert.That(f.Value, Is.EqualTo(Emision.AddDays(30)));
            Assert.That(f.DiasDesde(Emision), Is.EqualTo(30));
        }

        [Test]
        public void DesdeDiasCredito_LanzaSiDiasNoPositivos()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                FechaVencimiento.DesdeDiasCredito(Emision, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                FechaVencimiento.DesdeDiasCredito(Emision, -5));
        }

        // ----------------------------- TryCreate -----------------------------

        [Test]
        public void TryCreate_DevuelveTrueYObjetoCuandoEsValido()
        {
            var ok = FechaVencimiento.TryCreate(Emision, Emision.AddDays(5), out var f);
            Assert.That(ok, Is.True);
            Assert.That(f, Is.Not.Null);
            Assert.That(f!.Value, Is.EqualTo(Emision.AddDays(5)));
        }

        [Test]
        public void TryCreate_DevuelveFalseCuandoEsInvalido()
        {
            var ok = FechaVencimiento.TryCreate(Emision, Emision.AddDays(-1), out var f);
            Assert.That(ok, Is.False);
            Assert.That(f, Is.Null);
        }

        // ---------------------- Integración con FormaDePago ----------------------

        [Test]
        public void ParaFormaDePago_Contado_IgualaVencimientoALaEmision()
        {
            var contado = FormaDePago.ContadoEfectivo(); // cualquier método predefinido
            var f = FechaVencimiento.ParaFormaDePago(contado, Emision, diasCredito: 999); // se ignora en contado
            Assert.That(f.Value, Is.EqualTo(Emision));
            Assert.That(f.EsMismoDiaQue(Emision), Is.True);
        }

        [Test]
        public void ParaFormaDePago_Credito_RequiereDiasCreditoYCalculaFecha()
        {
            var credito = FormaDePago.Credito();

            // Lanza si no se proveen días > 0
            Assert.Throws<ArgumentException>(() =>
                FechaVencimiento.ParaFormaDePago(credito, Emision, null));
            Assert.Throws<ArgumentException>(() =>
                FechaVencimiento.ParaFormaDePago(credito, Emision, 0));

            // OK con días válidos
            var f = FechaVencimiento.ParaFormaDePago(credito, Emision, 15);
            Assert.That(f.Value, Is.EqualTo(Emision.AddDays(15)));
            Assert.That(f.DiasDesde(Emision), Is.EqualTo(15));
        }

        // ------------------------------ Utilidades ------------------------------

        [Test]
        public void ToString_FormatoIso_yyyyMMdd()
        {
            var f = FechaVencimiento.Create(Emision, Emision.AddDays(3));
            Assert.That(f.ToString(), Is.EqualTo(Emision.AddDays(3).ToString("yyyy-MM-dd")));
        }
    }
}
