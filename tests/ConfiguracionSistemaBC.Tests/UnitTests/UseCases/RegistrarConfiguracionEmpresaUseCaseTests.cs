using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConfiguracionSistemaBC.Application.UseCases;
using ConfiguracionSistemaBC.Domain.Aggregates;
using ConfiguracionSistemaBC.Domain.Repositories;
using SharedKernel.ValueObjects;
using Moq;
using NUnit.Framework;
using ConfiguracionSistemaBC.Domain.ValueObjects;

namespace ConfiguracionSistemaBC.Tests.Application.UseCases
{
    [TestFixture]
    public class RegistrarConfiguracionEmpresaUseCaseTests
    {
        private static RegistrarConfiguracionEmpresaUseCase.DatosLegalesParams DatosLegalesOk() => new(
            Ruc: "20100070970",
            RazonSocial: "ACME S.A.C.",
            DireccionLinea: "AV. INDUSTRIAL 123",
            Ubigeo: "150101", // Lima-Lima-Lima
            Departamento: "LIMA",
            Provincia: "LIMA",
            Distrito: "LIMA",
            NombreComercial: "ACME",
            PaisIso: "PE",
            AddressTypeCode: "0000"
        );

    private static RegistrarConfiguracionEmpresaUseCase.EstablecimientoParams EstPrincipalOk(string codigo = "PRIN") => new(
            Codigo: codigo,
            Nombre: "PRINCIPAL",
            DireccionLinea: "AV. INDUSTRIAL 123",
            Ubigeo: "150101",
            Departamento: "LIMA",
            Provincia: "LIMA",
            Distrito: "LIMA",
            PaisIso: "PE",
            AddressTypeCode: "0000"
        );

    private static RegistrarConfiguracionEmpresaUseCase.MonedaParams MonedaPen() => new("PEN");

    private static RegistrarConfiguracionEmpresaUseCase.PreferenciasParams PrefsOk() => new(
            Telefonos: "+51 999 888 777 / (01) 234-5678",
            EmailsVisibles: new[] { "ventas@acme.com", "info@acme.pe" },
            EmailsOcultos: new[] { "soporte@acme.com" },
            PieDePaginaHtml: "<p>Gracias por su compra<script>alert(1)</script></p>",
            Logo: new RegistrarConfiguracionEmpresaUseCase.LogoParams(
                FileName: "logo.png",
                ContentType: "image/png",
                BytesLength: 80_000,
                AnchoPx: 300,
                AltoPx: 120
            )
        );

    private static RegistrarConfiguracionEmpresaUseCase.SerieParams SerieFacturaDefecto(string estCodigo = "PRIN") => new(
            TipoComprobante: "FACTURA",  // admite "01" / "F" también
            Serie: "F001",
            EstablecimientoCodigo: estCodigo,
            CorrelativoInicial: "1",
            TipoOperacion: null,         // default 0101
            EsPorDefecto: true
        );

        private static RegistrarConfiguracionEmpresaUseCase.Params MakeParams(
            Guid tenantId,
            bool withPrefs = false,
            bool withSeries = false)
        {
            return new RegistrarConfiguracionEmpresaUseCase.Params(
                TenantId: tenantId,
                DatosLegales: DatosLegalesOk(),
                MonedaBase: MonedaPen(),
                Preferencias: withPrefs ? PrefsOk() : null,
                EstablecimientoPrincipal: EstPrincipalOk(),
                SeriesIniciales: withSeries ? new[] { SerieFacturaDefecto() } : null
            );
        }

        private static (Mock<IConfiguracionEmpresaRepository> repo, Mock<IUnitOfWork> uow, RegistrarConfiguracionEmpresaUseCase uc)
            BuildUseCase()
        {
            var repo = new Mock<IConfiguracionEmpresaRepository>(MockBehavior.Strict);
            var uow  = new Mock<IUnitOfWork>(MockBehavior.Strict);

            var uc = new RegistrarConfiguracionEmpresaUseCase(repo.Object, uow.Object);
            return (repo, uow, uc);
        }

        [Test]
        public async Task ExecuteAsync_Exito_Minimo_RegistraAmbientePruebaYEstPrincipal()
        {
            // Arrange
            var (repo, uow, uc) = BuildUseCase();
            var tenantId = Guid.NewGuid();
            var p = MakeParams(tenantId, withPrefs: false, withSeries: false);

            repo.Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ConfiguracionEmpresa?)null);

