using System.Globalization;
using NUnit.Framework;
using SharedKernel.ValueObjects;
using ConfiguracionSistemaBC.Domain.ValueObjects;

namespace ConfiguracionSistemaBC.Tests.ValueObjects
{
    [TestFixture]
    public class ModoPrecioTests
    {
        private Moneda PEN => Moneda.PEN();
        //private Moneda PEN => Moneda.PEN();

        [SetUp]
        public void SetUp()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        }

        // -----------------------------
        // Creación / mapping
        // -----------------------------

        [Test]
        public void From_IncluyeIGV_Y_SinIGV_MapeanCorrectamente()
        {
            var a = ModoPrecio.From("incluye_igv");
            var b = ModoPrecio.From("SIN_IGV");

            Assert.That(a.EsConIGV, Is.True);
            Assert.That(a.EsSinIGV, Is.False);

            Assert.That(b.EsSinIGV, Is.True);
            Assert.That(b.EsConIGV, Is.False);
        }

        [Test]
        public void TryFrom_Valido_DevuelveTrueYLaInstanciaCorrecta()
        {
            var ok1 = ModoPrecio.TryFrom("incluye_igv", out var a);
            var ok2 = ModoPrecio.TryFrom("sin_igv", out var b);

            Assert.That(ok1, Is.True);
            Assert.That(ok2, Is.True);
            Assert.That(a!.EsConIGV, Is.True);
            Assert.That(b!.EsSinIGV, Is.True);
        }

        [Test]
        public void TryFrom_Invalido_DevuelveFalse()
        {
            var ok = ModoPrecio.TryFrom("otro", out var m);
            Assert.That(ok, Is.False);
            Assert.That(m, Is.Null);
        }

        // -----------------------------
        // CalcularDesdeEditable (modo con IGV)
        // -----------------------------

        [Test]
        public void CalcularDesdeEditable_IncluyeIGV_UsandoPV_DaVV_IGV_y_PV_Consistentes()
        {
            var modo = ModoPrecio.IncluyeIGV;
            var tasa = 0.18m;

            // PV = 10.00 (con IGV) → VV = 8.47, IGV = 1.53 (para que VV + IGV = PV)
            var pvEditable = Dinero.Create(10.00m, PEN);
            var (vv, igv, pv) = modo.CalcularDesdeEditable(pvEditable, tasa);

            Assert.That(vv.Moneda, Is.EqualTo(PEN));
            Assert.That(igv.Moneda, Is.EqualTo(PEN));
            Assert.That(pv.Moneda, Is.EqualTo(PEN));

            Assert.That(vv.Monto, Is.EqualTo(8.47m));
            Assert.That(igv.Monto + vv.Monto, Is.EqualTo(pv.Monto).Within(0.01m)); // tolerancia normativa
            Assert.That(igv.Monto, Is.EqualTo(1.53m).Within(0.01m));
            Assert.That(pv.Monto, Is.EqualTo(10.00m));
        }

        [Test]
        public void CalcularDesdeEditable_IncluyeIGV_TasaFueraDeRango_Lanza()
        {
            var modo = ModoPrecio.IncluyeIGV;
            var pv = Dinero.Create(10m, PEN);

            Assert.That(() => modo.CalcularDesdeEditable(pv, -0.01m), Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => modo.CalcularDesdeEditable(pv, 1.01m), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        // -----------------------------
        // CalcularDesdeEditable (modo sin IGV)
        // -----------------------------

        [Test]
        public void CalcularDesdeEditable_SinIGV_UsandoVV_DaVV_IGV_y_PV_Consistentes()
        {
            var modo = ModoPrecio.SinIGV;
            var tasa = 0.18m;

            // VV = 8.47 (sin IGV) → IGV = 1.52, PV = 9.99 (por redondeo)
            var vvEditable = Dinero.Create(8.47m, PEN);
            var (vv, igv, pv) = modo.CalcularDesdeEditable(vvEditable, tasa);

            Assert.That(vv.Moneda, Is.EqualTo(PEN));
            Assert.That(igv.Moneda, Is.EqualTo(PEN));
            Assert.That(pv.Moneda, Is.EqualTo(PEN));

            Assert.That(vv.Monto, Is.EqualTo(8.47m));
            Assert.That(igv.Monto, Is.EqualTo(1.52m));   // 8.47 * 0.18 = 1.5246 -> 1.52
            Assert.That(pv.Monto, Is.EqualTo(9.99m));    // 8.47 * 1.18 = 9.9946 -> 9.99
            Assert.That(vv.Monto + igv.Monto, Is.EqualTo(pv.Monto));
        }

        [Test]
        public void CalcularDesdeEditable_SinIGV_TasaFueraDeRango_Lanza()
        {
            var modo = ModoPrecio.SinIGV;
            var vv = Dinero.Create(8m, PEN);

            Assert.That(() => modo.CalcularDesdeEditable(vv, -0.01m), Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => modo.CalcularDesdeEditable(vv, 1.01m), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        // -----------------------------
        // Conversores explícitos
        // -----------------------------

        [Test]
        public void DesdePrecioConIGV_CalculaVV_e_IGV_DeManeraQuePV_SeaIgual_VVmasIGV()
        {
            var tasa = 0.18m;
            var pv = Dinero.Create(10.00m, PEN);
            var modo = ModoPrecio.SinIGV; // el método no depende del estado; lo usamos como helper

            var (vv, igv, pvOut) = modo.DesdePrecioConIGV(pv, tasa);

            Assert.That(vv.Monto, Is.EqualTo(8.47m));
            Assert.That(igv.Monto, Is.EqualTo(1.53m).Within(0.01m)); // tolerancia normativa
            Assert.That(pvOut.Monto, Is.EqualTo(10.00m));
            Assert.That(vv.Monto + igv.Monto, Is.EqualTo(pvOut.Monto).Within(0.01m));
        }

        [Test]
        public void DesdeValorSinIGV_CalculaPV_e_IGV_Correctamente()
        {
            var tasa = 0.18m;
            var vv = Dinero.Create(8.47m, PEN);
            var modo = ModoPrecio.IncluyeIGV; // el método no depende del estado; lo usamos como helper

            var (vvOut, igv, pv) = modo.DesdeValorSinIGV(vv, tasa);

            Assert.That(vvOut.Monto, Is.EqualTo(8.47m));
            Assert.That(igv.Monto, Is.EqualTo(1.52m));
            Assert.That(pv.Monto, Is.EqualTo(9.99m));
            Assert.That(vvOut.Monto + igv.Monto, Is.EqualTo(pv.Monto));
        }

        // -----------------------------
        // Igualdad / ToString
        // -----------------------------

        [Test]
        public void Igualdad_PorValor_Y_ToString()
        {
            var a = ModoPrecio.From("INCLUYE_IGV");
            var b = ModoPrecio.IncluyeIGV;

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.ToString(), Is.EqualTo("INCLUYE_IGV"));
            Assert.That(ModoPrecio.SinIGV.ToString(), Is.EqualTo("SIN_IGV"));
        }
    }
}