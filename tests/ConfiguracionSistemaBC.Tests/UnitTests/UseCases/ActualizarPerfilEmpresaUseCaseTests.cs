using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConfiguracionSistemaBC.Application.UseCases;
using ConfiguracionSistemaBC.Domain.Aggregates;
using ConfiguracionSistemaBC.Domain.Repositories;
using SharedKernel.ValueObjects;
using ConfiguracionSistemaBC.Domain.ValueObjects;
using Moq;
using NUnit.Framework;

namespace ConfiguracionSistemaBC.Tests.UnitTests.UseCases
{
    [TestFixture]
    public class ActualizarPerfilEmpresaUseCaseTests
    {
        // ----------------- Helpers -----------------

        private static ConfiguracionEmpresa CrearAgregadoBase(Guid tenantId)
        {
            var agg = ConfiguracionEmpresa.RegistrarNueva(
                tenantId,
                Ruc.FromString("20100070970"),
                "ACME S.A.C.",
                DireccionPostal.From(
                    linea: "AV. INDUSTRIAL 123",
                    ubigeo: "150101",
                    departamento: "LIMA",
                    provincia: "LIMA",
                    distrito: "LIMA"
                ),
                Moneda.PEN()
            );

            // Estado inicial “vacío”
            agg.ReemplazarTelefonos(Telefono.Vacio);
            agg.ReemplazarEmails(Array.Empty<EmailEmpresa>());
            agg.ActualizarPieDePagina(PieDePagina.Vacio);
            agg.EstablecerLogo(null);

            return agg;
        }

        private static (Mock<IConfiguracionEmpresaRepository> repo, Mock<IUnitOfWork> uow, ActualizarPerfilEmpresaUseCase uc)
            BuildUseCase()
        {
            var repo = new Mock<IConfiguracionEmpresaRepository>(MockBehavior.Strict);
            var uow  = new Mock<IUnitOfWork>(MockBehavior.Strict);

            var uc = new ActualizarPerfilEmpresaUseCase(repo.Object, uow.Object);
            return (repo, uow, uc);
        }

        private static ActualizarPerfilEmpresaUseCase.PreferenciasParams PrefsCompletoValido() =>
            new(
                Telefonos: "+51 999 888 777 / (01) 234-5678",
                EmailsVisibles: new[] { "ventas@acme.com", "info@acme.pe" },
                EmailsOcultos: new[] { "soporte@acme.com" },
                PieDePaginaHtml: "<p>Gracias por su compra<script>alert(1)</script></p>",
                PieDePaginaTextoPlano: null,
                Logo: new ActualizarPerfilEmpresaUseCase.LogoParams(
                    FileName: "logo.png",
                    ContentType: "image/png",
                    BytesLength: 80_000,
                    AnchoPx: 300,
                    AltoPx: 120
                ),
                QuitarLogo: false
            );

        // ----------------- Tests -----------------

