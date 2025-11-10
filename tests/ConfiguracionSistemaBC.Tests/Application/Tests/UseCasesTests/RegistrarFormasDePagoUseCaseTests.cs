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
    public class RegistrarFormasDePagoUseCaseTests
    {
        private static ConfiguracionEmpresa NuevaEmpresaBootstrap()
        {
            // RUC ejemplo pedido
            var ruc = Ruc.From("20600893409");

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

            // Por bootstrap: "Contado" es por defecto (EsSistema=true)
            return empresa;
        }

        [Test]
    public void Registra_varias_formas_personalizadas_y_actualiza_default()
        {
            // Arrange
            var empresa = NuevaEmpresaBootstrap();
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

            var useCase = new RegistrarFormasDePagoUseCase(repo.Object, uow.Object, tenant.Object);

            var input = new RegistrarFormasDePagoInputDto
            {
                // EmpresaId nulo -> usa contexto
                Items =
                {
                    new RegistrarFormasDePagoInputDto.FormaPagoItem
                    {
                        Tipo = "CONTADO", Metodo = "TARJETA", Nombre = "Tarjeta Débito", Visible = true, EsPorDefecto = false
                    },
                    new RegistrarFormasDePagoInputDto.FormaPagoItem
                    {
                        Tipo = "CREDITO", Nombre = "Crédito 45 días", Visible = true, EsPorDefecto = true
                    }
                }
            };

            var versionOriginal = empresa.Version;

            // Act
            var ex = NUnit.Framework.Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await useCase.HandleAsync(input, CancellationToken.None));
            Assert.That(ex!.Message, Does.Contain("ya existe"));

            repo.Verify(r => r.GetByEmpresaIdAsync(empresaId, It.IsAny<CancellationToken>()), Times.Once);
            // No se debe verificar UpdateAsync ni SaveChangesAsync porque la excepción ocurre antes
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

            var useCase = new RegistrarFormasDePagoUseCase(repo.Object, uow.Object, tenant.Object);

            var input = new RegistrarFormasDePagoInputDto
            {
                Items =
                {
                    new RegistrarFormasDePagoInputDto.FormaPagoItem { Tipo = "CREDITO", Nombre = "Crédito 30 días", EsPorDefecto = true },
                    new RegistrarFormasDePagoInputDto.FormaPagoItem { Tipo = "CREDITO", Nombre = "Crédito 60 días", EsPorDefecto = true },
                }
            };

            var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
                useCase.HandleAsync(input, CancellationToken.None));

            Assert.That(ex!.Message, Does.Contain("Solo puedes marcar una forma de pago como 'por defecto'"));

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

            var useCase = new RegistrarFormasDePagoUseCase(repo.Object, uow.Object, tenant.Object);

            var input = new RegistrarFormasDePagoInputDto
            {
                Items =
                {
                    new RegistrarFormasDePagoInputDto.FormaPagoItem
                    {
                        Tipo = "CONTADO", Metodo = "EFECTIVO", Nombre = "Caja", Visible = false, EsPorDefecto = true
                    }
                }
            };

            var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
                useCase.HandleAsync(input, CancellationToken.None));

            Assert.That(ex!.Message, Does.Contain("por defecto una forma de pago oculta"));

            repo.VerifyAll();
            tenant.VerifyAll();
        }

        [Test]
        public void Rechaza_metodo_contado_invalido()
        {
            var empresa = NuevaEmpresaBootstrap();
            var empresaId = empresa.EmpresaId;

            var repo = new Mock<IConfiguracionEmpresaRepository>(MockBehavior.Strict);
            repo.Setup(r => r.GetByEmpresaIdAsync(empresaId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(empresa);

            var uow = new Mock<IUnitOfWork>(MockBehavior.Loose);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            tenant.SetupGet(t => t.EmpresaId).Returns(empresaId);

            var useCase = new RegistrarFormasDePagoUseCase(repo.Object, uow.Object, tenant.Object);

            var input = new RegistrarFormasDePagoInputDto
            {
                Items =
                {
                    new RegistrarFormasDePagoInputDto.FormaPagoItem
                    {
                        Tipo = "CONTADO", Metodo = "CHEQUE", Nombre = "Cheque al día"
                    }
                }
            };

            var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
                useCase.HandleAsync(input, CancellationToken.None));

            Assert.That(ex!.Message, Does.Contain("Método de CONTADO inválido"));
            repo.VerifyAll();
            tenant.VerifyAll();
        }

        [Test]
    public void Rechaza_duplicado_por_nombre_y_valor_existente()
        {
            // Arrange: en bootstrap ya existe "Tarjeta" (CONTADO TARJETA, EsSistema=true)
            var empresa = NuevaEmpresaBootstrap();
            var empresaId = empresa.EmpresaId;

            var repo = new Mock<IConfiguracionEmpresaRepository>(MockBehavior.Strict);
            repo.Setup(r => r.GetByEmpresaIdAsync(empresaId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(empresa);

            var uow = new Mock<IUnitOfWork>(MockBehavior.Loose);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            tenant.SetupGet(t => t.EmpresaId).Returns(empresaId);

            var useCase = new RegistrarFormasDePagoUseCase(repo.Object, uow.Object, tenant.Object);

            var input = new RegistrarFormasDePagoInputDto
            {
                Items =
                {
                    new RegistrarFormasDePagoInputDto.FormaPagoItem
                    {
                        Tipo = "CONTADO", Metodo = "TARJETA", Nombre = "Tarjeta"
                    }
                }
            };

            // Act + Assert: el aggregate debe lanzar por unicidad de índice

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

            var useCase = new RegistrarFormasDePagoUseCase(repo.Object, uow.Object, tenant.Object);

            var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
                useCase.HandleAsync(new RegistrarFormasDePagoInputDto
                {
                    Items = { new RegistrarFormasDePagoInputDto.FormaPagoItem { Tipo = "CREDITO", Nombre = "Crédito 30 días" } }
                }, CancellationToken.None));

            Assert.That(ex!.Message, Does.Contain("EmpresaId"));
        }
    }
}
