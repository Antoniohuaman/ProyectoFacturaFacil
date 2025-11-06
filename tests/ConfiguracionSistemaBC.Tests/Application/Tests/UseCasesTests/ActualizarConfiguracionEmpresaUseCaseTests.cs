using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic; // Para KeyNotFoundException
using ConfiguracionSistemaBC.Application.UseCases;
using ConfiguracionSistemaBC.Domain.Aggregates;
using ConfiguracionSistemaBC.Domain.Repositories;
using ConfiguracionSistemaBC.Application.Interfaces; // IUnitOfWork
using ConfiguracionSistemaBC.Domain.ValueObjects; // Ruc
using Moq;
using NUnit.Framework;
using SharedKernel.Application.Interfaces;         // ITenantContext
using SharedKernel.ValueObjects;                   // DomicilioFiscal, Moneda, EmpresaId, TenantId

namespace ConfiguracionSistemaBC.Tests.Application.UseCases
{
    [TestFixture]
    public class ActualizarConfiguracionEmpresaUseCaseTests
    {
        private static ConfiguracionEmpresa NuevaEmpresaBootstrap(out EmpresaId empresaId)
        {
            var ruc = Ruc.From("20600893409"); // RUC ejemplo
            var dom = DomicilioFiscal.FromPeru(
                linea: "Av. Siempre Viva 742",
                ubigeo: "150101",
                departamento: null,
                provincia: null,
                distrito: null,
                addressTypeCode: null
            );

            var empresa = ConfiguracionSistemaBC.Domain.Aggregates.ConfiguracionEmpresa
                .RegistrarNueva(ruc, "ACME S.A.C.", dom, Moneda.PEN());

            empresaId = empresa.EmpresaId;
            return empresa;
        }

