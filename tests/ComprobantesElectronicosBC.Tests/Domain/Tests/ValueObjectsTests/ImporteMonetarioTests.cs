using NUnit.Framework;
using System;
using System.Text.Json;
using ComprobantesElectronicosBC.Domain.ValueObjects;

namespace ComprobantesElectronicosBC.Tests.ValueObjects
{
    [TestFixture]
    public class ImporteMonetarioTests
    {
    private SharedKernel.ValueObjects.Moneda PEN = null!;
    private SharedKernel.ValueObjects.Moneda USD = null!;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            PEN = SharedKernel.ValueObjects.Moneda.PEN();
            USD = SharedKernel.ValueObjects.Moneda.USD();
        }

        // ---------- Creación y normalización ----------
        [Test]
        public void Create_NoNegativos_RedondeaSegunMoneda()
        {
            var imp = ImporteMonetario.Create(12.345m, PEN); // 2 decimales
            Assert.That(imp.Monto, Is.EqualTo(12.35m));
            Assert.That(imp.Moneda, Is.EqualTo(PEN));
        }

        [Test]
        public void Create_Negativo_Lanza()
        {
            Assert.That(() => ImporteMonetario.Create(-0.01m, PEN),
                        Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void CreateLibre_PermiteNegativos_YRedondea()
        {
            var imp = ImporteMonetario.CreateLibre(-5.555m, USD);
            Assert.That(imp.Monto, Is.EqualTo(-5.56m));
            Assert.That(imp.Moneda, Is.EqualTo(USD));
        }

        [Test]
        public void Zero_EsCeroYEnMonedaCorrecta()
        {
            var imp = ImporteMonetario.Zero(PEN);
            Assert.That(imp.EsCero, Is.True);
            Assert.That(imp.Monto, Is.EqualTo(0m));
            Assert.That(imp.Moneda, Is.EqualTo(PEN));
        }

        [Test]
        public void ConMonto_CreaNuevaInstancia_Normalizada()
        {
            var a = ImporteMonetario.Create(1.234m, PEN);
            var b = a.ConMonto(9.999m);
            Assert.That(b.Monto, Is.EqualTo(10.00m));
            Assert.That(b.Moneda, Is.EqualTo(PEN));
            Assert.That(b, Is.Not.SameAs(a));
        }

        // ---------- Operaciones aritméticas ----------
        [Test]
        public void Sumar_MismaMoneda_Ok()
        {
            var a = ImporteMonetario.Create(10.10m, PEN);
            var b = ImporteMonetario.Create( 2.23m, PEN);
            var r = a.Sumar(b);

            Assert.That(r.Monto, Is.EqualTo(12.33m));
            Assert.That(r.Moneda, Is.EqualTo(PEN));
        }

        [Test]
        public void Sumar_MonedaDistinta_Lanza()
        {
            var a = ImporteMonetario.Create(10m, PEN);
            var b = ImporteMonetario.Create( 1m, USD);
            Assert.That(() => a.Sumar(b), Throws.InvalidOperationException);
        }

        [Test]
        public void TrySumar_MonedaDistinta_RetornaFalse_SinResultado()
        {
            var a = ImporteMonetario.Create(10m, PEN);
            var b = ImporteMonetario.Create( 1m, USD);

            var ok = a.TrySumar(b, out var res);
            Assert.That(ok, Is.False);
            Assert.That(res, Is.Null);
        }

        [Test]
        public void Restar_MismaMoneda_Ok()
        {
            var a = ImporteMonetario.Create(10.00m, USD);
            var b = ImporteMonetario.Create( 2.19m, USD);
            var r = a.Restar(b);

            Assert.That(r.Monto, Is.EqualTo(7.81m));
            Assert.That(r.Moneda, Is.EqualTo(USD));
        }

        [Test]
        public void TryRestar_MonedaDistinta_RetornaFalse_SinResultado()
        {
            var a = ImporteMonetario.Create(10m, PEN);
            var b = ImporteMonetario.Create( 1m, USD);

            var ok = a.TryRestar(b, out var res);
            Assert.That(ok, Is.False);
            Assert.That(res, Is.Null);
        }

        [Test]
        public void Multiplicar_AplicaRedondeoSegunMoneda()
        {
            var a = ImporteMonetario.Create(2.345m, PEN);
            var r = a.Multiplicar(3m); // 7.035 -> 7.04
            Assert.That(r.Monto, Is.EqualTo(7.04m));
            Assert.That(r.Moneda, Is.EqualTo(PEN));
        }

        // ---------- Operadores ----------
        [Test]
        public void Operadores_SumanRestanYMultiplican()
        {
            var a = ImporteMonetario.Create(5.10m, PEN);
            var b = ImporteMonetario.Create(1.05m, PEN);

            var sum = a + b;    // 6.15
            var dif = a - b;    // 4.05
            var mul1 = a * 2m;  // 10.20
            var mul2 = 3m * b;  // 3.15

            Assert.That(sum.Monto, Is.EqualTo(6.15m));
            Assert.That(dif.Monto, Is.EqualTo(4.05m));
            Assert.That(mul1.Monto, Is.EqualTo(10.20m));
            Assert.That(mul2.Monto, Is.EqualTo(3.15m));
        }

        // ---------- Minor units ----------
        [Test]
        public void MinorUnits_RoundTrip_OK()
        {
            var a = ImporteMonetario.Create(12.34m, USD);
            var cents = a.AMinorUnits();
            Assert.That(cents, Is.EqualTo(1234L));

            var b = ImporteMonetario.DesdeMinorUnits(USD, cents);
            Assert.That(b.Monto, Is.EqualTo(a.Monto));
            Assert.That(b.Moneda, Is.EqualTo(USD));
        }

        // ---------- ToString ----------
        [Test]
        public void ToString_UsaFormateoDeMoneda()
        {
            var a = ImporteMonetario.Create(1234.5m, PEN);
            var s = a.ToString();

            // Moneda.Formatear usa símbolo + N{dec} con InvariantCulture
            Assert.That(s, Does.StartWith("S/. "));
            Assert.That(s, Does.Contain("1,234.50"));
        }

        // ---------- JSON (se ignora si Moneda no es deserializable con STJ) ----------
    }
}