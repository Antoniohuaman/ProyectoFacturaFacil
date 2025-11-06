using System;
using System.Threading;
using System.Threading.Tasks;
using ConfiguracionSistemaBC.Application.UseCases;
using ConfiguracionSistemaBC.Domain.Aggregates;
using ConfiguracionSistemaBC.Domain.Repositories;
using ConfiguracionSistemaBC.Domain.ValueObjects;          // AmbienteFe, (posible) Ruc de dominio
using Moq;
using NUnit.Framework;
using SharedKernel.Application.Interfaces;                 // ITenantContext
using SharedKernel.ValueObjects;                           // DomicilioFiscal, Moneda, Telefono, Email
using ConfiguracionSistemaBC.Application.Interfaces;       // IUnitOfWork

// Si conviven Ruc en Domain y en SharedKernel, puedes descomentar el alias que corresponda:
// using RucDomain = ConfiguracionSistemaBC.Domain.ValueObjects.Ruc;
// using RucShared = SharedKernel.ValueObjects.Ruc;

namespace ConfiguracionSistemaBC.Tests.Application.UseCases
{
    [TestFixture]
    public class RegistrarConfiguracionEmpresaUseCaseTests
    {
        private static RegistrarConfiguracionEmpresaInputDto BuildInput(
            string ruc = "20600893409",
            string razon = "ACME S.A.C.")
        {
            return new RegistrarConfiguracionEmpresaInputDto
            {
                Ruc = ruc,
                RazonSocial = razon,
                NombreComercial = "ACME",
                DireccionFiscal = new RegistrarConfiguracionEmpresaInputDto.DireccionFiscalDto
                {
                    PaisCodigo = "PE",
                    Ubigeo = "150101",
                    Direccion = "Av. Lima 123",
                    Referencia = "Cerca al parque"
                },
                MonedaCodigo = "PEN",
                Ambiente = "PRUEBA",
                EstablecimientoCodigo = "01",
                EstablecimientoNombre = "Establecimiento Principal",
                Telefono = "999888777",
                Emails = new[] { "admin@acme.test", "conta@acme.test" },
                PieDePagina = "Gracias por su preferencia",
                ModoPrecio = "INCLUYE_IGV"
            };
        }

        [Test]
        public async Task Crea_configuracion_basica__y_bootstrap_ok()
        {
            // Arrange
            var repo = new Mock<IConfiguracionEmpresaRepository>(MockBehavior.Strict);
            var uow  = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Loose); // opcional: no se usa en el alta

            repo.Setup(r => r.FindByRucAsync(It.IsAny<Ruc>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ConfiguracionEmpresa?)null);

            ConfiguracionEmpresa agregadoCapturado = null!;
            repo.Setup(r => r.AddAsync(It.IsAny<ConfiguracionEmpresa>(), It.IsAny<CancellationToken>()))
                .Callback<ConfiguracionEmpresa, CancellationToken>((a, _) => agregadoCapturado = a)
                .Returns(Task.CompletedTask);

                uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

            var useCase = new RegistrarConfiguracionEmpresaUseCase(repo.Object, uow.Object, tenant.Object);
            var input = BuildInput();

            // Act
            var output = await useCase.HandleAsync(input, CancellationToken.None);

            // Assert
            Assert.That(agregadoCapturado, Is.Not.Null, "No se capturó el aggregate.");
            Assert.That(output.Ruc, Is.EqualTo("20600893409"));
            Assert.That(output.RazonSocial, Is.EqualTo("ACME S.A.C."));
            Assert.That(output.EmpresaId, Is.Not.Empty);
            Assert.That(output.Ambiente, Is.EqualTo("PRUEBA"));
            Assert.That(output.MonedaBaseCodigo, Is.EqualTo("PEN"));

            // Establecimiento principal creado y con datos mapeados a la salida
            Assert.That(output.EstablecimientoPrincipal, Is.Not.Null);
            Assert.That(output.EstablecimientoPrincipal!.Codigo, Is.EqualTo("01"));
            Assert.That(output.EstablecimientoPrincipal!.Nombre, Is.EqualTo("Establecimiento Principal"));
            Assert.That(output.EstablecimientoPrincipal!.Direccion, Is.EqualTo("Av. Lima 123"));
            Assert.That(output.EstablecimientoPrincipal!.Ubigeo, Is.EqualTo("150101"));

            // Bootstrap de catálogos
            Assert.That(output.FormasDePagoPreCreadas, Is.GreaterThan(0));
            Assert.That(output.UnidadesDeMedidaPreCreadas, Is.GreaterThan(0));

            // Preferencias aplicadas (validación liviana)
            Assert.That(agregadoCapturado.Emails.Count, Is.EqualTo(2));
            Assert.That(agregadoCapturado.PieDePagina, Is.Not.Null);

            // Verificaciones de llamadas
            repo.Verify(r => r.FindByRucAsync(It.IsAny<Ruc>(), It.IsAny<CancellationToken>()), Times.Once);
            repo.Verify(r => r.AddAsync(It.IsAny<ConfiguracionEmpresa>(), It.IsAny<CancellationToken>()), Times.Once);
            uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void Falla_si_ruc_ya_registrado()
        {
            // Arrange
            var repo = new Mock<IConfiguracionEmpresaRepository>(MockBehavior.Strict);
            var uow  = new Mock<IUnitOfWork>(MockBehavior.Strict);

            // Creamos un agregado existente para ese RUC
            var existente = ConfiguracionEmpresa.RegistrarNueva(
                ConfiguracionSistemaBC.Domain.ValueObjects.Ruc.From("20600893409"),
                "ACME S.A.C.",
                DomicilioFiscal.FromPeru(
                    linea: "Av. Existente 1",
                    ubigeo: "150101",
                    departamento: null,
                    provincia: null,
                    distrito: null,
                    addressTypeCode: null
                ),
                Moneda.PEN());

            repo.Setup(r => r.FindByRucAsync(It.IsAny<Ruc>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(existente);

            var useCase = new RegistrarConfiguracionEmpresaUseCase(repo.Object, uow.Object);

            // Act
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await useCase.HandleAsync(BuildInput(), CancellationToken.None));

            // Assert
            Assert.That(ex!.Message, Does.Contain("Ya existe una configuración registrada"));
            repo.Verify(r => r.FindByRucAsync(It.IsAny<Ruc>(), It.IsAny<CancellationToken>()), Times.Once);
            repo.Verify(r => r.AddAsync(It.IsAny<ConfiguracionEmpresa>(), It.IsAny<CancellationToken>()), Times.Never);
            uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

    // El ambiente inicial siempre debe ser PRUEBA. No se permite crear directamente en PRODUCCION.
    }
}
