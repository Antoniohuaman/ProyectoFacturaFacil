using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConfiguracionSistemaBC.Application.UseCases;
using ConfiguracionSistemaBC.Domain.Aggregates;
using ConfiguracionSistemaBC.Domain.Repositories;
using ConfiguracionSistemaBC.Application.Interfaces; // IUnitOfWork
using ConfiguracionSistemaBC.Domain.ValueObjects; // Ruc
using Moq;
using NUnit.Framework;
using SharedKernel.Application.Interfaces;         // ITenantContext
using SharedKernel.ValueObjects;                   // DomicilioFiscal, Moneda, EmpresaId

namespace ConfiguracionSistemaBC.Tests.Application.UseCases
{
    [TestFixture]
    public class RegistrarUnidadDeMedidaUseCaseTests
    {
        private static ConfiguracionEmpresa NuevaEmpresaBootstrap()
        {
            // RUC ejemplo solicitado
            var ruc = Ruc.From("20600893409");

            var dom = DomicilioFiscal.FromPeru(
                linea: "Av. Siempre Viva 742",
                ubigeo: "150101",
                departamento: null,
                provincia: null,
                distrito: null,
                addressTypeCode: null
            );

            // Bootstrap incluye unidades del sistema (NIU default, ZZ, KGM, GRM, LTR, MTR, etc.)
            var empresa = ConfiguracionEmpresa.RegistrarNueva(ruc, "ACME S.A.C.", dom, Moneda.PEN());
            return empresa;
        }

