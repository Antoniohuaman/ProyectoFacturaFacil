using NUnit.Framework;
using GestionClientesBC.Domain.ValueObjects;
using SharedKernel.Exceptions;

namespace GestionClientesBC.Tests.ValueObjects
{
    [TestFixture]
    public class TipoClienteTests
    {

        [Test]
        public void Instancias_Estandar()
        {
            Assert.That(TipoCliente.SoloCliente.Mascara, Is.EqualTo(1));
            Assert.That(TipoCliente.SoloProveedor.Mascara, Is.EqualTo(2));
            Assert.That(TipoCliente.ClienteProveedor.Mascara, Is.EqualTo(3));

            Assert.That(TipoCliente.SoloCliente.Nombre, Is.EqualTo("Cliente"));
            Assert.That(TipoCliente.SoloProveedor.Nombre, Is.EqualTo("Proveedor"));
            Assert.That(TipoCliente.ClienteProveedor.Nombre, Is.EqualTo("Cliente/Proveedor"));

            Assert.That(TipoCliente.SoloCliente.Codigo, Is.EqualTo("C"));
            Assert.That(TipoCliente.SoloProveedor.Codigo, Is.EqualTo("P"));
            Assert.That(TipoCliente.ClienteProveedor.Codigo, Is.EqualTo("CP"));
        }


        [TestCase("C",  "Cliente")]
        [TestCase("p",  "Proveedor")]
        [TestCase("cp", "Cliente/Proveedor")]
        public void DesdeCodigo_MapeaCorrectamente(string codigo, string nombreEsperado)
        {
            var r = TipoCliente.DesdeCodigo(codigo);
            Assert.That(r.Nombre, Is.EqualTo(nombreEsperado));
        }


        [Test]
        public void DesdeCodigo_Invalido_Lanza()
        {
            Assert.That(() => TipoCliente.DesdeCodigo("x"),
                Throws.TypeOf<BusinessRuleException>());
            Assert.That(() => TipoCliente.DesdeCodigo(""),
                Throws.TypeOf<BusinessRuleException>());
        }


        [TestCase(true,  false, "C")]
        [TestCase(false, true,  "P")]
        [TestCase(true,  true,  "CP")]
        public void DesdeBools_MapeaCorrectamente(bool esCliente, bool esProveedor, string codigo)
        {
            var r = TipoCliente.DesdeBools(esCliente, esProveedor);
            Assert.That(r.Codigo, Is.EqualTo(codigo));
        }

        [Test]
        public void DesdeBools_TodosFalse_Lanza()
        {
            Assert.That(() => TipoCliente.DesdeBools(false, false),
                Throws.TypeOf<BusinessRuleException>());
        }


        [Test]
        public void Agregar_Quitar_Idempotentes_Y_Combinables()
        {
            var r = TipoCliente.SoloCliente;

            // Agregar proveedor
            r = r.AgregarProveedor();
            Assert.That(r, Is.EqualTo(TipoCliente.ClienteProveedor));

            // Quitar cliente
            r = r.QuitarCliente();
            Assert.That(r, Is.EqualTo(TipoCliente.SoloProveedor));

            // Agregar cliente de nuevo
            r = r.AgregarCliente();
            Assert.That(r, Is.EqualTo(TipoCliente.ClienteProveedor));

            // Quitar proveedor
            r = r.QuitarProveedor();
            Assert.That(r, Is.EqualTo(TipoCliente.SoloCliente));

            // Idempotencia
            Assert.That(r.AgregarCliente(), Is.EqualTo(TipoCliente.SoloCliente));
            Assert.That(r.QuitarProveedor(), Is.EqualTo(TipoCliente.SoloCliente));
        }


        [Test]
        public void Guards_DeOperacion_SegunRol()
        {
            // Solo Cliente: venta OK, compra falla
            var c = TipoCliente.SoloCliente;
            Assert.That(() => c.AsegurarPuedeEmitirComprobanteVenta(), Throws.Nothing);
            Assert.That(() => c.AsegurarPuedeRegistrarCompra(), Throws.TypeOf<BusinessRuleException>());

            // Solo Proveedor: venta falla, compra OK
            var p = TipoCliente.SoloProveedor;
            Assert.That(() => p.AsegurarPuedeEmitirComprobanteVenta(), Throws.TypeOf<BusinessRuleException>());
            Assert.That(() => p.AsegurarPuedeRegistrarCompra(), Throws.Nothing);

            // Ambos: todo OK
            var cp = TipoCliente.ClienteProveedor;
            Assert.That(() => cp.AsegurarPuedeEmitirComprobanteVenta(), Throws.Nothing);
            Assert.That(() => cp.AsegurarPuedeRegistrarCompra(), Throws.Nothing);
        }


    // No aplica en TipoCliente: no existe el estado "Ninguno" ni el método AsegurarTieneAlMenosUnRol


        [Test]
        public void IgualdadPorValor()
        {
            var a = TipoCliente.DesdeCodigo("cp");
            var b = TipoCliente.ClienteProveedor;
            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
            Assert.That(a.ToString(), Is.EqualTo("Cliente/Proveedor"));
        }
    }
}
