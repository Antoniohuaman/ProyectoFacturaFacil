using System;
using System.Threading;
using System.Threading.Tasks;
using ConfiguracionSistemaBC.Application.Ports;
using ConfiguracionSistemaBC.Application.UseCases;
using ConfiguracionSistemaBC.Domain.Aggregates;
using ConfiguracionSistemaBC.Domain.Repositories;
using ConfiguracionSistemaBC.Domain.ValueObjects;    // AmbienteFe
using Moq;
using NUnit.Framework;
using SharedKernel.Application.Interfaces;           // ITenantContext
using SharedKernel.ValueObjects;                     // DomicilioFiscal, Moneda, EmpresaId

namespace ConfiguracionSistemaBC.Tests.Application.UseCases
{
    [TestFixture]
    public class CambiarAmbienteEmpresaUseCaseTests
    {
        private static ConfiguracionEmpresa NuevaEmpresaPrueba()
        {
            // RUC de ejemplo solicitado: 20600893409
            var ruc = ConfiguracionSistemaBC.Domain.ValueObjects.Ruc.From("20600893409");
            var dom = DomicilioFiscal.FromPeru(
                linea: "Av. Siempre Viva 742",
                ubigeo: "150101",
                departamento: null,
                provincia: null,
                distrito: null,
                addressTypeCode: null
            );
            var empresa = ConfiguracionEmpresa.RegistrarNueva(ruc, "ACME S.A.C.", dom, Moneda.PEN());
            // Por factory, ambiente inicia en PRUEBA
            Assert.That(empresa.Ambiente, Is.EqualTo(AmbienteFe.PRUEBA));
            return empresa;
        }

