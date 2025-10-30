using System;
using Moq;
using SharedKernel.Application.Interfaces; // ITenantContext
using SharedKernel.ValueObjects;          // EmpresaId

namespace CatalogoArticulosBC.Tests.TestUtils
{
    public static class TenantTestHelpers
    {
        public static EmpresaId AnyEmpresaId()
        {
            // Usa factory de EmpresaId si existe; ajusta si es necesario
            return EmpresaId.From("11111111-1111-1111-1111-111111111111");
        }

        public static Mock<ITenantContext> MockTenant(EmpresaId? empresaId = null)
        {
            var mock = new Moq.Mock<ITenantContext>(MockBehavior.Strict);
            mock.SetupGet(t => t.EmpresaId).Returns(empresaId ?? AnyEmpresaId());
            return mock;
        }
    }
}
