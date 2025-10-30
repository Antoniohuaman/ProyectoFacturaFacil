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
using SharedKernel.Application.Interfaces;

namespace IndicadoresNegocioBC.Tests.Application.UseCases
{
    [TestFixture]
    public class ObtenerTopClientesUseCaseTests
    {
        // ====== Builders mínimos (ajusta a tus factories reales si difieren) ======
    private static Moneda PEN() => Moneda.PEN();
    private static Dinero Money(decimal m) => Dinero.Create(m, PEN());
    private static SegmentoIndicador SegmentoSoles() => SegmentoIndicador.ParaEmpresa(EmpresaId.From("11111111-1111-1111-1111-111111111111"), PEN());
        private static Periodo PeriodoMes(int year, int month)
        {
            var desde = new DateOnly(year, month, 1);
            var hasta = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
            return Periodo.Personalizado(desde, hasta);
        }
        private static EstablecimientoId Estab() => EstablecimientoId.From(Guid.NewGuid());
        private static UsuarioId Vendedor() => UsuarioId.From(Guid.NewGuid());
        // ==========================================================================

        private static void RegistrarVenta(
            IndicadorNegocio agg,
            Guid comprobanteId,
            DateOnly fecha,
            Guid? clienteId,
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
                clienteId: clienteId,
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
        public async Task Top_Rango_DeberiaOrdenarPorMontoYLuegoFrecuencia_YRespetarLimite()
        {
            // Arrange
            var repo = new Mock<IIndicadorNegocioRepository>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            var tipo = IndicadorNegocio.TipoIndicador.RankingClientes;
            var periodo = PeriodoMes(2025, 1);
            var segmento = SegmentoSoles();
            var empresa = EmpresaId.From("11111111-1111-1111-1111-111111111111");

            tenant.SetupGet(t => t.EmpresaId).Returns(empresa);
            var uc = new ObtenerTopClientesUseCase(repo.Object, tenant.Object);

            var agg = IndicadorNegocio.Crear(tipo, periodo, segmento);

            var c1 = Guid.NewGuid();
            var c2 = Guid.NewGuid();
            var c3 = Guid.NewGuid();

            // c1: total 300 (2 compras)
            RegistrarVenta(agg, Guid.NewGuid(), new DateOnly(2025, 1, 10), c1, new[] { ("A", 1m, 100m) });
            RegistrarVenta(agg, Guid.NewGuid(), new DateOnly(2025, 1, 12), c1, new[] { ("B", 1m, 200m) });

            // c2: total 250 (3 compras) -> por monto queda detrás de c1
            RegistrarVenta(agg, Guid.NewGuid(), new DateOnly(2025, 1, 10), c2, new[] { ("A", 1m, 50m) });
            RegistrarVenta(agg, Guid.NewGuid(), new DateOnly(2025, 1, 11), c2, new[] { ("B", 1m, 100m) });
            RegistrarVenta(agg, Guid.NewGuid(), new DateOnly(2025, 1, 13), c2, new[] { ("C", 1m, 100m) });

            // c3: total 50 (1 compra)
            RegistrarVenta(agg, Guid.NewGuid(), new DateOnly(2025, 1, 15), c3, new[] { ("X", 1m, 50m) });

            repo.Setup(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);

            var input = new ObtenerTopClientesInputDto(
                tipo: tipo,
                periodo: periodo,
                segmento: segmento,
                desde: new DateOnly(2025, 1, 1),
                hasta: new DateOnly(2025, 1, 31),
                limite: LimiteTop.Crear(2)
            );

            // Act
            var output = await uc.ExecuteAsync(input, CancellationToken.None);

            // Assert
            Assert.That(output, Is.Not.Null);
            Assert.That(output.TopClientes.Count, Is.EqualTo(2));
            Assert.That(output.TopClientes[0].ClienteId, Is.EqualTo(c1)); // mayor monto (300)
            Assert.That(output.TopClientes[1].ClienteId, Is.EqualTo(c2)); // segundo por monto (250)

            repo.Verify(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Top_Global_SinRango_UsaLimiteDefaultYOrdenDelAgregado()
        {
            // Arrange
            var repo = new Mock<IIndicadorNegocioRepository>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            var tipo = IndicadorNegocio.TipoIndicador.RankingClientes;
            var periodo = PeriodoMes(2025, 2);
            var segmento = SegmentoSoles();
            var empresa = EmpresaId.From("11111111-1111-1111-1111-111111111111");

            tenant.SetupGet(t => t.EmpresaId).Returns(empresa);
            var uc = new ObtenerTopClientesUseCase(repo.Object, tenant.Object);

            var agg = IndicadorNegocio.Crear(tipo, periodo, segmento);

            var c1 = Guid.NewGuid(); // total 200 (2)
            var c2 = Guid.NewGuid(); // total 150 (1)
            var c3 = Guid.NewGuid(); // total 120 (3)

            RegistrarVenta(agg, Guid.NewGuid(), new DateOnly(2025, 2, 5), c1, new[] { ("A", 1m, 50m) });
            RegistrarVenta(agg, Guid.NewGuid(), new DateOnly(2025, 2, 6), c1, new[] { ("B", 1m, 150m) });

            RegistrarVenta(agg, Guid.NewGuid(), new DateOnly(2025, 2, 7), c2, new[] { ("X", 1m, 150m) });

            RegistrarVenta(agg, Guid.NewGuid(), new DateOnly(2025, 2, 10), c3, new[] { ("R", 1m, 40m) });
            RegistrarVenta(agg, Guid.NewGuid(), new DateOnly(2025, 2, 11), c3, new[] { ("S", 1m, 40m) });
            RegistrarVenta(agg, Guid.NewGuid(), new DateOnly(2025, 2, 12), c3, new[] { ("T", 1m, 40m) });

            repo.Setup(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);

            var input = new ObtenerTopClientesInputDto(
                tipo: tipo,
                periodo: periodo,
                segmento: segmento
                // sin rango, sin limite -> usa Top global con limite por defecto (10)
            );

            // Act
            var output = await uc.ExecuteAsync(input, CancellationToken.None);

            // Assert
            Assert.That(output.TopClientes.Count, Is.GreaterThanOrEqualTo(3));
            // Orden global del agregado: por monto y luego frecuencia
            Assert.That(output.TopClientes[0].ClienteId, Is.EqualTo(c1)); // 200
            Assert.That(output.TopClientes[1].ClienteId, Is.EqualTo(c2)); // 150
            Assert.That(output.TopClientes[2].ClienteId, Is.EqualTo(c3)); // 120
        }

        [Test]
        public void Top_CuandoNoExisteIndicador_DeberiaLanzarNotFound()
        {
            // Arrange
            var repo = new Mock<IIndicadorNegocioRepository>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            var tipo = IndicadorNegocio.TipoIndicador.RankingClientes;
            var periodo = PeriodoMes(2025, 3);
            var segmento = SegmentoSoles();
            var empresa = EmpresaId.From("11111111-1111-1111-1111-111111111111");

            tenant.SetupGet(t => t.EmpresaId).Returns(empresa);
            var uc = new ObtenerTopClientesUseCase(repo.Object, tenant.Object);

            repo.Setup(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()))
                .ReturnsAsync((IndicadorNegocio?)null);

            var input = new ObtenerTopClientesInputDto(
                tipo: tipo,
                periodo: periodo,
                segmento: segmento,
                desde: new DateOnly(2025, 3, 1),
                hasta: new DateOnly(2025, 3, 31),
                limite: LimiteTop.Crear(5)
            );

            // Act + Assert
            Assert.That(async () => await uc.ExecuteAsync(input, CancellationToken.None),
                Throws.Exception.TypeOf<NotFoundException>());

            repo.Verify(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Top_ConEmpresaId_UsaOverloadRepositorioConTenant()
        {
            // Arrange
            var repo = new Mock<IIndicadorNegocioRepository>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            var tipo = IndicadorNegocio.TipoIndicador.RankingClientes;
            var periodo = PeriodoMes(2025, 4);
            var segmento = SegmentoSoles();
            var empresa = EmpresaId.From(Guid.NewGuid().ToString());

            tenant.SetupGet(t => t.EmpresaId).Returns(empresa);
            var uc = new ObtenerTopClientesUseCase(repo.Object, tenant.Object);

            var agg = IndicadorNegocio.Crear(tipo, periodo, segmento);
            RegistrarVenta(agg, Guid.NewGuid(), new DateOnly(2025, 4, 5), Guid.NewGuid(), new[] { ("X", 1m, 80m) });

            repo.Setup(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);

            var input = new ObtenerTopClientesInputDto(
                tipo: tipo,
                periodo: periodo,
                segmento: segmento,
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
            var tipo = IndicadorNegocio.TipoIndicador.RankingClientes;
            var periodo = PeriodoMes(2025, 5);
            var segmento = SegmentoSoles();

            Assert.That(
                () => new ObtenerTopClientesInputDto(
                    tipo, periodo, segmento,
                    desde: new DateOnly(2025, 5, 10),
                    hasta: new DateOnly(2025, 5, 5)),
                Throws.ArgumentException);
        }
    }
}
