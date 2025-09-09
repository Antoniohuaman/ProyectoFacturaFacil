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
    public class ObtenerVentasDiariasUseCaseTests
    {
        // ====== Builders mínimos usando solo APIs públicas/fábricas de VOs ======
        private static Moneda PEN() => Moneda.Create("PEN");
        private static Dinero Money(decimal m) => new Dinero(m, PEN());
        private static SegmentoIndicador SegmentoSoles() => SegmentoIndicador.ParaEmpresa(Guid.NewGuid(), PEN());
        private static Periodo PeriodoMes(int year, int month)
        {
            var inicio = new DateOnly(year, month, 1);
            var fin = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
            return Periodo.Personalizado(inicio, fin);
        }
        private static EstablecimientoId Estab() => EstablecimientoId.New();
        private static UsuarioId Vendedor() => UsuarioId.New();
        // ==========================================================================

        private static void RegistrarVenta(IndicadorNegocio agg, Guid comprobanteId, DateOnly fecha, decimal subtotal)
        {
            var item = new IndicadorNegocio.ComprobanteVenta.Item("PROD-1", 1m, Money(subtotal));
            var venta = new IndicadorNegocio.ComprobanteVenta(
                comprobanteId: comprobanteId,
                fecha: fecha,
                clienteId: null,
                total: Money(subtotal * 1.18m),
                igv: Money(subtotal * 0.18m),
                items: new[] { item },
                vendedorId: Vendedor(),
                tipoComprobante: "Factura",
                establecimientoId: Estab()
            );
            agg.RegistrarVentaAceptada(venta);
        }

        [Test]
        public async Task Obtener_CuandoHayVentasEnRango_DeberiaDevolverListaOrdenadaYResumenCorrecto()
        {
            // Arrange
            var repo = new Mock<IIndicadorNegocioRepository>(MockBehavior.Strict);
            var uc = new ObtenerVentasDiariasUseCase(repo.Object);

            var tipo = IndicadorNegocio.TipoIndicador.VentaDiaria;
            var periodo = PeriodoMes(2025, 1);
            var segmento = SegmentoSoles();

            var agg = IndicadorNegocio.Crear(tipo, periodo, segmento);

            // Ventas en distintos días
            var d10 = new DateOnly(2025, 1, 10);
            var d11 = new DateOnly(2025, 1, 11);
            var d20 = new DateOnly(2025, 1, 20);

            RegistrarVenta(agg, Guid.NewGuid(), d10, 100m);   // 118 / 18
            RegistrarVenta(agg, Guid.NewGuid(), d10, 50m);    // 59  / 9
            RegistrarVenta(agg, Guid.NewGuid(), d11, 200m);   // 236 / 36
            RegistrarVenta(agg, Guid.NewGuid(), d20, 10m);    // 11.8 / 1.8

            repo.Setup(r => r.GetByClaveAsync(tipo, periodo, segmento, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);

            var input = new ObtenerVentasDiariasInputDto(
                tipo: tipo,
                periodo: periodo,
                segmento: segmento,
                desde: new DateOnly(2025, 1, 10),
                hasta: new DateOnly(2025, 1, 15)
            );

            // Act
            var output = await uc.ExecuteAsync(input, CancellationToken.None);

            // Assert
            Assert.That(output, Is.Not.Null);
            Assert.That(output.IndicadorId, Is.EqualTo(agg.IndicadorId));
            Assert.That(output.VentasDiarias.Count, Is.EqualTo(2)); // días 10 y 11
            Assert.That(output.VentasDiarias[0].Fecha, Is.EqualTo(d10));
            Assert.That(output.VentasDiarias[1].Fecha, Is.EqualTo(d11));

            // Día 10: 100 + 50 => total 150; IGV 27; comprobantes 2
            var dia10 = output.VentasDiarias[0];
            Assert.That(dia10.NroComprobantes, Is.EqualTo(2));
            Assert.That(dia10.TotalVentas.Moneda, Is.EqualTo(segmento.Moneda));
            Assert.That(Math.Round(dia10.TotalVentas.Monto, 2), Is.EqualTo(177.00m)); // 118 + 59
            Assert.That(Math.Round(dia10.TotalIgv.Monto, 2), Is.EqualTo(27.00m));     // 18 + 9

            // Resumen rango (d10+d11)
            var esperadoVentas = 118m + 59m + 236m;   // 413
            var esperadoIgv = 18m + 9m + 36m;         // 63
            var esperadoComp = 3;

            Assert.That(output.TotalComprobantesRango, Is.EqualTo(esperadoComp));
            Assert.That(output.TotalVentasRango.Moneda, Is.EqualTo(segmento.Moneda));
            Assert.That(Math.Round(output.TotalVentasRango.Monto, 2), Is.EqualTo(esperadoVentas));
            Assert.That(Math.Round(output.TotalIgvRango.Monto, 2), Is.EqualTo(esperadoIgv));

            repo.Verify(r => r.GetByClaveAsync(tipo, periodo, segmento, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Obtener_CuandoRangoNoTieneVentas_DeberiaRetornarListaVaciaYResumenCero()
        {
            // Arrange
            var repo = new Mock<IIndicadorNegocioRepository>(MockBehavior.Strict);
            var uc = new ObtenerVentasDiariasUseCase(repo.Object);

            var tipo = IndicadorNegocio.TipoIndicador.VentaDiaria;
            var periodo = PeriodoMes(2025, 2);
            var segmento = SegmentoSoles();

            var agg = IndicadorNegocio.Crear(tipo, periodo, segmento);

            // registramos ventas fuera del rango
            RegistrarVenta(agg, Guid.NewGuid(), new DateOnly(2025, 2, 5), 100m);

            repo.Setup(r => r.GetByClaveAsync(tipo, periodo, segmento, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);

            var input = new ObtenerVentasDiariasInputDto(
                tipo, periodo, segmento,
                desde: new DateOnly(2025, 2, 10),
                hasta: new DateOnly(2025, 2, 12)
            );

            // Act
            var output = await uc.ExecuteAsync(input, CancellationToken.None);

            // Assert
            Assert.That(output.VentasDiarias.Count, Is.EqualTo(0));
            Assert.That(output.TotalComprobantesRango, Is.EqualTo(0));
            Assert.That(output.TotalVentasRango.Monto, Is.EqualTo(0m));
            Assert.That(output.TotalIgvRango.Monto, Is.EqualTo(0m));
        }

        [Test]
        public void Obtener_CuandoNoExisteIndicador_DeberiaLanzarNotFound()
        {
            // Arrange
            var repo = new Mock<IIndicadorNegocioRepository>(MockBehavior.Strict);
            var uc = new ObtenerVentasDiariasUseCase(repo.Object);

            var tipo = IndicadorNegocio.TipoIndicador.VentaDiaria;
            var periodo = PeriodoMes(2025, 3);
            var segmento = SegmentoSoles();

            repo.Setup(r => r.GetByClaveAsync(tipo, periodo, segmento, It.IsAny<CancellationToken>()))
                .ReturnsAsync((IndicadorNegocio?)null);

            var input = new ObtenerVentasDiariasInputDto(
                tipo, periodo, segmento,
                desde: new DateOnly(2025, 3, 1),
                hasta: new DateOnly(2025, 3, 31)
            );

            // Act + Assert
            Assert.That(async () => await uc.ExecuteAsync(input, CancellationToken.None),
                Throws.Exception.TypeOf<NotFoundException>());

            repo.Verify(r => r.GetByClaveAsync(tipo, periodo, segmento, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Obtener_ConEmpresaId_UsaOverloadDelRepositorioConTenant()
        {
            // Arrange
            var repo = new Mock<IIndicadorNegocioRepository>(MockBehavior.Strict);
            var uc = new ObtenerVentasDiariasUseCase(repo.Object);

            var tipo = IndicadorNegocio.TipoIndicador.VentaDiaria;
            var periodo = PeriodoMes(2025, 4);
            var segmento = SegmentoSoles();
            var empresa = EmpresaId.From(Guid.NewGuid().ToString());

            var agg = IndicadorNegocio.Crear(tipo, periodo, segmento);
            RegistrarVenta(agg, Guid.NewGuid(), new DateOnly(2025, 4, 2), 50m);

            repo.Setup(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);

            var input = new ObtenerVentasDiariasInputDto(
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
                () => new ObtenerVentasDiariasInputDto(
                    tipo, periodo, segmento,
                    desde: new DateOnly(2025, 5, 10),
                    hasta: new DateOnly(2025, 5, 5)),
                Throws.ArgumentException);
        }
    }
}
