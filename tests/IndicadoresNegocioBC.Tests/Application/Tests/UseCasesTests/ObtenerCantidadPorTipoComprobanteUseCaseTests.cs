using System;
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
using System.Linq;
using SharedKernel.Application.Interfaces;

namespace IndicadoresNegocioBC.Tests.Application.UseCases
{
    [TestFixture]
    public class ObtenerCantidadPorTipoComprobanteUseCaseTests
    {
        // ====== Helpers mínimos (ajusta a tus factories reales si difieren) ======
    private static Moneda PEN() => Moneda.PEN();
    private static Dinero Money(decimal m) => new Dinero(m, PEN());
    private static SegmentoIndicador SegmentoSoles() => SegmentoIndicador.ParaEmpresa(EmpresaId.From(Guid.NewGuid().ToString()), PEN());
        private static Periodo PeriodoMes(int year, int month)
        {
            var desde = new DateOnly(year, month, 1);
            var hasta = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
            return Periodo.Personalizado(desde, hasta);
        }
        private static EstablecimientoId Estab() => EstablecimientoId.From(Guid.NewGuid());
        private static UsuarioId Vendedor() => UsuarioId.From(Guid.NewGuid());
        // ==========================================================================
    private static readonly Guid Prod1 = Guid.Parse("11111111-1111-1111-1111-111111111111");

        private static void RegistrarVenta(
            IndicadorNegocio agg,
            Guid comprobanteId,
            DateOnly fecha,
            string tipoComprobante,
            decimal subtotal,
            EstablecimientoId? establecimientoId = null)
        {
            var est = establecimientoId ?? Estab();
            var item = new IndicadorNegocio.ComprobanteVenta.Item(Prod1, 1m, Money(subtotal));
            var venta = new IndicadorNegocio.ComprobanteVenta(
                comprobanteId: comprobanteId,
                fecha: fecha,
                clienteId: null,
                total: Money(subtotal * 1.18m), // total con IGV
                igv: Money(subtotal * 0.18m),
                items: new[] { item },
                vendedorId: Vendedor(),
                tipoComprobante: tipoComprobante,
                establecimientoId: est
            );
            agg.RegistrarVentaAceptada(venta);
        }

