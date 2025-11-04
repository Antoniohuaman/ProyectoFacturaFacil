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
    public class ObtenerVentasPorRangoUseCaseTests
    {
        // ====== Helpers mínimos (ajusta a tus factories reales si difieren) ======
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
            (Guid prod, decimal cant, decimal subTotal)[] detalles,
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
        public async Task CuandoHayVentasEnRango_DeberiaRetornarListaOrdenadaYResumenCorrecto()
        {
            // Arrange
            var repo = new Mock<IIndicadorNegocioRepository>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            var tipo = IndicadorNegocio.TipoIndicador.VentaDiaria;
            var periodo = PeriodoMes(2025, 1);
            var segmento = SegmentoSoles();
            var empresa = EmpresaId.From("11111111-1111-1111-1111-111111111111");

            var agg = IndicadorNegocio.Crear(tipo, periodo, segmento);

            var d10 = new DateOnly(2025, 1, 10);
            var d15 = new DateOnly(2025, 1, 15);
            var d20 = new DateOnly(2025, 1, 20);

            // Dentro del rango [10..20]: dos ventas (10 y 15). Fuera: una (20 si consultamos hasta 18)
            RegistrarVenta(agg, Guid.NewGuid(), d10, Guid.NewGuid(), new[] { (Guid.NewGuid(), 1m, 100m) }); // 118 / 18
            RegistrarVenta(agg, Guid.NewGuid(), d15, Guid.NewGuid(), new[] { (Guid.NewGuid(), 2m, 150m) }); // 177 / 27
            RegistrarVenta(agg, Guid.NewGuid(), d20, Guid.NewGuid(), new[] { (Guid.NewGuid(), 1m, 50m) });   // 59  / 9

            tenant.SetupGet(t => t.EmpresaId).Returns(empresa);
            var uc = new ObtenerVentasPorRangoUseCase(repo.Object, tenant.Object);

            repo.Setup(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);

            var input = new ObtenerVentasPorRangoInputDto(
                tipo: tipo,
                periodo: periodo,
                segmento: segmento,
                desde: new DateOnly(2025, 1, 10),
                hasta: new DateOnly(2025, 1, 18) // excluye la del 20
            );

            // Act
            var output = await uc.ExecuteAsync(input, CancellationToken.None);

            // Assert
            Assert.That(output, Is.Not.Null);
            Assert.That(output.IndicadorId, Is.EqualTo(agg.IndicadorId));
            Assert.That(output.Ventas.Count, Is.EqualTo(2));
            Assert.That(output.Ventas[0].Fecha, Is.EqualTo(d10));
            Assert.That(output.Ventas[1].Fecha, Is.EqualTo(d15));

            // Resumen: ventas 118 + 177 = 295; IGV 18 + 27 = 45; comprobantes 2
            Assert.That(output.TotalComprobantesRango, Is.EqualTo(2));
            Assert.That(output.TotalVentasRango.Moneda, Is.EqualTo(segmento.Moneda));
            Assert.That(Math.Round(output.TotalVentasRango.Monto, 2), Is.EqualTo(295m));
            Assert.That(Math.Round(output.TotalIgvRango.Monto, 2), Is.EqualTo(45m));

            repo.Verify(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task CuandoNoHayVentasEnRango_DeberiaRetornarListaVaciaYResumenCero()
        {
            // Arrange
            var repo = new Mock<IIndicadorNegocioRepository>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            var tipo = IndicadorNegocio.TipoIndicador.VentaDiaria;
            var periodo = PeriodoMes(2025, 2);
            var segmento = SegmentoSoles();
            var empresa = EmpresaId.From("11111111-1111-1111-1111-111111111111");

            var agg = IndicadorNegocio.Crear(tipo, periodo, segmento);

            // Venta fuera del rango
            RegistrarVenta(agg, Guid.NewGuid(), new DateOnly(2025, 2, 5), Guid.NewGuid(), new[] { (Guid.NewGuid(), 1m, 100m) });

            tenant.SetupGet(t => t.EmpresaId).Returns(empresa);
            var uc = new ObtenerVentasPorRangoUseCase(repo.Object, tenant.Object);

            repo.Setup(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);

            var input = new ObtenerVentasPorRangoInputDto(
                tipo, periodo, segmento,
                desde: new DateOnly(2025, 2, 10),
                hasta: new DateOnly(2025, 2, 12)
            );

            // Act
            var output = await uc.ExecuteAsync(input, CancellationToken.None);

            // Assert
            Assert.That(output.Ventas.Count, Is.EqualTo(0));
            Assert.That(output.TotalComprobantesRango, Is.EqualTo(0));
            Assert.That(output.TotalVentasRango.Monto, Is.EqualTo(0m));
            Assert.That(output.TotalIgvRango.Monto, Is.EqualTo(0m));
        }

        [Test]
        public void CuandoNoExisteIndicador_DeberiaLanzarNotFound()
        {
            // Arrange
            var repo = new Mock<IIndicadorNegocioRepository>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            var tipo = IndicadorNegocio.TipoIndicador.VentaDiaria;
            var periodo = PeriodoMes(2025, 3);
            var segmento = SegmentoSoles();
            var empresa = EmpresaId.From("11111111-1111-1111-1111-111111111111");

            tenant.SetupGet(t => t.EmpresaId).Returns(empresa);
            var uc = new ObtenerVentasPorRangoUseCase(repo.Object, tenant.Object);

            repo.Setup(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()))
                .ReturnsAsync((IndicadorNegocio?)null);

            var input = new ObtenerVentasPorRangoInputDto(
                tipo, periodo, segmento,
                desde: new DateOnly(2025, 3, 1),
                hasta: new DateOnly(2025, 3, 31)
            );

            // Act + Assert
            Assert.That(async () => await uc.ExecuteAsync(input, CancellationToken.None),
                Throws.Exception.TypeOf<NotFoundException>());

            repo.Verify(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task ConEmpresaId_DeberiaUsarOverloadRepositorioConTenant()
        {
            // Arrange
            var repo = new Mock<IIndicadorNegocioRepository>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            var tipo = IndicadorNegocio.TipoIndicador.VentaDiaria;
            var periodo = PeriodoMes(2025, 4);
            var segmento = SegmentoSoles();
            var empresa = EmpresaId.From(Guid.NewGuid().ToString());

            tenant.SetupGet(t => t.EmpresaId).Returns(empresa);
            var uc = new ObtenerVentasPorRangoUseCase(repo.Object, tenant.Object);

            var agg = IndicadorNegocio.Crear(tipo, periodo, segmento);
            RegistrarVenta(agg, Guid.NewGuid(), new DateOnly(2025, 4, 2), Guid.NewGuid(), new[] { (Guid.NewGuid(), 1m, 50m) });

            repo.Setup(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);

            var input = new ObtenerVentasPorRangoInputDto(
                tipo, periodo, segmento,
                desde: new DateOnly(2025, 4, 1),
                hasta: new DateOnly(2025, 4, 30),
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
            var periodo = PeriodoMes(2025, 5);
            var segmento = SegmentoSoles();

            Assert.That(
                () => new ObtenerVentasPorRangoInputDto(
                    tipo, periodo, segmento,
                    desde: new DateOnly(2025, 5, 10),
                    hasta: new DateOnly(2025, 5, 5)),
                Throws.ArgumentException);
        }
    }
}
