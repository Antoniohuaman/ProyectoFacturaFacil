using System;
using System.Threading;
using System.Threading.Tasks;
using ConfiguracionSistemaBC.Application.UseCases;
using ConfiguracionSistemaBC.Domain.Aggregates;
using ConfiguracionSistemaBC.Domain.Repositories;
using Moq;
using NUnit.Framework;
using SharedKernel.Application.Interfaces;           // ITenantContext
using SharedKernel.ValueObjects;                     // EmpresaId, DomicilioFiscal, Moneda
using ConfiguracionSistemaBC.Application.Interfaces; // IUnitOfWork

namespace ConfiguracionSistemaBC.Tests.Application.UseCases
{
    [TestFixture]
    public class RegistrarEstablecimientoUseCaseTests
    {
        private static ConfiguracionEmpresa NuevaEmpresa()
        {
            // Empresa bootstrap con "01" Establecimiento Principal
            var empresa = ConfiguracionEmpresa.RegistrarNueva(
                ConfiguracionSistemaBC.Domain.ValueObjects.Ruc.From("20600893409"),
                "ACME S.A.C.",
                DomicilioFiscal.FromPeru(
                    linea: "Av. Lima 123",
                    ubigeo: "150101",
                    departamento: null,
                    provincia: null,
                    distrito: null,
                    addressTypeCode: null
                ),
                Moneda.PEN()
            );
            return empresa;
        }

        private static RegistrarEstablecimientoInputDto InputOk(string codigo = "02", string nombre = "Tienda Centro")
        {
            return new RegistrarEstablecimientoInputDto
            {
                Codigo = codigo,
                Nombre = nombre,
                Direccion = new RegistrarEstablecimientoInputDto.DireccionFiscalDto
                {
                    PaisCodigo = "PE",
                    Ubigeo = "150102",
                    Direccion = "Jr. Cusco 456",
                    Referencia = "Frente al mercado"
                }
            };
        }

        [Test]
        public async Task Registra_establecimiento_y_no_es_principal_si_no_se_solicita()
        {
            // Arrange
            var empresa = NuevaEmpresa();
            var empresaId = empresa.EmpresaId;

            var repo = new Mock<IConfiguracionEmpresaRepository>(MockBehavior.Strict);
            repo.Setup(r => r.GetByEmpresaIdAsync(empresaId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(empresa);

            repo.Setup(r => r.UpdateIfVersionMatchAsync(empresa, It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
                uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            tenant.SetupGet(t => t.EmpresaId).Returns(empresaId);
            tenant.SetupGet(t => t.TenantId).Returns(TenantId.From(System.Guid.NewGuid())); // valor dummy

            var useCase = new RegistrarEstablecimientoUseCase(repo.Object, uow.Object, tenant.Object);
            var input = InputOk();

            // Act
            var output = await useCase.HandleAsync(input, CancellationToken.None);

            // Assert
            Assert.That(output.EmpresaId, Is.EqualTo(empresa.EmpresaId.Value));
            Assert.That(output.Codigo, Is.EqualTo("02"));
            Assert.That(output.Nombre, Is.EqualTo("Tienda Centro"));
            Assert.That(output.Direccion, Is.EqualTo("Jr. Cusco 456"));
            Assert.That(output.Ubigeo, Is.EqualTo("150102"));
            Assert.That(output.EstablecimientoId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(output.EsPrincipal, Is.False);

            repo.VerifyAll();
            uow.VerifyAll();
            // tenant.VerifyAll(); // No se verifica TenantId porque no se usa en el flujo
        }



        [Test]
        public void Lanza_si_duplicado_por_codigo()
        {
            // Arrange: empresa ya tiene "01" por bootstrap
            var empresa = NuevaEmpresa();
            var empresaId = empresa.EmpresaId;

            var repo = new Mock<IConfiguracionEmpresaRepository>(MockBehavior.Strict);
            repo.Setup(r => r.GetByEmpresaIdAsync(empresaId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(empresa);

            var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);

            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            tenant.SetupGet(t => t.EmpresaId).Returns(empresaId);
            tenant.SetupGet(t => t.TenantId).Returns(TenantId.From(System.Guid.NewGuid())); // valor dummy

            var useCase = new RegistrarEstablecimientoUseCase(repo.Object, uow.Object, tenant.Object);

            // usar el mismo código "01"
            var input = InputOk(codigo: "01", nombre: "Otro Nombre");

            // Act
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await useCase.HandleAsync(input, CancellationToken.None));

            // Assert
            Assert.That(ex!.Message, Does.Contain("Ya existe un establecimiento"));
            repo.Verify(r => r.UpdateIfVersionMatchAsync(It.IsAny<ConfiguracionEmpresa>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public void Lanza_si_no_hay_empresa_en_contexto()
        {
            // Arrange
            var repo = new Mock<IConfiguracionEmpresaRepository>(MockBehavior.Loose);
            var uow  = new Mock<IUnitOfWork>(MockBehavior.Loose);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            tenant.SetupGet(t => t.EmpresaId).Returns((EmpresaId)null!); // Forzar null para test
            tenant.SetupGet(t => t.TenantId).Returns(TenantId.From(System.Guid.NewGuid())); // valor dummy

            var useCase = new RegistrarEstablecimientoUseCase(repo.Object, uow.Object, tenant.Object);

            // Act
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await useCase.HandleAsync(new RegistrarEstablecimientoInputDto
                {
                    Codigo = "02",
                    Nombre = "Tienda Centro",
                    Direccion = new RegistrarEstablecimientoInputDto.DireccionFiscalDto
                    {
                        PaisCodigo = "PE",
                        Ubigeo = "150102",
                        Direccion = "X"
                    }
                }, CancellationToken.None));

            // Assert
            Assert.That(ex!.Message, Does.Contain("No hay EmpresaId en el contexto"));
        }

        [Test]
        public void Lanza_por_concurrencia_si_version_no_coincide()
        {
            // Arrange
            var empresa = NuevaEmpresa();
            var empresaId = empresa.EmpresaId;

            var repo = new Mock<IConfiguracionEmpresaRepository>(MockBehavior.Strict);
            repo.Setup(r => r.GetByEmpresaIdAsync(empresaId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(empresa);

            // Simular conflicto de versión
            repo.Setup(r => r.UpdateIfVersionMatchAsync(empresa, It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var uow = new Mock<IUnitOfWork>(MockBehavior.Loose);

            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            tenant.SetupGet(t => t.EmpresaId).Returns(empresaId);
            tenant.SetupGet(t => t.TenantId).Returns(TenantId.From(System.Guid.NewGuid())); // valor dummy

            var useCase = new RegistrarEstablecimientoUseCase(repo.Object, uow.Object, tenant.Object);
            var input = InputOk();

            // Act
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await useCase.HandleAsync(input, CancellationToken.None));

            // Assert
            Assert.That(ex!.Message, Does.Contain("concurrencia"));
        }
    }
}
