using System;
using NUnit.Framework;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace SharedKernel.Tests.ValueObjects
{
    [TestFixture]
    public class RazonSocialTests
    {
        [Test]
        public void Crear_ConDenominacionValida_NormalizaYRetornaInstancia()
        {
            var rs = RazonSocial.Crear("   INDUSTRIAS   R&G   3   HERMANOS   S.A.C.  ");

            Assert.That(rs.Valor, Is.EqualTo("INDUSTRIAS R&G 3 HERMANOS S.A.C."));
            Assert.That(rs.ToString(), Is.EqualTo(rs.Valor));
        }

        [Test]
        public void Crear_PermitePuntuacionFrecuenteYNumeros()
        {
            var rs = RazonSocial.Crear(@"Comercial Acme, S.A. (Perú) #1 / ""División A""");
            Assert.That(rs.Valor, Is.EqualTo(@"Comercial Acme, S.A. (Perú) #1 / ""División A"""));
        }

        [Test]
        public void Crear_ConCaracteresInvalidos_LanzaBusinessRuleException()
        {
            Assert.That(() => RazonSocial.Crear("Comercial ACME @ Perú"),
                Throws.TypeOf<BusinessRuleException>(), "No debe permitirse '@' en razón social.");
            Assert.That(() => RazonSocial.Crear("ACME * Perú"),
                Throws.TypeOf<BusinessRuleException>(), "No debe permitirse '*' en razón social.");
        }

        [Test]
        public void Crear_VaciaOLlenaDeEspacios_LanzaBusinessRuleException()
        {
            Assert.That(() => RazonSocial.Crear(""),
                Throws.TypeOf<BusinessRuleException>());
            Assert.That(() => RazonSocial.Crear("   "),
                Throws.TypeOf<BusinessRuleException>());
        }

        [Test]
        public void Crear_SuperaLongitudMaxima_LanzaBusinessRuleException()
        {
            var muyLarga = new string('X', 201);
            Assert.That(() => RazonSocial.Crear(muyLarga),
                Throws.TypeOf<BusinessRuleException>());
        }
    }
}
