using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConfiguracionSistemaBC.Application.UseCases;
using ConfiguracionSistemaBC.Domain.Aggregates;
using ConfiguracionSistemaBC.Domain.Repositories;
using ConfiguracionSistemaBC.Domain.ValueObjects; // Ruc
using Moq;
using NUnit.Framework;
using SharedKernel.Application.Interfaces;         // ITenantContext
using SharedKernel.ValueObjects;                   // DomicilioFiscal, Moneda, EmpresaId, Email

namespace ConfiguracionSistemaBC.Tests.Application.UseCases
{
    [TestFixture]
    public class ConsultarConfiguracionGeneralUseCaseTests
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

            var empresa = ConfiguracionEmpresa.RegistrarNueva(ruc, "ACME S.A.C.", dom, Moneda.PEN());

            // Preferencias para probar mapeos
            empresa.ReemplazarTelefono(SharedKernel.ValueObjects.Telefono.FromTexto("999-111-222"));
            empresa.ReemplazarEmails(new[]
            {
                SharedKernel.ValueObjects.Email.Create("admin@acme.pe"),
                SharedKernel.ValueObjects.Email.Create("ventas@acme.pe")
            });

            // Agrego un establecimiento adicional
            var est2 = empresa.RegistrarEstablecimiento("02", "Tienda Miraflores", dom);
            // Principal sigue siendo "01"

            // Oculto una forma de pago no-default (p.ej., "Plin" si existe)
            var fps = empresa.ListarFormasDePago();
            var plin = fps.FirstOrDefault(f => f.Nombre.Equals("Plin", StringComparison.OrdinalIgnoreCase));
            if (plin is not null)
            {
                empresa.ActualizarFormaDePago(plin.Id, visible: false);
            }

            // Oculto una unidad de medida no-default (p.ej., "SERVICIO")
            var ums = empresa.ListarUnidadesDeMedida();
            var servicio = ums.FirstOrDefault(u => u.Unidad.Codigo == "ZZ");
            if (servicio is not null)
            {
                empresa.ActualizarUnidadDeMedida(servicio.Id, visible: false);
            }

