using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using IndicadoresNegocioBC.Application.UseCases.IndicadorNegocio;
using IndicadoresNegocioBC.Domain.Aggregates;
using IndicadoresNegocioBC.Domain.Repositories;
using IndicadoresNegocioBC.Domain.ValueObjects;
using SharedKernel.Events;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace IndicadoresNegocioBC.Tests.Application.UseCases
{
    [TestFixture]
    public class ConsolidarIndicadorUseCaseTests
    {
        // ====== Builders mínimos (ajusta a tus factories reales si difieren) ======
    private static Moneda PEN() => Moneda.PEN();
    private static SegmentoIndicador SegmentoSoles() => SegmentoIndicador.ParaEmpresa(Guid.NewGuid(), PEN());
        private static Periodo PeriodoMes(int year, int month)
        {
            var desde = new DateOnly(year, month, 1);
            var hasta = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
            return Periodo.Personalizado(desde, hasta);
        }
    private static EstablecimientoId Estab() => EstablecimientoId.From(Guid.NewGuid());
    private static UsuarioId Vendedor() => UsuarioId.From(Guid.NewGuid());
    private static Dinero Money(decimal m) => new Dinero(m, PEN());
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
        public async Task Consolidar_CuandoNoConsolidado_DeberiaPersistir_PublicarEvento_YActualizarEstado()
        {
            // Arrange
            var repo = new Mock<IIndicadorNegocioRepository>(MockBehavior.Strict);
            var bus  = new Mock<IEventBus>(MockBehavior.Strict);
            var uc   = new ConsolidarIndicadorUseCase(repo.Object, bus.Object);

            var tipo = IndicadorNegocio.TipoIndicador.VentaDiaria;
            var periodo = PeriodoMes(2025, 1);
            var segmento = SegmentoSoles();

            var agg = IndicadorNegocio.Crear(tipo, periodo, segmento);
            RegistrarVenta(agg, Guid.NewGuid(), new DateOnly(2025, 1, 10), 100m); // Version = 1

            repo.Setup(r => r.GetByClaveAsync(tipo, periodo, segmento, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);

            repo.Setup(r => r.UpdateAsync(agg, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            bus.Setup(b => b.PublishAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

            var input = new ConsolidarIndicadorInputDto(tipo, periodo, segmento, ahora: new DateTimeOffset(2025, 1, 31, 23, 59, 59, TimeSpan.Zero));

            // Act
            var output = await uc.ExecuteAsync(input, CancellationToken.None);

            // Assert
            Assert.That(output, Is.Not.Null);
            Assert.That(output.FueIdempotente, Is.False);
            Assert.That(output.Estado, Is.EqualTo(EstadoIndicador.Consolidado));
            Assert.That(output.ConsolidadoEn.HasValue, Is.True);
            Assert.That(output.Version, Is.EqualTo(2)); // 1 (venta) + 1 (consolidación)

            repo.Verify(r => r.UpdateAsync(agg, It.IsAny<CancellationToken>()), Times.Once);
            bus.Verify(b => b.PublishAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Consolidar_CuandoYaEstaConsolidado_DeberiaSerIdempotente_SinPersistirNiPublicar()
        {
            // Arrange
            var repo = new Mock<IIndicadorNegocioRepository>(MockBehavior.Strict);
            var bus  = new Mock<IEventBus>(MockBehavior.Strict);
            var uc   = new ConsolidarIndicadorUseCase(repo.Object, bus.Object);

            var tipo = IndicadorNegocio.TipoIndicador.VentaDiaria;
            var periodo = PeriodoMes(2025, 2);
            var segmento = SegmentoSoles();

            var agg = IndicadorNegocio.Crear(tipo, periodo, segmento);
            // Consolidar una vez de manera directa para preparar el estado
            agg.ConsolidarPeriodo(new DateTimeOffset(2025, 2, 28, 23, 59, 59, TimeSpan.Zero));

            repo.Setup(r => r.GetByClaveAsync(tipo, periodo, segmento, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);

            var input = new ConsolidarIndicadorInputDto(tipo, periodo, segmento);

            // Act
            var output = await uc.ExecuteAsync(input, CancellationToken.None);

            // Assert
            Assert.That(output, Is.Not.Null);
            Assert.That(output.FueIdempotente, Is.True);
            Assert.That(output.Estado, Is.EqualTo(EstadoIndicador.Consolidado));

            repo.Verify(r => r.UpdateAsync(It.IsAny<IndicadorNegocio>(), It.IsAny<CancellationToken>()), Times.Never);
            bus.Verify(b => b.PublishAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public void Consolidar_CuandoNoExisteIndicador_DeberiaLanzarNotFound()
        {
            // Arrange
            var repo = new Mock<IIndicadorNegocioRepository>(MockBehavior.Strict);
            var bus  = new Mock<IEventBus>(MockBehavior.Strict);
            var uc   = new ConsolidarIndicadorUseCase(repo.Object, bus.Object);

            var tipo = IndicadorNegocio.TipoIndicador.VentaDiaria;
            var periodo = PeriodoMes(2025, 3);
            var segmento = SegmentoSoles();

            repo.Setup(r => r.GetByClaveAsync(tipo, periodo, segmento, It.IsAny<CancellationToken>()))
                .ReturnsAsync((IndicadorNegocio?)null);

            var input = new ConsolidarIndicadorInputDto(tipo, periodo, segmento);

            // Act + Assert
            Assert.That(async () => await uc.ExecuteAsync(input, CancellationToken.None),
                Throws.Exception.TypeOf<NotFoundException>());

            bus.Verify(b => b.PublishAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()), Times.Never);
            repo.Verify(r => r.UpdateAsync(It.IsAny<IndicadorNegocio>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Consolidar_ConEmpresaId_UsaOverloadRepositorioConTenant()
        {
            // Arrange
            var repo = new Mock<IIndicadorNegocioRepository>(MockBehavior.Strict);
            var bus  = new Mock<IEventBus>(MockBehavior.Strict);
            var uc   = new ConsolidarIndicadorUseCase(repo.Object, bus.Object);

            var tipo = IndicadorNegocio.TipoIndicador.VentaDiaria;
            var periodo = PeriodoMes(2025, 4);
            var segmento = SegmentoSoles();
            var empresa = EmpresaId.From(Guid.NewGuid().ToString());

            var agg = IndicadorNegocio.Crear(tipo, periodo, segmento);

            repo.Setup(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);

            repo.Setup(r => r.UpdateAsync(agg, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            bus.Setup(b => b.PublishAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

            var input = new ConsolidarIndicadorInputDto(tipo, periodo, segmento, empresa);

            // Act
            var _ = await uc.ExecuteAsync(input, CancellationToken.None);

            // Assert
            repo.Verify(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()), Times.Once);
            repo.Verify(r => r.GetByClaveAsync(tipo, periodo, segmento, It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
