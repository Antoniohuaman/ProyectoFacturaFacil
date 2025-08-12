using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConfiguracionSistemaBC.Application.UseCases;
using ConfiguracionSistemaBC.Domain.Aggregates;
using ConfiguracionSistemaBC.Domain.Repositories;
using ConfiguracionSistemaBC.Domain.ValueObjects;
using Moq;
using NUnit.Framework;

namespace ConfiguracionSistemaBC.Tests.Application.UseCases
{
    [TestFixture]
    public class RegistrarSerieUseCaseTests
    {
        // ---------- Helpers ----------
        private static ConfiguracionEmpresa CrearAgregadoConEstPrincipal(Guid tenantId, string estCodigo = "PRIN")
        {
            var agg = ConfiguracionEmpresa.RegistrarNueva(
                tenantId,
                Ruc.FromString("20100070970"),
                "ACME S.A.C.",
                DireccionPostal.From("AV. INDUSTRIAL 123", "150101", "LIMA", "LIMA", "LIMA"),
                Moneda.PEN
            );

            agg.RegistrarEstablecimiento(estCodigo, "PRINCIPAL",
                DireccionPostal.From("AV. INDUSTRIAL 123", "150101", "LIMA", "LIMA", "LIMA"));

            return agg;
        }

        private static (Mock<IConfiguracionEmpresaRepository> repo, Mock<IUnitOfWork> uow, RegistrarSerieUseCase uc)
            BuildUseCase()
        {
            var repo = new Mock<IConfiguracionEmpresaRepository>(MockBehavior.Strict);
            var uow  = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var uc   = new RegistrarSerieUseCase(repo.Object, uow.Object);
            return (repo, uow, uc);
        }

        // ---------- Casos de éxito ----------

        [Test]
        public async Task ExecuteAsync_Exito_SerieFactura_PorDefecto()
        {
            var (repo, uow, uc) = BuildUseCase();
            var tenantId = Guid.NewGuid();
            var agg = CrearAgregadoConEstPrincipal(tenantId);

            repo.Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);
            uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

            var p = new RegistrarSerieUseCase.Params(
                TenantId: tenantId,
                TipoComprobante: "FACTURA",  // también valen "01" / "F"
                Serie: "F001",
                EstablecimientoCodigo: "PRIN",
                CorrelativoInicial: "1",
                TipoOperacion: null,          // default 0101
                EsPorDefecto: true
            );

            var result = await uc.ExecuteAsync(p);

            // Resultado
            Assert.That(result.TipoComprobanteCodigo, Is.EqualTo("01"));
            Assert.That(result.Serie, Is.EqualTo("F001"));
            Assert.That(result.Correlativo, Is.EqualTo(1));
            Assert.That(result.TipoOperacionCodigo, Is.EqualTo("0101"));
            Assert.That(result.EsPorDefecto, Is.True);

            // Estado en el agregado
            var creada = agg.ObtenerSeriePorId(result.SerieId);
            Assert.That(creada, Is.Not.Null);
            Assert.That(creada!.EsPorDefecto, Is.True);
            Assert.That(agg.ObtenerSeriePorDefecto(TipoComprobanteCodigo.Factura)!.Id, Is.EqualTo(result.SerieId));

            repo.Verify(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
            uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            repo.VerifyNoOtherCalls();
            uow.VerifyNoOtherCalls();
        }

        [Test]
        public async Task ExecuteAsync_Exito_SerieBoleta_SinDefault_ConTipoOperacion()
        {
            var (repo, uow, uc) = BuildUseCase();
            var tenantId = Guid.NewGuid();
            var agg = CrearAgregadoConEstPrincipal(tenantId);

            repo.Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);
            uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

            var p = new RegistrarSerieUseCase.Params(
                TenantId: tenantId,
                TipoComprobante: "03",   // Boleta
                Serie: "B123",
                EstablecimientoCodigo: "PRIN",
                CorrelativoInicial: "00000010",
                TipoOperacion: "1001",   // detracción
                EsPorDefecto: false
            );

            var result = await uc.ExecuteAsync(p);

            Assert.That(result.TipoComprobanteCodigo, Is.EqualTo("03"));
            Assert.That(result.Serie, Is.EqualTo("B123"));
            Assert.That(result.Correlativo, Is.EqualTo(10));
            Assert.That(result.TipoOperacionCodigo, Is.EqualTo("1001"));
            Assert.That(result.EsPorDefecto, Is.False);

            // Para Boleta no debe haber default (no lo pedimos)
            Assert.That(agg.ObtenerSeriePorDefecto(TipoComprobanteCodigo.Boleta), Is.Null);

            repo.Verify(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
            uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            repo.VerifyNoOtherCalls();
            uow.VerifyNoOtherCalls();
        }

        // ---------- Casos de error ----------

