using NUnit.Framework;
using GestionClientesBC.Domain.ValueObjects;
using SharedKernel.Exceptions;

namespace GestionClientesBC.Tests.ValueObjects
{
    [TestFixture]
    public class RolClienteTests
    {

        [Test]
        public void Instancias_Basicas_Singletons()
        {
            Assert.That(RolCliente.Mayorista.Codigo, Is.EqualTo(RolCliente.COD_MAY));
            Assert.That(RolCliente.Mayorista.Nombre, Is.EqualTo("Mayorista"));

            Assert.That(RolCliente.Minorista.Codigo, Is.EqualTo(RolCliente.COD_MIN));
            Assert.That(RolCliente.Distribuidor.Codigo, Is.EqualTo(RolCliente.COD_DIS));
            Assert.That(RolCliente.Revendedor.Codigo, Is.EqualTo(RolCliente.COD_REV));
            Assert.That(RolCliente.SinDefinir.Codigo, Is.EqualTo(RolCliente.COD_SIN));
        }


        [TestCase("may", "Mayorista")]
        [TestCase("MIN", "Minorista")]
        [TestCase("dis", "Distribuidor")]
        [TestCase("REV", "Revendedor")]
        [TestCase("sin", "Sin definir")]
        public void DesdeCodigo_MapeaCorrectamente(string codigo, string nombreEsperado)
        {
            var t = RolCliente.DesdeCodigo(codigo);
            Assert.That(t.Nombre, Is.EqualTo(nombreEsperado));
        }


        [TestCase("Mayorista",   RolCliente.COD_MAY)]
        [TestCase("minorista",   RolCliente.COD_MIN)]
        [TestCase("Distribuidor",RolCliente.COD_DIS)]
        [TestCase("revendedor",  RolCliente.COD_REV)]
        [TestCase("Sin definir", RolCliente.COD_SIN)]
        public void DesdeNombre_MapeaCorrectamente(string nombre, string codigoEsperado)
        {
            var t = RolCliente.DesdeNombre(nombre);
            Assert.That(t.Codigo, Is.EqualTo(codigoEsperado));
        }


        [TestCase("")]
        [TestCase(" ")]
        [TestCase(null)]
        [TestCase("vip")]
        public void Invalidos_Lanzan(string? input)
        {
            Assert.That(() => RolCliente.DesdeCodigo(input!), Throws.TypeOf<BusinessRuleException>());
            Assert.That(() => RolCliente.DesdeNombre(input!), Throws.TypeOf<BusinessRuleException>());
        }


        [Test]
        public void IgualdadPorValor_MismoCodigo_Iguales()
        {
            var a = RolCliente.DesdeCodigo("MAY");
            var b = RolCliente.Mayorista;
            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }


        [Test]
        public void ToString_RetornaNombre()
        {
            Assert.That(RolCliente.Distribuidor.ToString(), Is.EqualTo("Distribuidor"));
        }
    }
}