            ConfiguracionEmpresa? agregadoCapturado = null;
            repo.Setup(r => r.AddAsync(It.IsAny<ConfiguracionEmpresa>(), It.IsAny<CancellationToken>()))
                .Callback<ConfiguracionEmpresa, CancellationToken>((agg, _) => agregadoCapturado = agg)
                .Returns(Task.CompletedTask);

            uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

            // Act
            var result = await uc.ExecuteAsync(p, CancellationToken.None);

            // Assert
            Assert.That(result.TenantId, Is.EqualTo(tenantId));
            Assert.That(result.Ruc, Is.EqualTo("20100070970"));
            Assert.That(result.RazonSocial, Is.EqualTo("ACME S.A.C."));
            Assert.That(result.MonedaBaseCodigo, Is.EqualTo("PEN"));
            Assert.That(result.Ambiente, Is.EqualTo("PRUEBA")); // por defecto

            // Establecimiento principal creado
            Assert.That(result.Establecimientos, Has.Count.EqualTo(1));
            Assert.That(result.Establecimientos[0].Codigo, Is.EqualTo("PRIN"));
            Assert.That(result.Series, Is.Empty); // no enviamos series

            // Validar estado del agregado capturado
            Assert.That(agregadoCapturado, Is.Not.Null);
            Assert.That(agregadoCapturado!.Ambiente.EsPrueba, Is.True);
            Assert.That(agregadoCapturado.Ruc.Numero, Is.EqualTo("20100070970"));
            Assert.That(agregadoCapturado.MonedaBase.Simbolo, Is.EqualTo("S/.") );
            Assert.That(agregadoCapturado.MonedaBase, Is.EqualTo(Moneda.PEN()));
            Assert.That(agregadoCapturado.ListarEstablecimientos(), Has.Count.EqualTo(1));

            repo.Verify(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
            repo.Verify(r => r.AddAsync(It.IsAny<ConfiguracionEmpresa>(), It.IsAny<CancellationToken>()), Times.Once);
            uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            repo.VerifyNoOtherCalls();
            uow.VerifyNoOtherCalls();
        }

