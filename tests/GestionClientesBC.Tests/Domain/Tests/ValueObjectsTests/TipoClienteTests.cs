using NUnit.Framework;
using GestionClientesBC.Domain.ValueObjects;
using SharedKernel.Exceptions;

namespace GestionClientesBC.Tests.ValueObjects
{
    [TestFixture]
    public class TipoClienteTests
    {
        [Test]
        public void Instancias_Basicas_Singletons()
        {
            Assert.That(TipoCliente.Mayorista.Codigo, Is.EqualTo(TipoCliente.COD_MAY));
            Assert.That(TipoCliente.Mayorista.Nombre, Is.EqualTo("Mayorista"));

            Assert.That(TipoCliente.Minorista.Codigo, Is.EqualTo(TipoCliente.COD_MIN));
            Assert.That(TipoCliente.Distribuidor.Codigo, Is.EqualTo(TipoCliente.COD_DIS));
            Assert.That(TipoCliente.Revendedor.Codigo, Is.EqualTo(TipoCliente.COD_REV));
            Assert.That(TipoCliente.SinDefinir.Codigo, Is.EqualTo(TipoCliente.COD_SIN));
        }

        [TestCase("may", "Mayorista")]
        [TestCase("MIN", "Minorista")]
        [TestCase("dis", "Distribuidor")]
        [TestCase("REV", "Revendedor")]
        [TestCase("sin", "Sin definir")]
        public void DesdeCodigo_MapeaCorrectamente(string codigo, string nombreEsperado)
        {
            var t = TipoCliente.DesdeCodigo(codigo);
            Assert.That(t.Nombre, Is.EqualTo(nombreEsperado));
        }

        [TestCase("Mayorista",   TipoCliente.COD_MAY)]
        [TestCase("minorista",   TipoCliente.COD_MIN)]
        [TestCase("Distribuidor",TipoCliente.COD_DIS)]
        [TestCase("revendedor",  TipoCliente.COD_REV)]
        [TestCase("Sin definir", TipoCliente.COD_SIN)]
        public void DesdeNombre_MapeaCorrectamente(string nombre, string codigoEsperado)
        {
            var t = TipoCliente.DesdeNombre(nombre);
            Assert.That(t.Codigo, Is.EqualTo(codigoEsperado));
        }

        [TestCase("")]
        [TestCase(" ")]
        [TestCase(null)]
        [TestCase("vip")]
        public void Invalidos_Lanzan(string? input)
        {
            Assert.That(() => TipoCliente.DesdeCodigo(input!), Throws.TypeOf<BusinessRuleException>());
            Assert.That(() => TipoCliente.DesdeNombre(input!), Throws.TypeOf<BusinessRuleException>());
        }

        [Test]
        public void IgualdadPorValor_MismoCodigo_Iguales()
        {
            var a = TipoCliente.DesdeCodigo("MAY");
            var b = TipoCliente.Mayorista;
            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void ToString_RetornaNombre()
        {
            Assert.That(TipoCliente.Distribuidor.ToString(), Is.EqualTo("Distribuidor"));
        }
    }
}
