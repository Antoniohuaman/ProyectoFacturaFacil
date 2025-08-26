using NUnit.Framework;
using GestionClientesBC.Domain.ValueObjects;
using SharedKernel.Exceptions;

namespace GestionClientesBC.Tests.ValueObjects
{
    [TestFixture]
    public class RolComercialTests
    {
        [Test]
        public void Instancias_Estandar()
        {
            Assert.That(RolComercial.Ninguno.Mascara, Is.EqualTo(0));
            Assert.That(RolComercial.SoloCliente.Mascara, Is.EqualTo(1));
            Assert.That(RolComercial.SoloProveedor.Mascara, Is.EqualTo(2));
            Assert.That(RolComercial.ClienteProveedor.Mascara, Is.EqualTo(3));

            Assert.That(RolComercial.SoloCliente.Nombre, Is.EqualTo("Cliente"));
            Assert.That(RolComercial.SoloProveedor.Nombre, Is.EqualTo("Proveedor"));
            Assert.That(RolComercial.ClienteProveedor.Nombre, Is.EqualTo("Cliente/Proveedor"));
            Assert.That(RolComercial.Ninguno.Nombre, Is.EqualTo("Sin rol"));

            Assert.That(RolComercial.SoloCliente.Codigo, Is.EqualTo("C"));
            Assert.That(RolComercial.SoloProveedor.Codigo, Is.EqualTo("P"));
            Assert.That(RolComercial.ClienteProveedor.Codigo, Is.EqualTo("CP"));
            Assert.That(RolComercial.Ninguno.Codigo, Is.EqualTo("N"));
        }

        [TestCase("n",  "Sin rol")]
        [TestCase("C",  "Cliente")]
        [TestCase("p",  "Proveedor")]
        [TestCase("cp", "Cliente/Proveedor")]
        public void DesdeCodigo_MapeaCorrectamente(string codigo, string nombreEsperado)
        {
            var r = RolComercial.DesdeCodigo(codigo);
            Assert.That(r.Nombre, Is.EqualTo(nombreEsperado));
        }

        [Test]
        public void DesdeCodigo_Invalido_Lanza()
        {
            Assert.That(() => RolComercial.DesdeCodigo("x"),
                Throws.TypeOf<BusinessRuleException>());
            Assert.That(() => RolComercial.DesdeCodigo(""),
                Throws.TypeOf<BusinessRuleException>());
        }

        [TestCase(true,  false, "C")]
        [TestCase(false, true,  "P")]
        [TestCase(true,  true,  "CP")]
        [TestCase(false, false, "N")]
        public void DesdeBools_MapeaCorrectamente(bool esCliente, bool esProveedor, string codigo)
        {
            var r = RolComercial.DesdeBools(esCliente, esProveedor);
            Assert.That(r.Codigo, Is.EqualTo(codigo));
        }

        [Test]
        public void Agregar_Quitar_Idempotentes_Y_Combinables()
        {
            var r = RolComercial.Ninguno;

            r = r.AgregarCliente();
            Assert.That(r, Is.EqualTo(RolComercial.SoloCliente));

            r = r.AgregarProveedor();
            Assert.That(r, Is.EqualTo(RolComercial.ClienteProveedor));

            r = r.QuitarCliente();
            Assert.That(r, Is.EqualTo(RolComercial.SoloProveedor));

            r = r.QuitarProveedor();
            Assert.That(r, Is.EqualTo(RolComercial.Ninguno));

            // Idempotencia
            Assert.That(r.QuitarProveedor(), Is.EqualTo(RolComercial.Ninguno));
            Assert.That(r.AgregarCliente().AgregarCliente(), Is.EqualTo(RolComercial.SoloCliente));
        }

        [Test]
        public void Guards_DeOperacion_SegunRol()
        {
            // Solo Cliente: venta OK, compra falla
            var c = RolComercial.SoloCliente;
            Assert.That(() => c.AsegurarPuedeEmitirComprobanteVenta(), Throws.Nothing);
            Assert.That(() => c.AsegurarPuedeRegistrarCompra(), Throws.TypeOf<BusinessRuleException>());

            // Solo Proveedor: venta falla, compra OK
            var p = RolComercial.SoloProveedor;
            Assert.That(() => p.AsegurarPuedeEmitirComprobanteVenta(), Throws.TypeOf<BusinessRuleException>());
            Assert.That(() => p.AsegurarPuedeRegistrarCompra(), Throws.Nothing);

            // Ambos: todo OK
            var cp = RolComercial.ClienteProveedor;
            Assert.That(() => cp.AsegurarPuedeEmitirComprobanteVenta(), Throws.Nothing);
            Assert.That(() => cp.AsegurarPuedeRegistrarCompra(), Throws.Nothing);

            // Ninguno: todo falla
            var n = RolComercial.Ninguno;
            Assert.That(() => n.AsegurarPuedeEmitirComprobanteVenta(), Throws.TypeOf<BusinessRuleException>());
            Assert.That(() => n.AsegurarPuedeRegistrarCompra(), Throws.TypeOf<BusinessRuleException>());
        }

        [Test]
        public void AsegurarTieneAlMenosUnRol()
        {
            Assert.That(() => RolComercial.Ninguno.AsegurarTieneAlMenosUnRol(),
                Throws.TypeOf<BusinessRuleException>());

            Assert.That(() => RolComercial.SoloCliente.AsegurarTieneAlMenosUnRol(),
                Throws.Nothing);
        }

        [Test]
        public void IgualdadPorValor()
        {
            var a = RolComercial.DesdeCodigo("cp");
            var b = RolComercial.ClienteProveedor;
            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
            Assert.That(a.ToString(), Is.EqualTo("Cliente/Proveedor"));
        }
    }
}