        [Test]
        public async Task ExecuteAsync_Exito_ConPreferenciasYSerieFacturaPorDefecto()
        {
            // Arrange
            var (repo, uow, uc) = BuildUseCase();
            var tenantId = Guid.NewGuid();
            var p = MakeParams(tenantId, withPrefs: true, withSeries: true);

            repo.Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ConfiguracionEmpresa?)null);

            ConfiguracionEmpresa? agregado = null;
            repo.Setup(r => r.AddAsync(It.IsAny<ConfiguracionEmpresa>(), It.IsAny<CancellationToken>()))
                .Callback<ConfiguracionEmpresa, CancellationToken>((agg, _) => agregado = agg)
                .Returns(Task.CompletedTask);

            uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

            // Act
            var result = await uc.ExecuteAsync(p);

            // Assert: ambiente
            Assert.That(result.Ambiente, Is.EqualTo("PRUEBA"));

            // Establecimiento (1) y Serie (1)
            Assert.That(result.Establecimientos, Has.Count.EqualTo(1));
            Assert.That(result.Series, Has.Count.EqualTo(1));
            var s = result.Series[0];
            Assert.That(s.Serie, Is.EqualTo("F001"));
            Assert.That(s.Tipo, Is.EqualTo(TipoComprobanteCodigo.Factura));
            Assert.That(s.EsPorDefecto, Is.True);
            Assert.That(s.CorrelativoActual.Valor, Is.EqualTo(1));
            Assert.That(s.TipoOperacion, Is.EqualTo(TipoOperacion.Default));

            // Preferencias aplicadas al agregado
            Assert.That(agregado, Is.Not.Null);
            // teléfonos (2 números deducidos del texto)
            Assert.That(agregado!.Telefonos.EsVacio, Is.False);
            Assert.That(agregado.Telefonos.Numeros.Count, Is.EqualTo(2));

            // emails (2 visibles + 1 oculto)
            Assert.That(agregado.Emails.Count, Is.EqualTo(3));
            Assert.That(agregado.Emails.Count(e => e.EsVisible), Is.EqualTo(2));
            Assert.That(agregado.Emails.Count(e => !e.EsVisible), Is.EqualTo(1));

            // pie de página (saneado: elimina <script>)
            Assert.That(agregado.PieDePagina.EsVacio, Is.False);
            Assert.That(agregado.PieDePagina.Html, Does.Contain("<p>"));
            Assert.That(agregado.PieDePagina.Html, Does.Not.Contain("<script").IgnoreCase);

            // logo
            Assert.That(agregado.Logo, Is.Not.Null);
            Assert.That(agregado.Logo!.ContentType, Is.EqualTo("image/png"));
            Assert.That(agregado.Logo.AnchoPx, Is.EqualTo(300));
            Assert.That(agregado.Logo.AltoPx, Is.EqualTo(120));

            repo.Verify(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
            repo.Verify(r => r.AddAsync(It.IsAny<ConfiguracionEmpresa>(), It.IsAny<CancellationToken>()), Times.Once);
            uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            repo.VerifyNoOtherCalls();
            uow.VerifyNoOtherCalls();
        }

        [Test]
        public void ExecuteAsync_Falla_SiYaExisteConfiguracion()
        {
            // Arrange
            var (repo, uow, uc) = BuildUseCase();
            var tenantId = Guid.NewGuid();
            var p = MakeParams(tenantId);

            // simulamos existente
            var existente = ConfiguracionEmpresa.RegistrarNueva(
                tenantId,
                Ruc.FromString("20100070970"),
                "YA EXISTE SAC",
                DireccionPostal.From("X", "150101", "LIMA", "LIMA", "LIMA"),
                Moneda.PEN()
            );

            repo.Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existente);

            // Act + Assert
            Assert.That(
                async () => await uc.ExecuteAsync(p),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("ya fue registrada")
            );

            repo.Verify(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
            repo.VerifyNoOtherCalls();
            uow.VerifyNoOtherCalls();
        }

        [Test]
        public void ExecuteAsync_Falla_TenantIdVacio()
        {
            var (repo, uow, uc) = BuildUseCase();

            var p = MakeParams(Guid.Empty);

            Assert.That(
                async () => await uc.ExecuteAsync(p),
                Throws.TypeOf<ArgumentException>().With.Message.Contains("TenantId inválido")
            );

            repo.VerifyNoOtherCalls();
            uow.VerifyNoOtherCalls();
        }

        [Test]
        public void ExecuteAsync_Falla_SerieReferenciaEstablecimientoDistinto()
        {
            // Arrange
            var (repo, uow, uc) = BuildUseCase();
            var tenantId = Guid.NewGuid();

            var baseParams = new RegistrarConfiguracionEmpresaUseCase.Params(
                TenantId: tenantId,
                DatosLegales: DatosLegalesOk(),
                MonedaBase: MonedaPen(),
                Preferencias: null,
                EstablecimientoPrincipal: EstPrincipalOk("PRIN"),
                SeriesIniciales: new[]
                {
                    // Serie apunta a "OTRO", distinto al "PRIN" que se registra en el UC
                    new RegistrarConfiguracionEmpresaUseCase.SerieParams(
                        TipoComprobante: "01",
                        Serie: "F001",
                        EstablecimientoCodigo: "OTRO",
                        CorrelativoInicial: "1",
                        TipoOperacion: null,
                        EsPorDefecto: true
                    )
                }
            );

            repo.Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ConfiguracionEmpresa?)null);

            // Act + Assert
            Assert.That(
                async () => await uc.ExecuteAsync(baseParams),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("referencia el establecimiento")
            );

            repo.Verify(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
            repo.VerifyNoOtherCalls();
            uow.VerifyNoOtherCalls();
        }

        [Test]
        public void ExecuteAsync_Falla_EmailInvalido_EnPreferencias()
        {
            // Arrange
            var (repo, uow, uc) = BuildUseCase();
            var tenantId = Guid.NewGuid();

            var prefs = new RegistrarConfiguracionEmpresaUseCase.PreferenciasParams(
                Telefonos: null,
                EmailsVisibles: new[] { "bueno@acme.com" },
                EmailsOcultos: new[] { "malo@@acme..com" }, // inválido
                PieDePaginaHtml: null,
                Logo: null
            );

            var p = new RegistrarConfiguracionEmpresaUseCase.Params(
                TenantId: tenantId,
                DatosLegales: DatosLegalesOk(),
                MonedaBase: MonedaPen(),
                Preferencias: prefs,
                EstablecimientoPrincipal: EstPrincipalOk(),
                SeriesIniciales: null
            );

            repo.Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ConfiguracionSistemaBC.Domain.Aggregates.ConfiguracionEmpresa?)null);

            // Act + Assert
            Assert.That(
                async () => await uc.ExecuteAsync(p),
                Throws.TypeOf<ArgumentOutOfRangeException>().With.Message.Contains("Email inválido")
            );

            repo.Verify(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
            repo.VerifyNoOtherCalls();
            uow.VerifyNoOtherCalls();
        }
    }
}