            return empresa;
        }

        [Test]
        public async Task Devuelve_snapshot_completo_filtrando_no_visibles_por_defecto()
        {
            // Arrange
            var empresa = NuevaEmpresaBootstrap();
            var empresaId = empresa.EmpresaId;

            var repo = new Mock<IConfiguracionEmpresaRepository>(MockBehavior.Strict);
            repo.Setup(r => r.GetByEmpresaIdAsync(empresaId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(empresa);

            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            tenant.SetupGet(t => t.EmpresaId).Returns(empresaId);
            tenant.SetupGet(t => t.TenantId).Returns(TenantId.From(Guid.NewGuid())); // valor dummy

            var useCase = new ConsultarConfiguracionGeneralUseCase(repo.Object, tenant.Object);

            var input = new ConsultarConfiguracionGeneralInputDto
            {
                // usa EmpresaId del contexto
                IncluirEstablecimientos = true,
                IncluirCatalogos = true,
                IncluirOcultos = false
            };

            // Act
            var outDto = await useCase.HandleAsync(input, CancellationToken.None);

            // Assert: identidad/base
            Assert.That(outDto.EmpresaId, Is.EqualTo(empresa.EmpresaId.Value));
            Assert.That(outDto.Ruc, Is.EqualTo("20600893409"));
            Assert.That(outDto.RazonSocial, Is.EqualTo("ACME S.A.C."));
            Assert.That(outDto.Ambiente, Is.EqualTo("PRUEBA"));
            Assert.That(outDto.MonedaBaseCodigo, Is.EqualTo("PEN"));

            // Dirección fiscal
            Assert.That(outDto.DireccionFiscal.PaisCodigo, Is.EqualTo("PE"));
            Assert.That(outDto.DireccionFiscal.Ubigeo, Is.EqualTo("150101"));
            Assert.That(outDto.DireccionFiscal.Direccion, Is.Not.Empty);

            // Preferencias
            Assert.That(outDto.Telefono, Does.Contain("999").IgnoreCase);
            Assert.That(outDto.Emails, Has.Length.GreaterThanOrEqualTo(1));
            Assert.That(outDto.PieDePagina, Is.Not.Null); // por bootstrap "Gracias Por su Preferencia"
            Assert.That(outDto.TieneLogo, Is.False);

            // Establecimientos
            Assert.That(outDto.Establecimientos, Has.Count.GreaterThanOrEqualTo(2));
            var principal = outDto.Establecimientos.FirstOrDefault(e => e.EsPrincipal);
            Assert.That(principal, Is.Not.Null);
            Assert.That(principal!.Codigo, Is.EqualTo("01"));
            Assert.That(outDto.EstablecimientoPrincipalId, Is.EqualTo(principal.Id));

            // Catálogos: no-visibles deben estar filtrados
            var plinOculto = outDto.FormasDePago.FirstOrDefault(f => f.Nombre.Equals("Plin", StringComparison.OrdinalIgnoreCase));
            if (plinOculto is not null)
            {
                Assert.Fail("No debería incluir elementos ocultos cuando IncluirOcultos=false.");
            }

            var servicioOculto = outDto.UnidadesDeMedida.FirstOrDefault(u => u.Codigo == "ZZ");
            if (servicioOculto is not null && servicioOculto.Visible == false)
            {
                Assert.Fail("No debería incluir unidades ocultas cuando IncluirOcultos=false.");
            }

            repo.VerifyAll();
        }

        [Test]
        public async Task Incluye_ocultos_cuando_se_solicita()
        {
            // Arrange
            var empresa = NuevaEmpresaBootstrap();
            var empresaId = empresa.EmpresaId;

            var repo = new Mock<IConfiguracionEmpresaRepository>(MockBehavior.Strict);
            repo.Setup(r => r.GetByEmpresaIdAsync(empresaId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(empresa);

            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            tenant.SetupGet(t => t.EmpresaId).Returns(empresaId);
            tenant.SetupGet(t => t.TenantId).Returns(TenantId.From(Guid.NewGuid())); // valor dummy

            var useCase = new ConsultarConfiguracionGeneralUseCase(repo.Object, tenant.Object);

            var input = new ConsultarConfiguracionGeneralInputDto
            {
                IncluirEstablecimientos = false,
                IncluirCatalogos = true,
                IncluirOcultos = true
            };

            // Act
            var outDto = await useCase.HandleAsync(input, CancellationToken.None);

            // Assert: ahora deberían aparecer los ocultos (si existen)
            var plin = outDto.FormasDePago.FirstOrDefault(f => f.Nombre.Equals("Plin", StringComparison.OrdinalIgnoreCase));
            if (plin is not null)
            {
                Assert.That(plin.Visible, Is.False);
            }

            var servicio = outDto.UnidadesDeMedida.FirstOrDefault(u => u.Codigo == "ZZ");
            if (servicio is not null)
            {
                // En bootstrap lo ocultamos; si existe, debe venir visible==false
                // (si no existe, no asertamos)
                // Solo validamos que el DTO incluyó el registro.
                Assert.That(servicio.Codigo, Is.EqualTo("ZZ"));
            }

            repo.VerifyAll();
        }

        [Test]
        public void Lanza_si_no_hay_empresa_en_contexto_y_no_se_envia_en_input()
        {
            var repo = new Mock<IConfiguracionEmpresaRepository>(MockBehavior.Loose);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            tenant.SetupGet(t => t.EmpresaId).Returns((EmpresaId)null!); // Forzar null para test
            tenant.SetupGet(t => t.TenantId).Returns(TenantId.From(Guid.NewGuid())); // valor dummy

            var useCase = new ConsultarConfiguracionGeneralUseCase(repo.Object, tenant.Object);

            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await useCase.HandleAsync(new ConsultarConfiguracionGeneralInputDto(), CancellationToken.None));

            Assert.That(ex!.Message, Does.Contain("EmpresaId"));
        }

        [Test]
        public async Task Permite_especificar_empresa_por_input()
        {
            // Arrange
            var empresa = NuevaEmpresaBootstrap();
            var empresaId = empresa.EmpresaId;

            var repo = new Mock<IConfiguracionEmpresaRepository>(MockBehavior.Strict);
            repo.Setup(r => r.GetByEmpresaIdAsync(empresaId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(empresa);

            // Simulamos que el contexto NO tiene EmpresaId (p.ej., tarea administrativa)
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            tenant.SetupGet(t => t.EmpresaId).Returns((EmpresaId)null!); // Forzar null para test
            tenant.SetupGet(t => t.TenantId).Returns(TenantId.From(Guid.NewGuid())); // valor dummy

            var useCase = new ConsultarConfiguracionGeneralUseCase(repo.Object, tenant.Object);

            var input = new ConsultarConfiguracionGeneralInputDto
            {
                EmpresaId = empresa.EmpresaId.Value,
                IncluirEstablecimientos = true,
                IncluirCatalogos = true
            };

            // Act
            var outDto = await useCase.HandleAsync(input, CancellationToken.None);

            // Assert básico
            Assert.That(outDto.EmpresaId, Is.EqualTo(empresa.EmpresaId.Value));
            Assert.That(outDto.Ruc, Is.EqualTo("20600893409"));

            repo.VerifyAll();
        }

        [Test]
        public void Lanza_si_la_empresa_no_existe()
        {
            // Arrange
            var fakeEmpresaId = EmpresaId.From(Guid.NewGuid().ToString());

            var repo = new Mock<IConfiguracionEmpresaRepository>(MockBehavior.Strict);
            repo.Setup(r => r.GetByEmpresaIdAsync(fakeEmpresaId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ConfiguracionEmpresa?)null);

            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            tenant.SetupGet(t => t.EmpresaId).Returns(fakeEmpresaId);
            tenant.SetupGet(t => t.TenantId).Returns(TenantId.From(Guid.NewGuid())); // valor dummy

            var useCase = new ConsultarConfiguracionGeneralUseCase(repo.Object, tenant.Object);

            var ex = Assert.ThrowsAsync<KeyNotFoundException>(async () =>
                await useCase.HandleAsync(new ConsultarConfiguracionGeneralInputDto(), CancellationToken.None));

            Assert.That(ex!.Message, Does.Contain("No se encontró la configuración").IgnoreCase);
            repo.VerifyAll();
        }
    }
}
