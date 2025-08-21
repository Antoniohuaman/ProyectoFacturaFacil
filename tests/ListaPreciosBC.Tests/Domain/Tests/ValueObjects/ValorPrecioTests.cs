using System;
using NUnit.Framework;
using ListaPreciosBC.Domain.ValueObjects;
using SharedKernel.ValueObjects; // Dinero, Moneda

namespace ListaPreciosBC.Tests.ValueObjects
{
    [TestFixture]
    public class ValorPrecioTests
    {
    private static readonly Moneda PEN = Moneda.PEN(); // Asumiendo método fábrica en tu SK

        [Test]
        public void Crear_desde_dinero_valido_queda_con_incluye_impuesto_true_por_defecto()
        {
            var vp = ValorPrecio.Crear(new Dinero(118.00m, PEN));
            Assert.That(vp.Importe.Monto, Is.EqualTo(118.00m));
            Assert.That(vp.Importe.Moneda, Is.EqualTo(PEN));
            Assert.That(vp.IncluyeImpuesto, Is.True);
        }

        [Test]
        public void DesdeMonto_con_moneda_valida_funciona()
        {
            var vp = ValorPrecio.DesdeMonto(100m, PEN, incluyeImpuesto: false);
            Assert.That(vp.Importe.Monto, Is.EqualTo(100m));
            Assert.That(vp.IncluyeImpuesto, Is.False);
        }

        [Test]
        public void Crear_rechaza_importe_nulo_o_negativo()
        {
            Assert.That(() => ValorPrecio.Crear(null!), Throws.TypeOf<ArgumentNullException>());
            // Si Dinero ya valida negativos, esta aserción podría lanzar desde Dinero o desde ValorPrecio;
            // En ambos casos, esperamos alguna excepción.
            Assert.That(() => ValorPrecio.DesdeMonto(-1m, PEN), Throws.Exception);
        }

        [Test]
        public void ConImpuesto_y_SinImpuesto_cambian_solo_el_flag()
        {
            var baseVp = ValorPrecio.DesdeMonto(10m, PEN, false);

            var con = baseVp.ConImpuesto();
            var sin = con.SinImpuesto();

            Assert.That(con.IncluyeImpuesto, Is.True);
            Assert.That(sin.IncluyeImpuesto, Is.False);
            Assert.That(con.Importe, Is.EqualTo(baseVp.Importe));
            Assert.That(sin.Importe, Is.EqualTo(baseVp.Importe));
        }

        [Test]
        public void Neto_y_Bruto_para_articulo_gravado_funcionan_y_redondean()
        {
            // Tasa 18%
            const decimal tasa = 0.18m;

            // Caso A: precio ingresado con impuesto (bruto=118) -> neto=100
            var conImp = ValorPrecio.DesdeMonto(118.00m, PEN, incluyeImpuesto: true);
            var netoA = conImp.Neto(tasa, gravaImpuesto: true);
            var brutoA = conImp.Bruto(tasa, gravaImpuesto: true);

            Assert.That(netoA.Monto, Is.EqualTo(100.00m));
            Assert.That(brutoA.Monto, Is.EqualTo(118.00m)); // ya era bruto

            // Caso B: precio ingresado sin impuesto (neto=100) -> bruto=118
            var sinImp = ValorPrecio.DesdeMonto(100.00m, PEN, incluyeImpuesto: false);
            var netoB = sinImp.Neto(tasa, gravaImpuesto: true);
            var brutoB = sinImp.Bruto(tasa, gravaImpuesto: true);

            Assert.That(netoB.Monto, Is.EqualTo(100.00m));
            Assert.That(brutoB.Monto, Is.EqualTo(118.00m));

            // Redondeo AwayFromZero
            var conImpRed = ValorPrecio.DesdeMonto(10.015m, PEN, incluyeImpuesto: true);
            var netoRed = conImpRed.Neto(tasa, true);
            Assert.That(netoRed.Monto, Is.EqualTo(Math.Round(10.015m / 1.18m, 2, MidpointRounding.AwayFromZero)));
        }

        [Test]
        public void Neto_y_Bruto_para_articulo_no_gravado_ignoran_el_flag()
        {
            const decimal tasa = 0.18m;

            var conImp = ValorPrecio.DesdeMonto(118.00m, PEN, incluyeImpuesto: true);
            var sinImp = ValorPrecio.DesdeMonto(100.00m, PEN, incluyeImpuesto: false);

            var netoA = conImp.Neto(tasa, gravaImpuesto: false);
            var brutoA = conImp.Bruto(tasa, gravaImpuesto: false);

            var netoB = sinImp.Neto(tasa, gravaImpuesto: false);
            var brutoB = sinImp.Bruto(tasa, gravaImpuesto: false);

            Assert.That(netoA.Monto, Is.EqualTo(118.00m));
            Assert.That(brutoA.Monto, Is.EqualTo(118.00m));
            Assert.That(netoB.Monto, Is.EqualTo(100.00m));
            Assert.That(brutoB.Monto, Is.EqualTo(100.00m));
        }

        [Test]
        public void Igualdad_por_valor_considera_importe_y_flag()
        {
            var a = ValorPrecio.DesdeMonto(100m, PEN, incluyeImpuesto: true);
            var b = ValorPrecio.DesdeMonto(100m, PEN, incluyeImpuesto: true);
            var c = ValorPrecio.DesdeMonto(100m, PEN, incluyeImpuesto: false);
            var d = ValorPrecio.DesdeMonto(118m, PEN, incluyeImpuesto: true);

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.Equals(c), Is.False);
            Assert.That(a.Equals(d), Is.False);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void ToString_incluye_indicacion_de_impuestos()
        {
            var a = ValorPrecio.DesdeMonto(10m, PEN, true);
            var b = ValorPrecio.DesdeMonto(10m, PEN, false);

            Assert.That(a.ToString(), Does.Contain("Inc. Impuesto"));
            Assert.That(b.ToString(), Does.Contain("Sin impuesto"));
        }
    }
}
