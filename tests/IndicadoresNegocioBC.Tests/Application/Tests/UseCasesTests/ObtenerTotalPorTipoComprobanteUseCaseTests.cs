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
using SharedKernel.Application.Interfaces;

namespace IndicadoresNegocioBC.Tests.Application.UseCases
{
    [TestFixture]
    public class ObtenerTotalPorTipoComprobanteUseCaseTests
    {
        // ====== Helpers mínimos (ajusta a tus factories reales si difieren) ======
    private static Moneda PEN() => Moneda.PEN();
    private static Dinero Money(decimal m) => Dinero.Create(m, PEN());
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

        private static void RegistrarVenta(
            IndicadorNegocio agg,
            Guid comprobanteId,
            DateOnly fecha,
            string tipoComprobante,
            decimal subtotal,
            EstablecimientoId? establecimientoId = null)
        {
            var est = establecimientoId ?? Estab();
            var item = new IndicadorNegocio.ComprobanteVenta.Item("PROD-1", 1m, Money(subtotal));
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
        public async Task Total_Factura_EnRango_Y_PorEstablecimiento_DeberiaSerCorrecto()
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
            // 10: Factura estA 100 (118 total), Factura estB 50 (59)
            // 12: Boleta estA 200 (236) -> no debe contar (tipo distinto)
            // 20: Factura estA 80 (94.4) -> fuera de rango
            RegistrarVenta(agg, Guid.NewGuid(), new DateOnly(2025, 1, 10), "Factura", 100m, estA);
            RegistrarVenta(agg, Guid.NewGuid(), new DateOnly(2025, 1, 10), "Factura",  50m, estB);
            RegistrarVenta(agg, Guid.NewGuid(), new DateOnly(2025, 1, 12), "Boleta",  200m, estA);
            RegistrarVenta(agg, Guid.NewGuid(), new DateOnly(2025, 1, 20), "Factura", 80m,  estA);

            tenant.SetupGet(t => t.EmpresaId).Returns(empresa);
            var uc = new ObtenerTotalPorTipoComprobanteUseCase(repo.Object, tenant.Object);

            repo.Setup(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);

            var input = new ObtenerTotalPorTipoComprobanteInputDto(
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
            // Solo debe contar: 10/01 Factura estA 100 => total 118
            Assert.That(output.Total.Moneda, Is.EqualTo(segmento.Moneda));
            Assert.That(Math.Round(output.Total.Monto, 2), Is.EqualTo(118m));
            repo.Verify(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Total_Factura_TodoElPeriodo_SinRango_DeberiaSumarTodasLasFacturas()
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
            var uc = new ObtenerTotalPorTipoComprobanteUseCase(repo.Object, tenant.Object);

            repo.Setup(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);

            var input = new ObtenerTotalPorTipoComprobanteInputDto(
                tipo: tipo,
                periodo: periodo,
                segmento: segmento,
                tipoComprobante: "Factura" // sin rango ni establecimiento
            );

            // Act
            var output = await uc.ExecuteAsync(input, CancellationToken.None);

            // Assert
            // Total esperado Facturas: (100+50+80)*1.18 = 177.00 + 94.40 = 271.40? cuidado:
            // (100*1.18)=118; (50*1.18)=59; (80*1.18)=94.4  => 118 + 59 + 94.4 = 271.4
            Assert.That(Math.Round(output.Total.Monto, 2), Is.EqualTo(271.40m));
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
            var uc = new ObtenerTotalPorTipoComprobanteUseCase(repo.Object, tenant.Object);

            repo.Setup(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()))
                .ReturnsAsync((IndicadorNegocio?)null);

            var input = new ObtenerTotalPorTipoComprobanteInputDto(
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
            var uc = new ObtenerTotalPorTipoComprobanteUseCase(repo.Object, tenant.Object);

            var agg = IndicadorNegocio.Crear(tipo, periodo, segmento);
            RegistrarVenta(agg, Guid.NewGuid(), new DateOnly(2025, 4, 2), "Factura", 50m);

            repo.Setup(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);

            var input = new ObtenerTotalPorTipoComprobanteInputDto(
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
                () => new ObtenerTotalPorTipoComprobanteInputDto(
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
                () => new ObtenerTotalPorTipoComprobanteInputDto(
                    tipo, periodo, segmento,
                    tipoComprobante: "Factura",
                    desde: new DateOnly(2025, 6, 10),
                    hasta: new DateOnly(2025, 6, 5)),
                Throws.ArgumentException);
        }
    }
}