        [Test]
        public async Task ExecuteAsync_Exito_Actualiza_Telefonos_Emails_PieDePagina_Logo()
        {
            // Arrange
            var (repo, uow, uc) = BuildUseCase();
            var tenantId = Guid.NewGuid();
            var agg = CrearAgregadoBase(tenantId);

            repo.Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);
            repo.Setup(r => r.UpdateAsync(agg, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

            var p = new ActualizarPerfilEmpresaUseCase.Params(
                TenantId: tenantId,
                Preferencias: PrefsCompletoValido()
            );

            // Act
            var result = await uc.ExecuteAsync(p);

            // Assert resultado visible
            Assert.That(result.TenantId, Is.EqualTo(tenantId));
            Assert.That(result.Ambiente, Is.EqualTo("PRUEBA")); // no cambia aquí
            Assert.That(result.MonedaBaseCodigo, Is.EqualTo("PEN"));
            Assert.That(result.Telefonos, Is.Not.Empty);
            Assert.That(result.EmailsVisibles, Has.Length.EqualTo(2));
            Assert.That(result.EmailsOcultos, Has.Length.EqualTo(1));
            Assert.That(result.PieDePaginaHtml, Does.Contain("<p>"));
            Assert.That(result.PieDePaginaHtml, Does.Not.Contain("<script").IgnoreCase);
            Assert.That(result.TieneLogo, Is.True);

            // Assert estado del agregado
            Assert.That(agg.Telefonos.EsVacio, Is.False);
            Assert.That(agg.Telefonos.Numeros.Count, Is.EqualTo(2));
            Assert.That(agg.Emails.Count, Is.EqualTo(3));
            Assert.That(agg.Emails.Count(e => e.EsVisible), Is.EqualTo(2));
            Assert.That(agg.Emails.Count(e => !e.EsVisible), Is.EqualTo(1));
            Assert.That(agg.PieDePagina.EsVacio, Is.False);
            Assert.That(agg.PieDePagina.Html, Does.Not.Contain("<script").IgnoreCase);
            Assert.That(agg.Logo, Is.Not.Null);
            Assert.That(agg.Logo!.ContentType, Is.EqualTo("image/png"));
            Assert.That(agg.Logo.AnchoPx, Is.EqualTo(300));
            Assert.That(agg.Logo.AltoPx, Is.EqualTo(120));

            repo.Verify(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
            repo.Verify(r => r.UpdateAsync(agg, It.IsAny<CancellationToken>()), Times.Once);
            uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            repo.VerifyNoOtherCalls();
            uow.VerifyNoOtherCalls();
        }

        [Test]
        public async Task ExecuteAsync_Exito_NullEnTodos_NoCambiaNada_PeroPersiste()
        {
            // Arrange
            var (repo, uow, uc) = BuildUseCase();
            var tenantId = Guid.NewGuid();
            var agg = CrearAgregadoBase(tenantId);

            // Estado inicial con algo para verificar que se mantiene igual
            agg.ReemplazarTelefonos(Telefono.FromTexto("999 111 222"));
            agg.ReemplazarEmails(new[] { EmailEmpresa.From("ventas@acme.com", true) });
            agg.ActualizarPieDePagina(PieDePagina.FromTextoPlano("Gracias"));
            agg.EstablecerLogo(LogoImagen.FromUpload("logo.jpg", "image/jpeg", 50_000, 200, 80));

            repo.Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);
            repo.Setup(r => r.UpdateAsync(agg, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

            var p = new ActualizarPerfilEmpresaUseCase.Params(
                TenantId: tenantId,
                Preferencias: new ActualizarPerfilEmpresaUseCase.PreferenciasParams(
                    Telefonos: null,
                    EmailsVisibles: null,
                    EmailsOcultos: null,
                    PieDePaginaHtml: null,
                    PieDePaginaTextoPlano: null,
                    Logo: null,
                    QuitarLogo: false
                )
            );

            // Act
            var result = await uc.ExecuteAsync(p);

            // Assert: se mantienen valores
            Assert.That(result.Telefonos, Is.Not.Empty);
            Assert.That(result.EmailsVisibles, Has.Length.EqualTo(1));
            Assert.That(result.EmailsOcultos, Is.Empty);
            Assert.That(result.PieDePaginaHtml, Is.Not.Empty);
            Assert.That(result.TieneLogo, Is.True);

            repo.Verify(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
            repo.Verify(r => r.UpdateAsync(agg, It.IsAny<CancellationToken>()), Times.Once);
            uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            repo.VerifyNoOtherCalls();
            uow.VerifyNoOtherCalls();
        }

        [Test]
        public async Task ExecuteAsync_Exito_LimpiarTelefonos_Emails_Pie_EliminarLogo()
        {
            // Arrange
            var (repo, uow, uc) = BuildUseCase();
            var tenantId = Guid.NewGuid();
            var agg = CrearAgregadoBase(tenantId);

            // poner algo inicial para luego limpiar
            agg.ReemplazarTelefonos(Telefono.FromTexto("+51 999 888 777 / 01 2345678"));
            agg.ReemplazarEmails(new[] {
                EmailEmpresa.From("ventas@acme.com", true),
                EmailEmpresa.From("soporte@acme.com", false)
            });
            agg.ActualizarPieDePagina(PieDePagina.FromHtml("<p>Hola</p>"));
            agg.EstablecerLogo(LogoImagen.FromUpload("logo.png", "image/png", 70_000, 250, 90));

            repo.Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);
            repo.Setup(r => r.UpdateAsync(agg, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

            var p = new ActualizarPerfilEmpresaUseCase.Params(
                TenantId: tenantId,
                Preferencias: new ActualizarPerfilEmpresaUseCase.PreferenciasParams(
                    Telefonos: "", // limpiar
                    EmailsVisibles: Array.Empty<string>(),
                    EmailsOcultos: Array.Empty<string>(),
                    PieDePaginaHtml: null,
                    PieDePaginaTextoPlano: "", // limpiar
                    Logo: null,
                    QuitarLogo: true
                )
            );

            // Act
            var result = await uc.ExecuteAsync(p);

            // Assert resultado
            Assert.That(result.Telefonos, Is.EqualTo(string.Empty));
            Assert.That(result.EmailsVisibles, Is.Empty);
            Assert.That(result.EmailsOcultos, Is.Empty);
            Assert.That(result.PieDePaginaHtml, Is.EqualTo(string.Empty));
            Assert.That(result.TieneLogo, Is.False);

            // Assert agregado
            Assert.That(agg.Telefonos.EsVacio, Is.True);
            Assert.That(agg.Emails, Is.Empty);
            Assert.That(agg.PieDePagina.EsVacio, Is.True);
            Assert.That(agg.Logo, Is.Null);

            repo.Verify(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
            repo.Verify(r => r.UpdateAsync(agg, It.IsAny<CancellationToken>()), Times.Once);
            uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            repo.VerifyNoOtherCalls();
            uow.VerifyNoOtherCalls();
        }

        [Test]
        public void ExecuteAsync_Falla_TenantNoExiste()
        {
            // Arrange
            var (repo, uow, uc) = BuildUseCase();
            var tenantId = Guid.NewGuid();

            repo.Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ConfiguracionEmpresa?)null);

            var p = new ActualizarPerfilEmpresaUseCase.Params(
                TenantId: tenantId,
                Preferencias: new ActualizarPerfilEmpresaUseCase.PreferenciasParams(
                    Telefonos: null, EmailsVisibles: null, EmailsOcultos: null,
                    PieDePaginaHtml: null, PieDePaginaTextoPlano: null,
                    Logo: null, QuitarLogo: false
                )
            );

            // Act + Assert
            Assert.That(
                async () => await uc.ExecuteAsync(p),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("no existe")
            );

            repo.Verify(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
            repo.VerifyNoOtherCalls();
            uow.VerifyNoOtherCalls();
        }

        [Test]
        public void ExecuteAsync_Falla_EmailInvalido()
        {
            // Arrange
            var (repo, uow, uc) = BuildUseCase();
            var tenantId = Guid.NewGuid();
            var agg = CrearAgregadoBase(tenantId);

            repo.Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);

            var prefs = new ActualizarPerfilEmpresaUseCase.PreferenciasParams(
                Telefonos: null,
                EmailsVisibles: new[] { "ok@acme.com" },
                EmailsOcultos: new[] { "malo@@acme..com" }, // inválido
                PieDePaginaHtml: null,
                PieDePaginaTextoPlano: null,
                Logo: null,
                QuitarLogo: false
            );

            var p = new ActualizarPerfilEmpresaUseCase.Params(tenantId, prefs);

            // Act + Assert
            Assert.That(
                async () => await uc.ExecuteAsync(p),
                Throws.TypeOf<ArgumentOutOfRangeException>().With.Message.Contains("Correo electrónico inválido")
            );

            repo.Verify(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
            repo.VerifyNoOtherCalls();
            uow.VerifyNoOtherCalls();
        }

        [Test]
        public void ExecuteAsync_Falla_LogoInvalido()
        {
            // Arrange
            var (repo, uow, uc) = BuildUseCase();
            var tenantId = Guid.NewGuid();
            var agg = CrearAgregadoBase(tenantId);

            repo.Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);

            var prefs = new ActualizarPerfilEmpresaUseCase.PreferenciasParams(
                Telefonos: null,
                EmailsVisibles: null,
                EmailsOcultos: null,
                PieDePaginaHtml: null,
                PieDePaginaTextoPlano: null,
                Logo: new ActualizarPerfilEmpresaUseCase.LogoParams(
                    FileName: "logo.gif",       // extensión no permitida
                    ContentType: "image/gif",   // content-type inválido
                    BytesLength: 10_000,
                    AnchoPx: 200,
                    AltoPx: 80
                ),
                QuitarLogo: false
            );

            var p = new ActualizarPerfilEmpresaUseCase.Params(tenantId, prefs);

            // Act + Assert
            Assert.That(
                async () => await uc.ExecuteAsync(p),
                Throws.TypeOf<ArgumentOutOfRangeException>().With.Message.Contains("Content-Type no permitido")
                .Or.With.Message.Contains("Extensión no permitida")
            );

            repo.Verify(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
            repo.VerifyNoOtherCalls();
            uow.VerifyNoOtherCalls();
        }

        [Test]
        public void ExecuteAsync_Falla_TenantIdVacio()
        {
            var (repo, uow, uc) = BuildUseCase();

            var p = new ActualizarPerfilEmpresaUseCase.Params(
                TenantId: Guid.Empty,
                Preferencias: new ActualizarPerfilEmpresaUseCase.PreferenciasParams(
                    Telefonos: null, EmailsVisibles: null, EmailsOcultos: null,
                    PieDePaginaHtml: null, PieDePaginaTextoPlano: null,
                    Logo: null, QuitarLogo: false
                )
            );

            Assert.That(
                async () => await uc.ExecuteAsync(p),
                Throws.TypeOf<ArgumentException>().With.Message.Contains("TenantId inválido")
            );

            repo.VerifyNoOtherCalls();
            uow.VerifyNoOtherCalls();
        }
    }
}