        [Test]
        public async Task Registra_unidades_personalizadas_y_establece_default()
        {
            // Arrange
            var empresa = NuevaEmpresaBootstrap();
            var empresaId = empresa.EmpresaId;

            var repo = new Mock<IConfiguracionEmpresaRepository>(MockBehavior.Strict);
            repo.Setup(r => r.GetByEmpresaIdAsync(empresaId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(empresa);
            repo.Setup(r => r.UpdateAsync(empresa, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

                var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
                uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            tenant.SetupGet(t => t.EmpresaId).Returns(empresaId);

            var useCase = new RegistrarUnidadDeMedidaUseCase(repo.Object, uow.Object, tenant.Object);

            var input = new RegistrarUnidadDeMedidaInputDto
            {
                Items =
                {
                    new RegistrarUnidadDeMedidaInputDto.Item
                    {
                        UnidadCodigo = "CMT", Nombre = "CENTÍMETRO", Visible = true, EsPorDefecto = false
                    },
                    new RegistrarUnidadDeMedidaInputDto.Item
                    {
                        UnidadCodigo = "MMT", Nombre = "MILÍMETRO", Visible = true, EsPorDefecto = true
                    }
                }
            };

            var versionOriginal = empresa.Version;

            // Act
            var outDto = await useCase.HandleAsync(input, CancellationToken.None);

            // Assert
            Assert.That(outDto.EmpresaId, Is.EqualTo(empresa.EmpresaId.Value));
            Assert.That(outDto.Creadas, Has.Count.EqualTo(2));
            Assert.That(outDto.TotalUnidades, Is.GreaterThan(0));
            Assert.That(outDto.UnidadDefaultId, Is.Not.Null);

            // La unidad por defecto ahora debe ser "MILÍMETRO" (MMT)
            var umDefault = empresa.ObtenerUnidadDeMedidaPorDefecto();
            Assert.That(umDefault, Is.Not.Null);
            Assert.That(umDefault!.Unidad.Codigo, Is.EqualTo("MMT"));
            Assert.That(umDefault!.Nombre, Is.EqualTo("MILÍMETRO"));

            // Validación DTO de “CENTÍMETRO”
            var cmt = outDto.Creadas.FirstOrDefault(c => c.UnidadCodigo == "CMT");
            Assert.That(cmt, Is.Not.Null);
            Assert.That(cmt!.Nombre, Is.EqualTo("CENTÍMETRO"));
            Assert.That(cmt.EsSistema, Is.False);

            repo.Verify(r => r.UpdateAsync(empresa, It.IsAny<CancellationToken>()), Times.Once);
            repo.VerifyAll();
            uow.VerifyAll();
            tenant.VerifyAll();
        }

        [Test]
        public void Rechaza_multiples_default_en_el_mismo_lote()
        {
            var empresa = NuevaEmpresaBootstrap();
            var empresaId = empresa.EmpresaId;

            var repo = new Mock<IConfiguracionEmpresaRepository>(MockBehavior.Strict);
            repo.Setup(r => r.GetByEmpresaIdAsync(empresaId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(empresa);

            var uow = new Mock<IUnitOfWork>(MockBehavior.Loose);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            tenant.SetupGet(t => t.EmpresaId).Returns(empresaId);

            var useCase = new RegistrarUnidadDeMedidaUseCase(repo.Object, uow.Object, tenant.Object);

            var input = new RegistrarUnidadDeMedidaInputDto
            {
                Items =
                {
                    new RegistrarUnidadDeMedidaInputDto.Item { UnidadCodigo = "CMT", Nombre = "CENTÍMETRO", EsPorDefecto = true },
                    new RegistrarUnidadDeMedidaInputDto.Item { UnidadCodigo = "MMT", Nombre = "MILÍMETRO", EsPorDefecto = true },
                }
            };

            var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
                useCase.HandleAsync(input, CancellationToken.None));

            Assert.That(ex!.Message, Does.Contain("Solo puedes marcar una unidad de medida como 'por defecto'"));
            repo.VerifyAll();
            tenant.VerifyAll();
        }

        [Test]
        public void Rechaza_default_en_item_oculto()
        {
            var empresa = NuevaEmpresaBootstrap();
            var empresaId = empresa.EmpresaId;

            var repo = new Mock<IConfiguracionEmpresaRepository>(MockBehavior.Strict);
            repo.Setup(r => r.GetByEmpresaIdAsync(empresaId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(empresa);

            var uow = new Mock<IUnitOfWork>(MockBehavior.Loose);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            tenant.SetupGet(t => t.EmpresaId).Returns(empresaId);

            var useCase = new RegistrarUnidadDeMedidaUseCase(repo.Object, uow.Object, tenant.Object);

            var input = new RegistrarUnidadDeMedidaInputDto
            {
                Items =
                {
                    new RegistrarUnidadDeMedidaInputDto.Item
                    {
                        UnidadCodigo = "CMT", Nombre = "CENTÍMETRO", Visible = false, EsPorDefecto = true
                    }
                }
            };

            var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
                useCase.HandleAsync(input, CancellationToken.None));

            Assert.That(ex!.Message, Does.Contain("por defecto una unidad de medida oculta"));
            repo.VerifyAll();
            tenant.VerifyAll();
        }

        [Test]
        public void Rechaza_codigo_duplicado_contra_bootstrap_del_sistema()
        {
            // Bootstrap trae NIU (UNIDAD) del sistema.
            var empresa = NuevaEmpresaBootstrap();
            var empresaId = empresa.EmpresaId;

            var repo = new Mock<IConfiguracionEmpresaRepository>(MockBehavior.Strict);
            repo.Setup(r => r.GetByEmpresaIdAsync(empresaId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(empresa);

            var uow = new Mock<IUnitOfWork>(MockBehavior.Loose);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            tenant.SetupGet(t => t.EmpresaId).Returns(empresaId);

            var useCase = new RegistrarUnidadDeMedidaUseCase(repo.Object, uow.Object, tenant.Object);

            var input = new RegistrarUnidadDeMedidaInputDto
            {
                Items =
                {
                    // Intento de agregar NIU (ya existe por sistema)
                    new RegistrarUnidadDeMedidaInputDto.Item { UnidadCodigo = "NIU", Nombre = "UNIDAD" }
                }
            };

            var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
                useCase.HandleAsync(input, CancellationToken.None));

            Assert.That(ex!.Message, Does.Contain("ya existe").IgnoreCase);
            repo.VerifyAll();
            tenant.VerifyAll();
        }

        [Test]
        public void Lanza_si_no_hay_empresa_en_contexto_y_no_se_envia_en_input()
        {
            var repo = new Mock<IConfiguracionEmpresaRepository>(MockBehavior.Loose);
            var uow = new Mock<IUnitOfWork>(MockBehavior.Loose);

            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            tenant.SetupGet(t => t.EmpresaId).Throws(new InvalidOperationException("No hay EmpresaId en el contexto."));

            var useCase = new RegistrarUnidadDeMedidaUseCase(repo.Object, uow.Object, tenant.Object);

            var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
                useCase.HandleAsync(new RegistrarUnidadDeMedidaInputDto
                {
                    Items =
                    {
                        new RegistrarUnidadDeMedidaInputDto.Item { UnidadCodigo = "CMT", Nombre = "CENTÍMETRO" }
                    }
                }, CancellationToken.None));

            Assert.That(ex!.Message, Does.Contain("EmpresaId"));
        }
    }
}