        [Test]
        public async Task Cantidad_Factura_EnRango_Y_PorEstablecimiento_DeberiaSerCorrecta()
        {
            // Arrange
            var repo = new Mock<IIndicadorNegocioRepository>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            var tipo = IndicadorNegocio.TipoIndicador.VentaDiaria;
            var periodo = PeriodoMes(2025, 1);
            var segmento = SegmentoSoles();
            var empresa = EmpresaId.From(Guid.NewGuid().ToString());

            var agg = IndicadorNegocio.Crear(tipo, periodo, segmento);

            var estA = EstablecimientoId.From(Guid.NewGuid());
            var estB = EstablecimientoId.From(Guid.NewGuid());

            // Ventas:
            // 10: Factura estA, Factura estB
            // 12: Boleta estA
            // 20: Factura estA (fuera de rango si Hasta=15)
            RegistrarVenta(agg, Guid.NewGuid(), new DateOnly(2025, 1, 10), "Factura", 100m, estA);
            RegistrarVenta(agg, Guid.NewGuid(), new DateOnly(2025, 1, 10), "Factura",  50m, estB);
            RegistrarVenta(agg, Guid.NewGuid(), new DateOnly(2025, 1, 12), "Boleta",  200m, estA);
            RegistrarVenta(agg, Guid.NewGuid(), new DateOnly(2025, 1, 20), "Factura", 80m,  estA);

            tenant.SetupGet(t => t.EmpresaId).Returns(empresa);
            var uc = new ObtenerCantidadPorTipoComprobanteUseCase(repo.Object, tenant.Object);

            repo.Setup(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);

            var input = new ObtenerCantidadPorTipoComprobanteInputDto(
                tipo: tipo,
                periodo: periodo,
                segmento: segmento,
                tipoComprobante: "factura",              // minúsculas (el agregado normaliza)
                desde: new DateOnly(2025, 1, 10),
                hasta: new DateOnly(2025, 1, 15),
                establecimientoId: estA                  // filtra estA
            );

            // Act
            var output = await uc.ExecuteAsync(input, CancellationToken.None);

            // Assert
            // Solo debe contar: 10/01 Factura estA => 1
            Assert.That(output.Cantidad, Is.EqualTo(1));
            repo.Verify(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Cantidad_Factura_TodoElPeriodo_SinRango_DeberiaContarTodasLasFacturas()
        {
            // Arrange
            var repo = new Mock<IIndicadorNegocioRepository>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            var tipo = IndicadorNegocio.TipoIndicador.VentaDiaria;
            var periodo = PeriodoMes(2025, 2);
            var segmento = SegmentoSoles();
            var empresa = EmpresaId.From(Guid.NewGuid().ToString());

            var agg = IndicadorNegocio.Crear(tipo, periodo, segmento);

            // Tres facturas y una boleta (boleta no cuenta)
            RegistrarVenta(agg, Guid.NewGuid(), new DateOnly(2025, 2, 5),  "Factura", 100m);
            RegistrarVenta(agg, Guid.NewGuid(), new DateOnly(2025, 2, 10), "Factura",  50m);
            RegistrarVenta(agg, Guid.NewGuid(), new DateOnly(2025, 2, 15), "Boleta",  20m);
            RegistrarVenta(agg, Guid.NewGuid(), new DateOnly(2025, 2, 25), "Factura",  80m);

            tenant.SetupGet(t => t.EmpresaId).Returns(empresa);
            var uc = new ObtenerCantidadPorTipoComprobanteUseCase(repo.Object, tenant.Object);

            repo.Setup(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);

            var input = new ObtenerCantidadPorTipoComprobanteInputDto(
                tipo: tipo,
                periodo: periodo,
                segmento: segmento,
                tipoComprobante: "Factura" // sin rango ni establecimiento
            );

            // Act
            var output = await uc.ExecuteAsync(input, CancellationToken.None);

            // Assert
            Assert.That(output.Cantidad, Is.EqualTo(3));
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
            var empresa = EmpresaId.From(Guid.NewGuid().ToString());

            tenant.SetupGet(t => t.EmpresaId).Returns(empresa);
            var uc = new ObtenerCantidadPorTipoComprobanteUseCase(repo.Object, tenant.Object);

            repo.Setup(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()))
                .ReturnsAsync((IndicadorNegocio?)null);

            var input = new ObtenerCantidadPorTipoComprobanteInputDto(
                tipo: tipo,
                periodo: periodo,
                segmento: segmento,
                tipoComprobante: "BOLETA",
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
            var uc = new ObtenerCantidadPorTipoComprobanteUseCase(repo.Object, tenant.Object);

            var agg = IndicadorNegocio.Crear(tipo, periodo, segmento);
            RegistrarVenta(agg, Guid.NewGuid(), new DateOnly(2025, 4, 2), "Factura", 50m);

            repo.Setup(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);

            var input = new ObtenerCantidadPorTipoComprobanteInputDto(
                tipo: tipo,
                periodo: periodo,
                segmento: segmento,
                tipoComprobante: "Factura",
                empresaId: empresa
            );

            // Act
            var _ = await uc.ExecuteAsync(input, CancellationToken.None);

            // Assert
            repo.Verify(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()), Times.Once);
            repo.Verify(r => r.GetByClaveAsync(tipo, periodo, segmento, It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public void InputDto_TipoComprobanteVacio_DeberiaFallar()
        {
            var tipo = IndicadorNegocio.TipoIndicador.VentaDiaria;
            var periodo = PeriodoMes(2025, 5);
            var segmento = SegmentoSoles();

            Assert.That(
                () => new ObtenerCantidadPorTipoComprobanteInputDto(
                    tipo, periodo, segmento,
                    tipoComprobante: "   "),
                Throws.ArgumentNullException);
        }

        [Test]
        public void InputDto_RangoInvalido_DeberiaFallar()
        {
            var tipo = IndicadorNegocio.TipoIndicador.VentaDiaria;
            var periodo = PeriodoMes(2025, 6);
            var segmento = SegmentoSoles();

            Assert.That(
                () => new ObtenerCantidadPorTipoComprobanteInputDto(
                    tipo, periodo, segmento,
                    tipoComprobante: "Factura",
                    desde: new DateOnly(2025, 6, 10),
                    hasta: new DateOnly(2025, 6, 5)),
                Throws.ArgumentException);
        }
    }
}
