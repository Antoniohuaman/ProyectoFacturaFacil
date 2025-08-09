using System;
using NUnit.Framework;
using ComprobantesElectronicosBC.Domain.ValueObjects;

namespace ComprobantesElectronicosBC.Tests.UnitTests.ValueObjects
{
    public class FormaDePagoTests
    {
        [Test]
        public void Contado_DeberiaUsarCodigo10_YMetodoCONTADO()
        {
            var fp = FormaDePago.Contado();

            Assert.Multiple(() =>
            {
                Assert.That(fp.PaymentMeansCode, Is.EqualTo(FormaDePago.CONTADO)); // "10"
                Assert.That(fp.MetodoCodigo, Is.EqualTo("CONTADO"));
                Assert.That(fp.MetodoNombre, Is.Null);
                Assert.That(fp.EsContado, Is.True);
                Assert.That(fp.EsCredito, Is.False);
                Assert.That(fp.ToString(), Is.EqualTo("Contado (CONTADO)"));
            });
        }

        [Test]
        public void ContadoEfectivo_DeberiaSerContadoConMetodoEFECTIVO()
        {
            var fp = FormaDePago.ContadoEfectivo();

            Assert.Multiple(() =>
            {
                Assert.That(fp.PaymentMeansCode, Is.EqualTo(FormaDePago.CONTADO));
                Assert.That(fp.MetodoCodigo, Is.EqualTo("EFECTIVO"));
                Assert.That(fp.EsContado, Is.True);
            });
        }

        [Test]
        public void ContadoPredefinido_AceptaMayusMinusYEspacios_ValidaContraCatalogo()
        {
            var fp = FormaDePago.ContadoPredefinido("  efectivo  "); // normaliza → "EFECTIVO"

            Assert.Multiple(() =>
            {
                Assert.That(fp.MetodoCodigo, Is.EqualTo("EFECTIVO"));
                Assert.That(fp.PaymentMeansCode, Is.EqualTo(FormaDePago.CONTADO));
            });
        }

        [Test]
        public void ContadoPredefinido_RechazaMetodoNoCatalogado()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                FormaDePago.ContadoPredefinido("PAYPAL")); // no está en catálogo

            Assert.That(ex!.Message, Does.Contain("Método no permitido"));
        }

        [Test]
        public void ContadoPersonalizado_RequiereMetodo_NoSoloEspacios()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                FormaDePago.ContadoPersonalizado("   "));

            Assert.That(ex!.Message, Does.Contain("obligatorio").IgnoreCase);
        }

        [Test]
        public void ContadoPersonalizado_NormalizaAMayusculas_YPermiteNombreOpcional()
        {
            var fp = FormaDePago.ContadoPersonalizado("Pos Bcp", "POS QR Caja");

            Assert.Multiple(() =>
            {
                Assert.That(fp.MetodoCodigo, Is.EqualTo("POS BCP"));   // Upper/trim
                Assert.That(fp.MetodoNombre, Is.EqualTo("POS QR Caja"));
                Assert.That(fp.ToString(), Is.EqualTo("Contado (POS QR Caja)"));
            });
        }

        [Test]
        public void Validaciones_LongitudesMetodoYNombre()
        {
            var metodo31 = new string('A', 31);
            var nombre61 = new string('B', 61);

            var ex1 = Assert.Throws<ArgumentException>(() =>
                FormaDePago.ContadoPersonalizado(metodo31));

            var ex2 = Assert.Throws<ArgumentException>(() =>
                FormaDePago.ContadoPersonalizado("MI METODO", nombre61));

            Assert.Multiple(() =>
            {
                Assert.That(ex1!.Message, Does.Contain("código del método"));
                Assert.That(ex2!.Message, Does.Contain("nombre del método"));
            });
        }

        [Test]
        public void Credito_DeberiaUsarCodigo20_SinMetodo()
        {
            var fp = FormaDePago.Credito();

            Assert.Multiple(() =>
            {
                Assert.That(fp.PaymentMeansCode, Is.EqualTo(FormaDePago.CREDITO)); // "20"
                Assert.That(fp.MetodoCodigo, Is.Null);
                Assert.That(fp.MetodoNombre, Is.Null);
                Assert.That(fp.EsCredito, Is.True);
                Assert.That(fp.EsContado, Is.False);
                Assert.That(fp.ToString(), Is.EqualTo("Crédito"));
            });
        }

        [Test]
        public void Atajos_DeberianCrearContadoConMetodosEsperados()
        {
            Assert.That(FormaDePago.ContadoTarjeta().MetodoCodigo, Is.EqualTo("TARJETA"));
            Assert.That(FormaDePago.ContadoTransferencia("BCP").MetodoCodigo, Is.EqualTo("TRANSFERENCIA"));
            Assert.That(FormaDePago.ContadoYape().MetodoCodigo, Is.EqualTo("YAPE"));
            Assert.That(FormaDePago.ContadoPlin().MetodoCodigo, Is.EqualTo("PLIN"));
            Assert.That(FormaDePago.ContadoDeposito().MetodoCodigo, Is.EqualTo("DEPOSITO"));
            Assert.That(FormaDePago.ContadoBcp().MetodoCodigo, Is.EqualTo("BCP"));
            Assert.That(FormaDePago.ContadoInterbank().MetodoCodigo, Is.EqualTo("INTERBANK"));
        }
    }
}
