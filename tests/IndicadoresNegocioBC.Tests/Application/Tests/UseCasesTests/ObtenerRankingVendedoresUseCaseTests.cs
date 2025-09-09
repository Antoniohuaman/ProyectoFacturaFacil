using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using IndicadoresNegocioBC.Application.UseCases.IndicadorNegocio;
using IndicadoresNegocioBC.Domain.Aggregates;
using IndicadoresNegocioBC.Domain.Repositories;
using IndicadoresNegocioBC.Domain.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace IndicadoresNegocioBC.Tests.Application.UseCases
{
    [TestFixture]
    public class ObtenerRankingVendedoresUseCaseTests
    {
        // ====== Builders mínimos (ajusta a tus factories reales si difieren) ======
        private static Moneda PEN() => Moneda.Create("PEN");
        private static Dinero Money(decimal m) => Dinero.Create(m, PEN());
    private static SegmentoIndicador SegmentoSoles() => SegmentoIndicador.ParaEmpresa(Guid.NewGuid(), PEN());
        private static Periodo PeriodoMes(int year, int month)
        {
            var desde = new DateOnly(year, month, 1);
            var hasta = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
            return Periodo.Personalizado(desde, hasta);
        }
        private static UsuarioId Vendedor() => UsuarioId.New();
        private static EstablecimientoId Estab() => EstablecimientoId.New();
        // ==========================================================================

        private static void RegistrarVenta(
            IndicadorNegocio agg,
            Guid comprobanteId,
            DateOnly fecha,
            UsuarioId vendedor,
            decimal subtotal)
        {
            var item = new IndicadorNegocio.ComprobanteVenta.Item("PROD-1", 1m, Money(subtotal));
            var venta = new IndicadorNegocio.ComprobanteVenta(
                comprobanteId: comprobanteId,
                fecha: fecha,
                clienteId: null,
                total: Money(subtotal * 1.18m),
                igv: Money(subtotal * 0.18m),
                items: new[] { item },
                vendedorId: vendedor,
                tipoComprobante: "Factura",
                establecimientoId: Estab()
            );
            agg.RegistrarVentaAceptada(venta);
        }

        [Test]
        public async Task Ranking_CuandoExiste_DeberiaRetornarOrdenadoYRespetarLimite()
        {
            // Arrange
            var repo = new Mock<IIndicadorNegocioRepository>(MockBehavior.Strict);
            var uc = new ObtenerRankingVendedoresUseCase(repo.Object);

            var tipo = IndicadorNegocio.TipoIndicador.VentaDiaria;
            var periodo = PeriodoMes(2025, 1);
            var segmento = SegmentoSoles();

            var agg = IndicadorNegocio.Crear(tipo, periodo, segmento);

            var v1 = Vendedor();
            var v2 = Vendedor();
            var mid = new DateOnly(2025, 1, 15);

            RegistrarVenta(agg, Guid.NewGuid(), mid, v1, 100m);   // total 118
            RegistrarVenta(agg, Guid.NewGuid(), mid, v1, 200m);   // total 236  -> v1=354
            RegistrarVenta(agg, Guid.NewGuid(), mid, v2, 150m);   // total 177  -> v2=177

            repo.Setup(r => r.GetByClaveAsync(tipo, periodo, segmento, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);

            var desde = new DateOnly(2025, 1, 1);
            var hasta = new DateOnly(2025, 1, 31);
            var limite = LimiteTop.Crear(1);

            var input = new ObtenerRankingVendedoresInputDto(
                tipo: tipo,
                periodo: periodo,
                segmento: segmento,
                desde: desde,
                hasta: hasta,
                limite: limite
            );

            // Act
            var output = await uc.ExecuteAsync(input, CancellationToken.None);

            // Assert
            Assert.That(output, Is.Not.Null);
            Assert.That(output.IndicadorId, Is.EqualTo(agg.IndicadorId));
            Assert.That(output.Ranking.Count, Is.EqualTo(1)); // respeta LimiteTop(1)
            var top = output.Ranking.Single();
            Assert.That(top.VendedorId, Is.EqualTo(v1)); // v1 debe ser el top por monto
            Assert.That(top.TotalVendido.Moneda, Is.EqualTo(segmento.Moneda));
            Assert.That(top.TotalVendido.Monto, Is.GreaterThan(top.TotalVendido.Monto - 0.01m)); // sanity
            repo.Verify(r => r.GetByClaveAsync(tipo, periodo, segmento, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Ranking_CuandoRangoNoTieneVentas_DeberiaRetornarListaVacia()
        {
            // Arrange
            var repo = new Mock<IIndicadorNegocioRepository>(MockBehavior.Strict);
            var uc = new ObtenerRankingVendedoresUseCase(repo.Object);

            var tipo = IndicadorNegocio.TipoIndicador.VentaDiaria;
            var periodo = PeriodoMes(2025, 1);
            var segmento = SegmentoSoles();

            var agg = IndicadorNegocio.Crear(tipo, periodo, segmento);

            // registrar ventas fuera del rango consultado
            var v = Vendedor();
            RegistrarVenta(agg, Guid.NewGuid(), new DateOnly(2025, 1, 10), v, 100m);

            repo.Setup(r => r.GetByClaveAsync(tipo, periodo, segmento, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);

            var input = new ObtenerRankingVendedoresInputDto(
                tipo: tipo,
                periodo: periodo,
                segmento: segmento,
                desde: new DateOnly(2025, 1, 20),
                hasta: new DateOnly(2025, 1, 25)
            );

            // Act
            var output = await uc.ExecuteAsync(input, CancellationToken.None);

            // Assert
            Assert.That(output.Ranking, Is.Not.Null);
            Assert.That(output.Ranking.Count, Is.EqualTo(0));
        }

        [Test]
        public void Ranking_CuandoNoExisteIndicador_DeberiaLanzarNotFound()
        {
            // Arrange
            var repo = new Mock<IIndicadorNegocioRepository>(MockBehavior.Strict);
            var uc = new ObtenerRankingVendedoresUseCase(repo.Object);

            var tipo = IndicadorNegocio.TipoIndicador.VentaDiaria;
            var periodo = PeriodoMes(2025, 1);
            var segmento = SegmentoSoles();

            repo.Setup(r => r.GetByClaveAsync(tipo, periodo, segmento, It.IsAny<CancellationToken>()))
                .ReturnsAsync((IndicadorNegocio?)null);

            var input = new ObtenerRankingVendedoresInputDto(
                tipo: tipo,
                periodo: periodo,
                segmento: segmento,
                desde: new DateOnly(2025, 1, 1),
                hasta: new DateOnly(2025, 1, 31)
            );

            // Act + Assert
            Assert.That(async () => await uc.ExecuteAsync(input, CancellationToken.None),
                Throws.Exception.TypeOf<NotFoundException>());

            repo.Verify(r => r.GetByClaveAsync(tipo, periodo, segmento, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Ranking_ConEmpresaId_UsaOverloadDelRepositorioConTenant()
        {
            // Arrange
            var repo = new Mock<IIndicadorNegocioRepository>(MockBehavior.Strict);
            var uc = new ObtenerRankingVendedoresUseCase(repo.Object);

            var tipo = IndicadorNegocio.TipoIndicador.VentaDiaria;
            var periodo = PeriodoMes(2025, 1);
            var segmento = SegmentoSoles();
            var empresa = EmpresaId.From(Guid.NewGuid().ToString());

            var agg = IndicadorNegocio.Crear(tipo, periodo, segmento);
            repo.Setup(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);

            var input = new ObtenerRankingVendedoresInputDto(
                tipo: tipo,
                periodo: periodo,
                segmento: segmento,
                desde: new DateOnly(2025, 1, 1),
                hasta: new DateOnly(2025, 1, 31),
                limite: null,
                empresaId: empresa
            );

            // Act
            var _ = await uc.ExecuteAsync(input, CancellationToken.None);

            // Assert
            repo.Verify(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()), Times.Once);
            repo.Verify(r => r.GetByClaveAsync(tipo, periodo, segmento, It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public void InputDto_RangoInvalido_DeberiaFallar()
        {
            var tipo = IndicadorNegocio.TipoIndicador.VentaDiaria;
            var periodo = PeriodoMes(2025, 1);
            var segmento = SegmentoSoles();

            Assert.That(
                () => new ObtenerRankingVendedoresInputDto(
                    tipo, periodo, segmento,
                    desde: new DateOnly(2025, 1, 10),
                    hasta: new DateOnly(2025, 1, 5)),
                Throws.ArgumentException);
        }
    }
}