        [Test]
        public async Task Cambia_a_PRODUCCION_y_purga_documentos_de_prueba()
        {
            // Arrange
            var empresa = NuevaEmpresaPrueba();
            var empresaId = empresa.EmpresaId;

            var repo = new Mock<IConfiguracionEmpresaRepository>(MockBehavior.Strict);
            repo.Setup(r => r.GetByEmpresaIdAsync(empresaId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(empresa);
            repo.Setup(r => r.UpdateIfVersionMatchAsync(empresa, It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

            var purge = new Mock<IDocumentosElectronicosPurgeService>(MockBehavior.Strict);
            purge.Setup(p => p.PurgeTestDocumentsAsync(empresaId, It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            tenant.SetupGet(t => t.EmpresaId).Returns(empresaId);
            tenant.SetupGet(t => t.TenantId).Returns(TenantId.From(System.Guid.NewGuid())); // valor dummy

            var useCase = new CambiarAmbienteEmpresaUseCase(repo.Object, uow.Object, tenant.Object, purge.Object);

            var input = new CambiarAmbienteEmpresaInputDto
            {
                Destino = "PRODUCCION",
                BorrarDocumentosEmitidosEnPrueba = true
            };

            // Act
            var output = await useCase.HandleAsync(input, CancellationToken.None);

            // Assert
            Assert.That(output.EmpresaId, Is.EqualTo(empresa.EmpresaId.Value));
            Assert.That(output.AmbienteAnterior, Is.EqualTo("PRUEBA"));
            Assert.That(output.AmbienteActual, Is.EqualTo("PRODUCCION"));
            Assert.That(output.PurgaEjecutada, Is.True);
            Assert.That(empresa.Ambiente, Is.EqualTo(AmbienteFe.PRODUCCION));

            repo.VerifyAll();
            uow.VerifyAll();
            purge.VerifyAll();
            // tenant.VerifyAll(); // No se verifica TenantId porque no se usa
        }

        [Test]
        public async Task Idempotente_si_el_destino_es_igual_al_actual_no_actualiza_ni_purga()
        {
            // Arrange: empresa en PRUEBA
            var empresa = NuevaEmpresaPrueba();
            var empresaId = empresa.EmpresaId;

            var repo = new Mock<IConfiguracionEmpresaRepository>(MockBehavior.Strict);
            repo.Setup(r => r.GetByEmpresaIdAsync(empresaId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(empresa);

            var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var purge = new Mock<IDocumentosElectronicosPurgeService>(MockBehavior.Strict);

            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            tenant.SetupGet(t => t.EmpresaId).Returns(empresaId);
            tenant.SetupGet(t => t.TenantId).Returns(TenantId.From(System.Guid.NewGuid())); // valor dummy

            var useCase = new CambiarAmbienteEmpresaUseCase(repo.Object, uow.Object, tenant.Object, purge.Object);

            var input = new CambiarAmbienteEmpresaInputDto
            {
                Destino = "PRUEBA",
                BorrarDocumentosEmitidosEnPrueba = true
            };

            // Act
            var output = await useCase.HandleAsync(input, CancellationToken.None);

            // Assert
            Assert.That(output.AmbienteAnterior, Is.EqualTo("PRUEBA"));
            Assert.That(output.AmbienteActual, Is.EqualTo("PRUEBA"));
            Assert.That(output.PurgaEjecutada, Is.False);

            // No debería intentar update ni save ni purga
            repo.Verify(r => r.UpdateIfVersionMatchAsync(It.IsAny<ConfiguracionEmpresa>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            purge.Verify(p => p.PurgeTestDocumentsAsync(It.IsAny<EmpresaId>(), It.IsAny<CancellationToken>()), Times.Never);
            repo.VerifyAll();
            // tenant.VerifyAll(); // No se verifica TenantId porque no se usa
        }

        [Test]
        public void Rechaza_transicion_de_PRODUCCION_a_PRUEBA()
        {
            // Arrange: forzamos empresa en PRODUCCION
            var empresa = NuevaEmpresaPrueba();
            empresa.CambiarAmbiente(AmbienteFe.PRODUCCION);
            Assert.That(empresa.Ambiente, Is.EqualTo(AmbienteFe.PRODUCCION));
            var empresaId = empresa.EmpresaId;

            var repo = new Mock<IConfiguracionEmpresaRepository>(MockBehavior.Strict);
            repo.Setup(r => r.GetByEmpresaIdAsync(empresaId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(empresa);

            var uow = new Mock<IUnitOfWork>(MockBehavior.Loose);
            var purge = new Mock<IDocumentosElectronicosPurgeService>(MockBehavior.Loose);

            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            tenant.SetupGet(t => t.EmpresaId).Returns(empresaId);
            tenant.SetupGet(t => t.TenantId).Returns(TenantId.From(System.Guid.NewGuid())); // valor dummy

            var useCase = new CambiarAmbienteEmpresaUseCase(repo.Object, uow.Object, tenant.Object, purge.Object);

            var input = new CambiarAmbienteEmpresaInputDto { Destino = "PRUEBA" };

            // Act
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await useCase.HandleAsync(input, CancellationToken.None));

            // Assert: el tipo exacto depende de tu implementación de ValidarTransicion;
            // controlamos por mensaje genérico de transición inválida, si aplica.
            Assert.That(ex!.Message, Does.Contain("volver a PRUEBA").IgnoreCase);

            // No se debe persistir ni purgar
            repo.Verify(r => r.UpdateIfVersionMatchAsync(It.IsAny<ConfiguracionEmpresa>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            purge.Verify(p => p.PurgeTestDocumentsAsync(It.IsAny<EmpresaId>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public void Lanza_si_no_hay_empresa_en_contexto()
        {
            var repo = new Mock<IConfiguracionEmpresaRepository>(MockBehavior.Loose);
            var uow  = new Mock<IUnitOfWork>(MockBehavior.Loose);
            var purge = new Mock<IDocumentosElectronicosPurgeService>(MockBehavior.Loose);

            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            tenant.SetupGet(t => t.EmpresaId).Returns((EmpresaId)null!); // Forzar null para test
            tenant.SetupGet(t => t.TenantId).Returns(TenantId.From(System.Guid.NewGuid())); // valor dummy

            var useCase = new CambiarAmbienteEmpresaUseCase(repo.Object, uow.Object, tenant.Object, purge.Object);

            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await useCase.HandleAsync(new CambiarAmbienteEmpresaInputDto { Destino = "PRODUCCION" }, CancellationToken.None));

            Assert.That(ex!.Message, Does.Contain("EmpresaId"));
        }

        [Test]
        public void Lanza_por_concurrencia_si_version_no_coincide()
        {
            // Arrange
            var empresa = NuevaEmpresaPrueba();
            var empresaId = empresa.EmpresaId;

            var repo = new Mock<IConfiguracionEmpresaRepository>(MockBehavior.Strict);
            repo.Setup(r => r.GetByEmpresaIdAsync(empresaId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(empresa);
            repo.Setup(r => r.UpdateIfVersionMatchAsync(empresa, It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false); // simulamos conflicto

            var uow = new Mock<IUnitOfWork>(MockBehavior.Loose);
            var purge = new Mock<IDocumentosElectronicosPurgeService>(MockBehavior.Loose);

            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            tenant.SetupGet(t => t.EmpresaId).Returns(empresaId);
            tenant.SetupGet(t => t.TenantId).Returns(TenantId.From(System.Guid.NewGuid())); // valor dummy

            var useCase = new CambiarAmbienteEmpresaUseCase(repo.Object, uow.Object, tenant.Object, purge.Object);

            var input = new CambiarAmbienteEmpresaInputDto { Destino = "PRODUCCION" };

            // Act
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await useCase.HandleAsync(input, CancellationToken.None));

            // Assert
            Assert.That(ex!.Message, Does.Contain("Concurrencia").IgnoreCase);
        }

        [Test]
        public void Lanza_si_se_solicita_purga_y_no_hay_servicio_configurado()
        {
            // Arrange
            var empresa = NuevaEmpresaPrueba();
            var empresaId = empresa.EmpresaId;

            var repo = new Mock<IConfiguracionEmpresaRepository>(MockBehavior.Strict);
            repo.Setup(r => r.GetByEmpresaIdAsync(empresaId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(empresa);
            repo.Setup(r => r.UpdateIfVersionMatchAsync(empresa, It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            tenant.SetupGet(t => t.EmpresaId).Returns(empresaId);
            tenant.SetupGet(t => t.TenantId).Returns(TenantId.From(System.Guid.NewGuid())); // valor dummy

            // No pasamos purge service
            var useCase = new CambiarAmbienteEmpresaUseCase(repo.Object, uow.Object, tenant.Object, purgeService: null);

            var input = new CambiarAmbienteEmpresaInputDto
            {
                Destino = "PRODUCCION",
                BorrarDocumentosEmitidosEnPrueba = true
            };

            // Act
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await useCase.HandleAsync(input, CancellationToken.None));

            // Assert
            Assert.That(ex!.Message, Does.Contain("purga").IgnoreCase);
        }
    }
}