        [Test]
        public void ExecuteAsync_Falla_TenantVacio()
        {
            var (repo, uow, uc) = BuildUseCase();

            var p = new RegistrarSerieUseCase.Params(
                TenantId: Guid.Empty,
                TipoComprobante: "01",
                Serie: "F001",
                EstablecimientoCodigo: "PRIN",
                CorrelativoInicial: "1"
            );

            Assert.That(
                async () => await uc.ExecuteAsync(p),
                Throws.TypeOf<ArgumentException>().With.Message.Contains("TenantId inválido")
            );

            repo.VerifyNoOtherCalls();
            uow.VerifyNoOtherCalls();
        }

        [Test]
        public void ExecuteAsync_Falla_ConfiguracionNoExiste()
        {
            var (repo, uow, uc) = BuildUseCase();
            var tenantId = Guid.NewGuid();

            repo.Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ConfiguracionEmpresa?)null);

            var p = new RegistrarSerieUseCase.Params(
                TenantId: tenantId,
                TipoComprobante: "01",
                Serie: "F001",
                EstablecimientoCodigo: "PRIN",
                CorrelativoInicial: "1"
            );

            Assert.That(
                async () => await uc.ExecuteAsync(p),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("aún no existe")
            );

            repo.Verify(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
            repo.VerifyNoOtherCalls();
            uow.VerifyNoOtherCalls();
        }

        [Test]
        public void ExecuteAsync_Falla_EstablecimientoNoExiste()
        {
            var (repo, uow, uc) = BuildUseCase();
            var tenantId = Guid.NewGuid();
            var agg = CrearAgregadoConEstPrincipal(tenantId, estCodigo: "PRIN");

            repo.Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);

            var p = new RegistrarSerieUseCase.Params(
                TenantId: tenantId,
                TipoComprobante: "01",
                Serie: "F001",
                EstablecimientoCodigo: "OTRO", // no existe
                CorrelativoInicial: "1"
            );

            Assert.That(
                async () => await uc.ExecuteAsync(p),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("no existe")
            );

            repo.Verify(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
            repo.VerifyNoOtherCalls();
            uow.VerifyNoOtherCalls();
        }

        [Test]
        public void ExecuteAsync_Falla_PrefijoNoCoincideConTipo()
        {
            var (repo, uow, uc) = BuildUseCase();
            var tenantId = Guid.NewGuid();
            var agg = CrearAgregadoConEstPrincipal(tenantId);

            repo.Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);

            var p = new RegistrarSerieUseCase.Params(
                TenantId: tenantId,
                TipoComprobante: "01", // Factura
                Serie: "B001",         // prefijo B (boleta) -> debe fallar
                EstablecimientoCodigo: "PRIN",
                CorrelativoInicial: "1"
            );

            Assert.That(
                async () => await uc.ExecuteAsync(p),
                Throws.TypeOf<ArgumentException>() // lanzada por SerieCodigo.ValidarSegunTipo
            );

            repo.Verify(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
            repo.VerifyNoOtherCalls();
            uow.VerifyNoOtherCalls();
        }

        [Test]
        public void ExecuteAsync_Falla_CorrelativoFueraDeRango()
        {
            var (repo, uow, uc) = BuildUseCase();
            var tenantId = Guid.NewGuid();
            var agg = CrearAgregadoConEstPrincipal(tenantId);

            repo.Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);

            var p = new RegistrarSerieUseCase.Params(
                TenantId: tenantId,
                TipoComprobante: "03",
                Serie: "B001",
                EstablecimientoCodigo: "PRIN",
                CorrelativoInicial: "000000000" // 9 dígitos -> inválido
            );

            Assert.That(
                async () => await uc.ExecuteAsync(p),
                Throws.TypeOf<ArgumentOutOfRangeException>()
            );

            repo.Verify(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
            repo.VerifyNoOtherCalls();
            uow.VerifyNoOtherCalls();
        }

        [Test]
        public void ExecuteAsync_Falla_SerieDuplicadaMismoTipo()
        {
            var (repo, uow, uc) = BuildUseCase();
            var tenantId = Guid.NewGuid();
            var agg = CrearAgregadoConEstPrincipal(tenantId);

            // ya existe F001 (Factura) en el agregado
            var est = agg.BuscarEstablecimientoPorCodigo("PRIN")!;
            agg.AgregarSerie(TipoComprobanteCodigo.Factura, SerieCodigo.ForTipo("F001", TipoComprobanteCodigo.Factura),
                est.Id, Correlativo.From(1));

            repo.Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);

            var p = new RegistrarSerieUseCase.Params(
                TenantId: tenantId,
                TipoComprobante: "01",
                Serie: "F001",
                EstablecimientoCodigo: "PRIN",
                CorrelativoInicial: "2"
            );

            Assert.That(
                async () => await uc.ExecuteAsync(p),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("ya existe")
            );

            repo.Verify(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
            repo.VerifyNoOtherCalls();
            uow.VerifyNoOtherCalls();
        }
    }
}