        [Test]
        public async Task Actualiza_preferencias_y_moneda_correctamente()
        {
            // Arrange
            var empresa = NuevaEmpresaBootstrap(out var empresaId);
            var repo = new Mock<IConfiguracionEmpresaRepository>(MockBehavior.Strict);
            var versionOriginal = empresa.Version;
            repo.Setup(r => r.GetByEmpresaIdAsync(empresaId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(empresa);
            repo.Setup(r => r.UpdateIfVersionMatchAsync(empresa, versionOriginal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

                var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
                uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

            var tenant = new Mock<ITenantContext>(MockBehavior.Loose);
            tenant.SetupGet(t => t.EmpresaId).Returns(empresaId);
            tenant.SetupGet(t => t.TenantId).Returns(TenantId.New());

            var useCase = new ActualizarConfiguracionEmpresaUseCase(repo.Object, uow.Object, tenant.Object);

            var input = new ActualizarConfiguracionEmpresaInputDto
            {
                Telefono = "999-888-777",
                Emails = new() { "admin@acme.com", "ventas@acme.com" },
                PieDePagina = "Gracias por su preferencia",
                MostrarImagenEnComprobanteImpresa = true,
                MonedaCodigo = "USD"
            };


            // Act
            var outDto = await useCase.HandleAsync(input, CancellationToken.None);

            // Assert: snapshot
            Assert.That(outDto.EmpresaId, Is.EqualTo(empresa.EmpresaId.Value));
            Assert.That(outDto.Ruc, Is.EqualTo("20600893409"));
            Assert.That(outDto.MonedaBaseCodigo, Is.EqualTo("USD"));
            Assert.That(outDto.Telefono, Does.Contain("999"));
            Assert.That(outDto.Emails, Has.Length.EqualTo(2));
            Assert.That(outDto.PieDePagina, Does.Contain("Gracias por su preferencia"));
            Assert.That(outDto.MostrarImagenEnComprobanteImpresa, Is.True);

            // Assert: estado agregado
            Assert.That(empresa.MonedaBase.Codigo, Is.EqualTo("USD"));
            Assert.That(empresa.Emails.Select(e => e.Value).ToArray(), Is.EquivalentTo(new[] { "admin@acme.com", "ventas@acme.com" }));

            repo.Verify(r => r.UpdateIfVersionMatchAsync(empresa, versionOriginal, It.IsAny<CancellationToken>()), Times.Once);
            repo.VerifyAll();
            uow.VerifyAll();
        }

        [Test]
        public async Task Actualiza_datos_legales_sin_cambiar_RUC()
        {
            // Arrange
            var empresa = NuevaEmpresaBootstrap(out var empresaId);
            var repo = new Mock<IConfiguracionEmpresaRepository>(MockBehavior.Strict);
            var versionOriginal = empresa.Version;
            repo.Setup(r => r.GetByEmpresaIdAsync(empresaId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(empresa);
            repo.Setup(r => r.UpdateIfVersionMatchAsync(empresa, versionOriginal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

                var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
                uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

            var tenant = new Mock<ITenantContext>(MockBehavior.Loose);
            tenant.SetupGet(t => t.EmpresaId).Returns(empresaId);
            tenant.SetupGet(t => t.TenantId).Returns(TenantId.New());

            var useCase = new ActualizarConfiguracionEmpresaUseCase(repo.Object, uow.Object, tenant.Object);

            var input = new ActualizarConfiguracionEmpresaInputDto
            {
                RazonSocial = "ACME S.A.C. Renovada",
                NombreComercial = "ACME Store",
                DireccionFiscal = new ActualizarConfiguracionEmpresaInputDto.DireccionFiscalDto
                {
                    PaisCodigo = "PE",
                    Ubigeo = "150102",
                    Direccion = "Av. Los Olivos 123"
                }
            };


            // Act
            var outDto = await useCase.HandleAsync(input, CancellationToken.None);

            // Assert
            Assert.That(outDto.Ruc, Is.EqualTo("20600893409")); // RUC inmutable
            Assert.That(outDto.RazonSocial, Is.EqualTo("ACME S.A.C. Renovada"));
            Assert.That(outDto.NombreComercial, Is.EqualTo("ACME Store"));
            Assert.That(outDto.DireccionFiscal.Ubigeo, Is.EqualTo("150102"));
            Assert.That(outDto.DireccionFiscal.Direccion, Is.EqualTo("Av. Los Olivos 123"));

            repo.Verify(r => r.UpdateIfVersionMatchAsync(empresa, versionOriginal, It.IsAny<CancellationToken>()), Times.Once);
            repo.VerifyAll();
            uow.VerifyAll();
        }

        [Test]
        public async Task Usa_EmpresaId_del_input_cuando_se_provee()
        {
            // Arrange
            var empresa = NuevaEmpresaBootstrap(out var empresaIdReal);
            var empresaIdInput = EmpresaId.From(Guid.NewGuid().ToString()); // diferente al del contexto

            var repo = new Mock<IConfiguracionEmpresaRepository>(MockBehavior.Strict);
            var versionOriginal = empresa.Version;
            repo.Setup(r => r.GetByEmpresaIdAsync(empresaIdInput, It.IsAny<CancellationToken>()))
                .ReturnsAsync(empresa);
            repo.Setup(r => r.UpdateIfVersionMatchAsync(empresa, versionOriginal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

                var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
                uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

            var tenant = new Mock<ITenantContext>(MockBehavior.Loose);
            tenant.SetupGet(t => t.EmpresaId).Returns(empresaIdReal); // contexto trae otro
            tenant.SetupGet(t => t.TenantId).Returns(TenantId.New());

            var useCase = new ActualizarConfiguracionEmpresaUseCase(repo.Object, uow.Object, tenant.Object);

            var input = new ActualizarConfiguracionEmpresaInputDto
            {
                EmpresaId = empresaIdInput.Value,
                PieDePagina = "Footer actualizado"
            };


            // Act
            var outDto = await useCase.HandleAsync(input, CancellationToken.None);

            // Assert: el use case buscó por el EmpresaId del INPUT (no el del contexto)
            Assert.That(outDto.EmpresaId, Is.EqualTo(empresa.EmpresaId.Value));
            Assert.That(outDto.PieDePagina, Does.Contain("actualizado"));

            repo.Verify(r => r.GetByEmpresaIdAsync(empresaIdInput, It.IsAny<CancellationToken>()), Times.Once);
            repo.Verify(r => r.UpdateIfVersionMatchAsync(empresa, versionOriginal, It.IsAny<CancellationToken>()), Times.Once);
            repo.VerifyNoOtherCalls();
            uow.VerifyAll();
                    }

        [Test]
        public void Lanza_si_no_hay_empresa_en_contexto_y_no_se_envia_en_input()
        {
            var repo = new Mock<IConfiguracionEmpresaRepository>(MockBehavior.Loose);
            var uow = new Mock<IUnitOfWork>(MockBehavior.Loose);
            var tenant = new Mock<ITenantContext>(MockBehavior.Loose);
            tenant.SetupGet(t => t.EmpresaId).Throws(new InvalidOperationException("EmpresaId no disponible en contexto"));
            tenant.SetupGet(t => t.TenantId).Returns(TenantId.New());

            var useCase = new ActualizarConfiguracionEmpresaUseCase(repo.Object, uow.Object, tenant.Object);

            var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
                useCase.HandleAsync(new ActualizarConfiguracionEmpresaInputDto
                {
                    PieDePagina = "algo"
                }, CancellationToken.None));

            Assert.That(ex!.Message, Does.Contain("EmpresaId"));
        }

        [Test]
        public void Lanza_si_no_existe_la_configuracion()
        {
            var anyEmpresaId = EmpresaId.From(Guid.NewGuid().ToString());
            var repo = new Mock<IConfiguracionEmpresaRepository>(MockBehavior.Strict);
            repo.Setup(r => r.GetByEmpresaIdAsync(anyEmpresaId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ConfiguracionEmpresa?)null);

            var uow = new Mock<IUnitOfWork>(MockBehavior.Loose);
            var tenant = new Mock<ITenantContext>(MockBehavior.Loose);
            tenant.SetupGet(t => t.EmpresaId).Returns(anyEmpresaId);
            tenant.SetupGet(t => t.TenantId).Returns(TenantId.New());

            var useCase = new ActualizarConfiguracionEmpresaUseCase(repo.Object, uow.Object, tenant.Object);

            var ex = Assert.ThrowsAsync<KeyNotFoundException>(() =>
                useCase.HandleAsync(new ActualizarConfiguracionEmpresaInputDto
                {
                    PieDePagina = "x"
                }, CancellationToken.None));

            Assert.That(ex!.Message, Does.Contain("No se encontró la configuración"));
            repo.VerifyAll();
        }

        [Test]
        public async Task Permite_vaciar_lista_de_emails()
        {
            // Arrange
            var empresa = NuevaEmpresaBootstrap(out var empresaId);
            // precargar emails
            empresa.ReemplazarEmails(new[] { Email.Create("uno@acme.com"), Email.Create("dos@acme.com") }.ToList());

            var versionOriginal = empresa.Version;
            var repo = new Mock<IConfiguracionEmpresaRepository>(MockBehavior.Strict);
            repo.Setup(r => r.GetByEmpresaIdAsync(empresaId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(empresa);
            repo.Setup(r => r.UpdateIfVersionMatchAsync(empresa, versionOriginal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

                var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
                uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

            var tenant = new Mock<ITenantContext>(MockBehavior.Loose);
            tenant.SetupGet(t => t.EmpresaId).Returns(empresaId);
            tenant.SetupGet(t => t.TenantId).Returns(TenantId.New());

            var useCase = new ActualizarConfiguracionEmpresaUseCase(repo.Object, uow.Object, tenant.Object);

            var input = new ActualizarConfiguracionEmpresaInputDto
            {
                Emails = new() // vacía
            };

            // Act
            var outDto = await useCase.HandleAsync(input, CancellationToken.None);

            // Assert: emails vaciados
            Assert.That(outDto.Emails, Is.Empty);
            Assert.That(empresa.Emails, Is.Empty);

            repo.Verify(r => r.UpdateIfVersionMatchAsync(empresa, versionOriginal, It.IsAny<CancellationToken>()), Times.Once);
            repo.VerifyAll();
            uow.VerifyAll();
        }
    }
}
