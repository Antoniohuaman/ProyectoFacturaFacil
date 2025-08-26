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
        public void Inhabilitado_PropiedadesBasicas()
        {
            var e = EstadoCliente.Inhabilitado;
            Assert.That(e.Codigo, Is.EqualTo(EstadoCliente.CodigoInhabilitado));
            Assert.That(e.Nombre, Is.EqualTo("Inhabilitado"));
            Assert.That(e.EsHabilitado, Is.False);
            Assert.That(e.PermiteOperaciones, Is.False);
            Assert.That(e.ToString(), Is.EqualTo("Inhabilitado"));
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

        [TestCase("inh")]
        [TestCase("  INH  ")]
        [TestCase("INH")]
        public void DesdeCodigo_INH_DevuelveInhabilitado(string codigo)
        {
            var e = EstadoCliente.DesdeCodigo(codigo);
            Assert.That(e, Is.SameAs(EstadoCliente.Inhabilitado));
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
            Assert.That(EstadoCliente.DesdeBool(false), Is.SameAs(EstadoCliente.Inhabilitado));
        }

        [Test]
        public void AsegurarOperacionPermitida_LanzaSiInhabilitado()
        {
            var e = EstadoCliente.Inhabilitado;
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
            var b = EstadoCliente.Inhabilitado;
            Assert.That(a, Is.Not.EqualTo(b));
        }
    }
}
