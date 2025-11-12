using NUnit.Framework;
using GestionClientesBC.Domain.ValueObjects;
using SharedKernel.Exceptions;

namespace GestionClientesBC.Tests.ValueObjects
{
    [TestFixture]
    public class EstadoClienteTests
    {
        [Test]
        public void Habilitado_PropiedadesBasicas()
        {
            var e = EstadoCliente.Habilitado;
            Assert.That(e.Codigo, Is.EqualTo(EstadoCliente.CodigoHabilitado));
            Assert.That(e.Nombre, Is.EqualTo("Habilitado"));
            Assert.That(e.EsHabilitado, Is.True);
            Assert.That(e.PermiteOperaciones, Is.True);
            Assert.That(e.ToString(), Is.EqualTo("Habilitado"));
        }

        [Test]
        public void Deshabilitado_PropiedadesBasicas()
        {
            var e = EstadoCliente.Deshabilitado;
            Assert.That(e.Codigo, Is.EqualTo(EstadoCliente.CodigoDeshabilitado));
            Assert.That(e.Nombre, Is.EqualTo("Deshabilitado"));
            Assert.That(e.EsHabilitado, Is.False);
            Assert.That(e.PermiteOperaciones, Is.False);
            Assert.That(e.ToString(), Is.EqualTo("Deshabilitado"));
        }

        [TestCase("hab")]
        [TestCase("  HAB  ")]
        [TestCase("HAB")]
        public void DesdeCodigo_HAB_DevuelveHabilitado(string codigo)
        {
            var e = EstadoCliente.DesdeCodigo(codigo);
            Assert.That(e, Is.SameAs(EstadoCliente.Habilitado));
            Assert.That(e.EsHabilitado, Is.True);
        }

        [TestCase("des")]
        [TestCase("  DES  ")]
        [TestCase("DES")]
        public void DesdeCodigo_DES_DevuelveDeshabilitado(string codigo)
        {
            var e = EstadoCliente.DesdeCodigo(codigo);
            Assert.That(e, Is.SameAs(EstadoCliente.Deshabilitado));
            Assert.That(e.PermiteOperaciones, Is.False);
        }

        [TestCase("X")]
        [TestCase("ACT")]
        [TestCase("")]
        public void DesdeCodigo_Invalido_Lanza(string codigo)
        {
            Assert.That(() => EstadoCliente.DesdeCodigo(codigo),
                Throws.TypeOf<BusinessRuleException>());
        }

        [Test]
        public void DesdeBool_TrueFalse_MapeaCorrectamente()
        {
            Assert.That(EstadoCliente.DesdeBool(true), Is.SameAs(EstadoCliente.Habilitado));
            Assert.That(EstadoCliente.DesdeBool(false), Is.SameAs(EstadoCliente.Deshabilitado));
        }

        [Test]
        public void AsegurarOperacionPermitida_LanzaSiDeshabilitado()
        {
            var e = EstadoCliente.Deshabilitado;
            Assert.That(() => e.AsegurarOperacionPermitida("emisión de comprobante"),
                Throws.TypeOf<BusinessRuleException>());
        }

        [Test]
        public void IgualdadPorValor_MismoCodigo_Iguales()
        {
            var a = EstadoCliente.DesdeCodigo("HAB");
            var b = EstadoCliente.Habilitado;
            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void IgualdadPorValor_DistintoCodigo_NoIguales()
        {
            var a = EstadoCliente.Habilitado;
            var b = EstadoCliente.Deshabilitado;
            Assert.That(a, Is.Not.EqualTo(b));
        }
    }
}
