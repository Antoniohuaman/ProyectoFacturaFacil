using System;
using System.Globalization;
using NUnit.Framework;
using CatalogoArticulosBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace CatalogoArticulosBC.Domain.Tests.ValueObjects
{
    [TestFixture]
    public class PrecioVentaTests
    {
        private CultureInfo? _oldCulture;
        private CultureInfo? _oldUICulture;

        [SetUp]
        public void SetUp()
        {
            // Congelar cultura para que P0 y F2 sean determinísticos en assertions
            _oldCulture = CultureInfo.CurrentCulture;
            _oldUICulture = CultureInfo.CurrentUICulture;
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        }

        [TearDown]
        public void TearDown()
        {
            CultureInfo.DefaultThreadCurrentCulture = _oldCulture ?? CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = _oldUICulture ?? CultureInfo.InvariantCulture;
        }

        // -------------------- Constructor guard clauses --------------------

        [Test]
        public void Ctor_MontoNegativo_LanzaOutOfRange()
        {
            TestDelegate act = () => _ = new PrecioVenta(-0.01m, Moneda.USD(), AfectacionImpuesto.Gravado_10);

            Assert.That(act, Throws.TypeOf<ArgumentOutOfRangeException>()
                .With.Property("ParamName").EqualTo("monto"));
        }

        [Test]
        public void Ctor_AfectacionNull_LanzaArgumentNull()
        {
            TestDelegate act = () => _ = new PrecioVenta(100m, Moneda.USD(), afectacionImpuesto: null!);

            Assert.That(act, Throws.TypeOf<ArgumentNullException>()
                .With.Property("ParamName").EqualTo("afectacionImpuesto"));
        }

        [Test]
        public void Ctor_Valido_AsignaPropiedades()
        {
            var p = new PrecioVenta(118m, Moneda.USD(), AfectacionImpuesto.Gravado_10, incluyeIGV: true);

            Assert.That(p.Monto, Is.EqualTo(118m));
            Assert.That(p.Moneda, Is.EqualTo(Moneda.USD()));
            Assert.That(p.AfectacionImpuesto, Is.EqualTo(AfectacionImpuesto.Gravado_10));
            Assert.That(p.IncluyeIGV, Is.True);
        }

        // -------------------- Cálculo con IGV gravado (18%) --------------------

        [Test]
        public void ConIGV_Gravado_ValorSinIGV_DividePor_1MasTasa()
        {
            // 118 con IGV -> base 100 exacto (tasa 18%)
            var p = new PrecioVenta(118m, Moneda.USD(), AfectacionImpuesto.Gravado_10, incluyeIGV: true);

            Assert.That(p.ValorSinIGV, Is.EqualTo(100m));
            Assert.That(p.ValorConIGV, Is.EqualTo(118m)); // ya incluye IGV
        }

        [Test]
        public void SinIGV_Gravado_ValorConIGV_MultiplicaPor_1MasTasa()
        {
            // 100 sin IGV -> con IGV 118 exacto
            var p = new PrecioVenta(100m, Moneda.USD(), AfectacionImpuesto.Gravado_10, incluyeIGV: false);

            Assert.That(p.ValorSinIGV, Is.EqualTo(100m));
            Assert.That(p.ValorConIGV, Is.EqualTo(118m));
        }

        // -------------------- Cálculo exonerado / inafecto (tasa 0) --------------------

        [Test]
        public void ConIGV_Exonerado_TasaCero_NoCambiaImporte()
        {
            var p = new PrecioVenta(150m, Moneda.PEN(), AfectacionImpuesto.Exonerado_20, incluyeIGV: true);

            Assert.That(p.ValorSinIGV, Is.EqualTo(150m));
            Assert.That(p.ValorConIGV, Is.EqualTo(150m));
        }

        [Test]
        public void SinIGV_Exonerado_TasaCero_NoCambiaImporte()
        {
            var p = new PrecioVenta(150m, Moneda.PEN(), AfectacionImpuesto.Exonerado_20, incluyeIGV: false);

            Assert.That(p.ValorSinIGV, Is.EqualTo(150m));
            Assert.That(p.ValorConIGV, Is.EqualTo(150m));
        }

        // -------------------- ToString (formato) --------------------

        [Test]
        public void ToString_ConIGV_Gravado_MuestraIncPorcentaje()
        {
            var p = new PrecioVenta(118m, Moneda.USD(), AfectacionImpuesto.Gravado_10, incluyeIGV: true);

            // Con cultura Invariant: "$ 118.00 (Inc. 18 %)"
            Assert.That(p.ToString(), Is.EqualTo("$ 118.00 (Inc. 18 %)"));
        }

        [Test]
        public void ToString_SinIGV_Gravado_MuestraMasPorcentaje()
        {
            var p = new PrecioVenta(100m, Moneda.USD(), AfectacionImpuesto.Gravado_10, incluyeIGV: false);

            // Con cultura Invariant: "$ 100.00 (+18 %)"
            Assert.That(p.ToString(), Is.EqualTo("$ 100.00 (+18 %)"));
        }

        [Test]
        public void ToString_Exonerado_MuestraCeroPorciento()
        {
            var p1 = new PrecioVenta(100m, Moneda.USD(), AfectacionImpuesto.Exonerado_20, incluyeIGV: true);
            var p2 = new PrecioVenta(100m, Moneda.USD(), AfectacionImpuesto.Exonerado_20, incluyeIGV: false);

            Assert.That(p1.ToString(), Is.EqualTo("$ 100.00 (Inc. 0 %)"));
            Assert.That(p2.ToString(), Is.EqualTo("$ 100.00 (+0 %)"));
        }

        // -------------------- Igualdad y hash --------------------

        [Test]
        public void Igualdad_MismosCampos_True_YHashIgual()
        {
            var a = new PrecioVenta(118m, Moneda.USD(), AfectacionImpuesto.Gravado_10, incluyeIGV: true);
            var b = new PrecioVenta(118m, Moneda.USD(), AfectacionImpuesto.Gravado_10, incluyeIGV: true);
            Assert.That(a.Moneda, Is.EqualTo(Moneda.USD()));
            Assert.That(a.AfectacionImpuesto, Is.EqualTo(AfectacionImpuesto.Gravado_10));
            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.Equals(b), Is.True);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }
    [Test]
        public void Desigualdad_PorMonto_Moneda_IncluyeIGV_O_Afectacion()
        {
            var baseOk = new PrecioVenta(100m, Moneda.USD(), AfectacionImpuesto.Gravado_10, incluyeIGV: true);

            var distintoMonto = new PrecioVenta(99m, Moneda.USD(), AfectacionImpuesto.Gravado_10, incluyeIGV: true);
            var distintaMoneda = new PrecioVenta(100m, Moneda.PEN(), AfectacionImpuesto.Gravado_10, incluyeIGV: true);
            var distintoIncluyeIGV = new PrecioVenta(100m, Moneda.USD(), AfectacionImpuesto.Gravado_10, incluyeIGV: false);
            var distintaAfectacion = new PrecioVenta(100m, Moneda.USD(), AfectacionImpuesto.Exonerado_20, incluyeIGV: true);

            Assert.That(baseOk, Is.Not.EqualTo(distintoMonto));
            Assert.That(baseOk, Is.Not.EqualTo(distintaMoneda));
            Assert.That(baseOk, Is.Not.EqualTo(distintoIncluyeIGV));
            Assert.That(baseOk, Is.Not.EqualTo(distintaAfectacion));
        }

        [Test]
        public void Equals_ContraNull_False()
        {
            var a = new PrecioVenta(10m, Moneda.USD(), AfectacionImpuesto.Gravado_10);
            Assert.That(a.Equals(null), Is.False);
        }
    }
}

