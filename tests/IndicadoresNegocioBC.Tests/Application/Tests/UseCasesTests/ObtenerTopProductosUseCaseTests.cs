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
    public class ObtenerTopProductosUseCaseTests
    {
        // ====== Builders mínimos (ajusta a tus factories reales si difieren) ======
        private static Moneda PEN() => Moneda.PEN();
    private static Dinero Money(decimal m) => Dinero.Create(m, PEN());
    private static readonly Guid EmpresaGuid = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static SegmentoIndicador SegmentoSoles() => SegmentoIndicador.ParaEmpresa(EmpresaGuid, PEN());
        private static Periodo PeriodoMes(int year, int month)
        {
            var desde = new DateOnly(year, month, 1);
            var hasta = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
            return Periodo.Personalizado(desde, hasta);
        }
        private static EstablecimientoId Estab() => EstablecimientoId.From(Guid.NewGuid());
        private static UsuarioId Vendedor() => UsuarioId.From(Guid.NewGuid());
        // ==========================================================================

        private static void RegistrarVenta(IndicadorNegocio agg, Guid comprobanteId, DateOnly fecha,
                                           (string prod, decimal cant, decimal subTotal)[] detalles,
                                           string tipoComprobante = "Factura")
        {
            var items = detalles.Select(d =>
                new IndicadorNegocio.ComprobanteVenta.Item(d.prod, d.cant, Money(d.subTotal))
            ).ToList();

            var totalSub = detalles.Sum(d => d.subTotal);
            var venta = new IndicadorNegocio.ComprobanteVenta(
                comprobanteId: comprobanteId,
                fecha: fecha,
                clienteId: null,
                total: Money(totalSub * 1.18m),
                igv: Money(totalSub * 0.18m),
                items: items,
                vendedorId: Vendedor(),
                tipoComprobante: tipoComprobante,
                establecimientoId: Estab()
            );

            agg.RegistrarVentaAceptada(venta);
        }

        [Test]
        public async Task Top_RangoPorMonto_RespetaLimiteYOrden()
        {
            // Arrange
            var repo = new Mock<IIndicadorNegocioRepository>(MockBehavior.Strict);
            var uc = new ObtenerTopProductosUseCase(repo.Object);

            var tipo = IndicadorNegocio.TipoIndicador.RankingProductos; // el agregado soporta ranking
            var periodo = PeriodoMes(2025, 1);
            var segmento = SegmentoSoles();

            var agg = IndicadorNegocio.Crear(tipo, periodo, segmento);

            // Producto A total 300, Producto B total 180, Producto C total 50 (por Subtotal)
            RegistrarVenta(agg, Guid.NewGuid(), new DateOnly(2025, 1, 10), new[]
            {
                ("A", 1m, 100m),
                ("B", 2m, 60m)
            });
            RegistrarVenta(agg, Guid.NewGuid(), new DateOnly(2025, 1, 11), new[]
            {
                ("A", 2m, 200m),
                ("C", 1m, 50m)
            });
            RegistrarVenta(agg, Guid.NewGuid(), new DateOnly(2025, 1, 12), new[]
            {
                ("B", 2m, 60m)
            });

            repo.Setup(r => r.GetByClaveAsync(tipo, periodo, segmento, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);

            var input = new ObtenerTopProductosInputDto(
                tipo: tipo,
                periodo: periodo,
                segmento: segmento,
                criterio: IndicadorNegocio.RankingCriterio.PorMonto,
                desde: new DateOnly(2025, 1, 1),
                hasta: new DateOnly(2025, 1, 31),
                limite: LimiteTop.Crear(2) // top 2
            );

            // Act
            var output = await uc.ExecuteAsync(input, CancellationToken.None);

            // Assert
            Assert.That(output, Is.Not.Null);
            Assert.That(output.TopProductos.Count, Is.EqualTo(2));
            Assert.That(output.TopProductos[0].ProductoId, Is.EqualTo("A")); // 300
            Assert.That(output.TopProductos[1].ProductoId, Is.EqualTo("B")); // 120
            repo.Verify(r => r.GetByClaveAsync(tipo, periodo, segmento, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Top_GlobalPorCantidad_SinRango_UsaDefaultLimite()
        {
            // Arrange
            var repo = new Mock<IIndicadorNegocioRepository>(MockBehavior.Strict);
            var uc = new ObtenerTopProductosUseCase(repo.Object);

            var tipo = IndicadorNegocio.TipoIndicador.RankingProductos;
            var periodo = PeriodoMes(2025, 2);
            var segmento = SegmentoSoles();

            var agg = IndicadorNegocio.Crear(tipo, periodo, segmento);

            // Cantidades: A=5, B=3, C=2
            RegistrarVenta(agg, Guid.NewGuid(), new DateOnly(2025, 2, 5), new[]
            {
                ("A", 2m, 100m),
                ("B", 1m, 50m)
            });
            RegistrarVenta(agg, Guid.NewGuid(), new DateOnly(2025, 2, 6), new[]
            {
                ("A", 3m, 150m),
                ("B", 2m, 100m),
                ("C", 2m, 60m)
            });

            repo.Setup(r => r.GetByClaveAsync(tipo, periodo, segmento, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);

            var input = new ObtenerTopProductosInputDto(
                tipo: tipo,
                periodo: periodo,
                segmento: segmento,
                criterio: IndicadorNegocio.RankingCriterio.PorCantidad
                // sin rango, sin limite -> usa Top global con limite por defecto (10)
            );

            // Act
            var output = await uc.ExecuteAsync(input, CancellationToken.None);

            // Assert
            Assert.That(output.TopProductos.Count, Is.GreaterThanOrEqualTo(3)); // default >= 3
            Assert.That(output.TopProductos[0].ProductoId, Is.EqualTo("A")); // cantidad 5
            Assert.That(output.TopProductos[1].ProductoId, Is.EqualTo("B")); // cantidad 3
            Assert.That(output.TopProductos[2].ProductoId, Is.EqualTo("C")); // cantidad 2
        }

        [Test]
        public void Top_CuandoNoExisteIndicador_DeberiaLanzarNotFound()
        {
            // Arrange
            var repo = new Mock<IIndicadorNegocioRepository>(MockBehavior.Strict);
            var uc = new ObtenerTopProductosUseCase(repo.Object);

            var tipo = IndicadorNegocio.TipoIndicador.RankingProductos;
            var periodo = PeriodoMes(2025, 3);
            var segmento = SegmentoSoles();

            repo.Setup(r => r.GetByClaveAsync(tipo, periodo, segmento, It.IsAny<CancellationToken>()))
                .ReturnsAsync((IndicadorNegocio?)null);

            var input = new ObtenerTopProductosInputDto(
                tipo: tipo,
                periodo: periodo,
                segmento: segmento,
                criterio: IndicadorNegocio.RankingCriterio.PorMonto,
                desde: new DateOnly(2025, 3, 1),
                hasta: new DateOnly(2025, 3, 31),
                limite: LimiteTop.Crear(5)
            );

            // Act + Assert
            Assert.That(async () => await uc.ExecuteAsync(input, CancellationToken.None),
                Throws.Exception.TypeOf<NotFoundException>());

            repo.Verify(r => r.GetByClaveAsync(tipo, periodo, segmento, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Top_ConEmpresaId_UsaOverloadRepositorioConTenant()
        {
            // Arrange
            var repo = new Mock<IIndicadorNegocioRepository>(MockBehavior.Strict);
            var uc = new ObtenerTopProductosUseCase(repo.Object);

            var tipo = IndicadorNegocio.TipoIndicador.RankingProductos;
            var periodo = PeriodoMes(2025, 4);
            var segmento = SegmentoSoles();
            var empresa = EmpresaId.From(Guid.NewGuid().ToString());

            var agg = IndicadorNegocio.Crear(tipo, periodo, segmento);
            RegistrarVenta(agg, Guid.NewGuid(), new DateOnly(2025, 4, 10), new[]
            {
                ("X", 1m, 80m)
            });

            repo.Setup(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);

            var input = new ObtenerTopProductosInputDto(
                tipo: tipo,
                periodo: periodo,
                segmento: segmento,
                criterio: IndicadorNegocio.RankingCriterio.PorMonto,
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
            var tipo = IndicadorNegocio.TipoIndicador.RankingProductos;
            var periodo = PeriodoMes(2025, 5);
            var segmento = SegmentoSoles();

            Assert.That(
                () => new ObtenerTopProductosInputDto(
                    tipo, periodo, segmento,
                    criterio: IndicadorNegocio.RankingCriterio.PorMonto,
                    desde: new DateOnly(2025, 5, 10),
                    hasta: new DateOnly(2025, 5, 5)),
                Throws.ArgumentException);
        }
    }
}
