using NUnit.Framework;
using SharedKernel.ValueObjects;
using SharedKernel.Exceptions;

namespace SharedKernel.Tests.ValueObjects
{
    [TestFixture]
    public class EmpresaIdTests
    {
        [Test]
        public void From_RecortaYConservaElValor()
        {
            var id = EmpresaId.From(" 20000000001 ");
            Assert.That(id.Value, Is.EqualTo("20000000001"));
            Assert.That(id.Valor, Is.EqualTo("20000000001")); // alias
            Assert.That(id.IsEmpty, Is.False);
            Assert.That(id.ToString(), Is.EqualTo("20000000001"));
        }

        [Test]
        public void From_LanzaSiNuloOVacio()
        {
            Assert.That(() => EmpresaId.From(null), Throws.TypeOf<BusinessRuleException>());
            Assert.That(() => EmpresaId.From(""), Throws.TypeOf<BusinessRuleException>());
            Assert.That(() => EmpresaId.From("   "), Throws.TypeOf<BusinessRuleException>());
        }

        [Test]
        public void TryParse_NoLanzaYRetornaFalseParaVacios()
        {
            var ok = EmpresaId.TryParse(" ACME-01 ", out var id);
            Assert.That(ok, Is.True);
            Assert.That(id!, Is.Not.Null);
            Assert.That(id!.Value, Is.EqualTo("ACME-01"));

            var bad = EmpresaId.TryParse("   ", out var id2);
            Assert.That(bad, Is.False);
            Assert.That(id2, Is.Null);
        }

        [Test]
        public void IgualdadPorValor_EsCorrecta()
        {
            var a = EmpresaId.From("20000000001");
            var b = EmpresaId.From("20000000001");
            var c = EmpresaId.From("20000000002");

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.EsMismaEmpresaQue(b), Is.True);
            Assert.That(a, Is.Not.EqualTo(c));
            Assert.That(a.EsMismaEmpresaQue(c), Is.False);
        }

        [Test]
        public void Conversiones_ExplicitaEImplicita_Funcionan()
        {
            // explícita string -> EmpresaId
            var id = (EmpresaId)"  ABC  ";
            Assert.That(id.Value, Is.EqualTo("ABC"));

            // implícita EmpresaId -> string
            string s = id;
            Assert.That(s, Is.EqualTo("ABC"));
        }
    }
}
