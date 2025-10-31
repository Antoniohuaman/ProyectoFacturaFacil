using System;
using System.Collections.Generic;
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
using SharedKernel.Application.Interfaces;

namespace IndicadoresNegocioBC.Tests.Application.UseCases
{
    [TestFixture]
    public class RegistrarAnulacionVentaUseCaseTests
    {
        // ====== Builders mínimos usando solo APIs públicas/fábricas de VOs ======
        private static Moneda PEN() => Moneda.Create("PEN");
        private static Dinero Money(decimal m) => new Dinero(m, PEN());
    private static SegmentoIndicador SegmentoSoles() => SegmentoIndicador.ParaEmpresa(EmpresaId.From(Guid.NewGuid().ToString()), PEN());
        private static Periodo PeriodoMesActual()
        {
            var hoy = IndicadoresNegocioBC.Tests.TestUtils.TestTime.BaseUtc();
            var inicio = new DateOnly(hoy.Year, hoy.Month, 1);
            var fin = new DateOnly(hoy.Year, hoy.Month, DateTime.DaysInMonth(hoy.Year, hoy.Month));
            return Periodo.Personalizado(inicio, fin);
        }
        private static EstablecimientoId Estab() => EstablecimientoId.New();
        private static UsuarioId Vendedor() => UsuarioId.New();
        // ===================================================================================

        private static IndicadorNegocio AgregadoConUnaVenta(
            IndicadorNegocio.TipoIndicador tipo,
            Periodo periodo,
            SegmentoIndicador segmento,
            Guid comprobanteId,
            DateOnly fecha)
        {
            var agg = IndicadorNegocio.Crear(tipo, periodo, segmento);

            var item = new IndicadorNegocio.ComprobanteVenta.Item("P001", 1m, Money(100m));
            var venta = new IndicadorNegocio.ComprobanteVenta(
                comprobanteId: comprobanteId,
                fecha: fecha,
                clienteId: null,
                total: Money(118m),
                igv: Money(18m),
                items: new[] { item },
                vendedorId: Vendedor(),
                tipoComprobante: "Factura",
                establecimientoId: Estab()
            );
            agg.RegistrarVentaAceptada(venta); // Version pasa a 1
            return agg;
        }

        [Test]
        public async Task Anular_CuandoExisteVenta_DeberiaActualizar_PublicarEvento_YReflectarTotales()
        {
            // Arrange
            var repo = new Mock<IIndicadorNegocioRepository>(MockBehavior.Strict);
            var bus  = new Mock<IEventBus>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            var tipo = IndicadorNegocio.TipoIndicador.VentaDiaria;
            var periodo = PeriodoMesActual();
            var segmento = SegmentoSoles();
            var empresa = EmpresaId.From(Guid.NewGuid().ToString());

            var compId = Guid.NewGuid();
            var fecha  = new DateOnly(periodo.Inicio.Year, periodo.Inicio.Month, Math.Min(10, periodo.FinInclusive.Day));
            var agregado = AgregadoConUnaVenta(tipo, periodo, segmento, compId, fecha); // Versión=1, 1 comprobante

            tenant.SetupGet(t => t.EmpresaId).Returns(empresa);
            var uc   = new RegistrarAnulacionVentaUseCase(repo.Object, bus.Object, tenant.Object);

            repo.Setup(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agregado);

            repo.Setup(r => r.UpdateAsync(agregado, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var publicados = new List<IDomainEvent>();
            bus.Setup(b => b.PublishAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
               .Callback<IDomainEvent, CancellationToken>((e, _) => publicados.Add(e))
               .Returns(Task.CompletedTask);

            var input = new RegistrarAnulacionVentaInputDto(tipo, periodo, segmento, compId);

            // Act
            var output = await uc.ExecuteAsync(input, CancellationToken.None);

            // Assert
            Assert.That(output, Is.Not.Null);
            Assert.That(output.FueIdempotente, Is.False);
            Assert.That(output.Version, Is.EqualTo(2));              // 1 (venta) + 1 (anulación)
            Assert.That(output.TotalComprobantes, Is.EqualTo(0));     // se revirtió
            Assert.That(output.TotalVentas.Monto, Is.EqualTo(0m));    // quedó en cero
            Assert.That(output.Estado, Is.EqualTo(EstadoIndicador.Actualizado)); // estado coherente

            repo.Verify(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()), Times.Once);
            repo.Verify(r => r.UpdateAsync(agregado, It.IsAny<CancellationToken>()), Times.Once);

            // Publica al menos 1 evento: AnulacionRegistrada (puede o no haber evento de estado)
            Assert.That(publicados.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(publicados[0].GetType().Name, Is.EqualTo("AnulacionRegistrada"));
        }

        [Test]
        public async Task Anular_DosVecesMismoComprobante_DeberiaSerIdempotente_LaSegundaNoPersisteNiPublica()
        {
            // Arrange
            var repo = new Mock<IIndicadorNegocioRepository>(MockBehavior.Strict);
            var bus  = new Mock<IEventBus>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            var tipo = IndicadorNegocio.TipoIndicador.VentaDiaria;
            var periodo = PeriodoMesActual();
            var segmento = SegmentoSoles();
            var empresa = EmpresaId.From(Guid.NewGuid().ToString());

            var compId = Guid.NewGuid();
            var fecha  = new DateOnly(periodo.Inicio.Year, periodo.Inicio.Month, Math.Min(10, periodo.FinInclusive.Day));
            var agregado = AgregadoConUnaVenta(tipo, periodo, segmento, compId, fecha); // Versión=1

            tenant.SetupGet(t => t.EmpresaId).Returns(empresa);
            var uc   = new RegistrarAnulacionVentaUseCase(repo.Object, bus.Object, tenant.Object);

            repo.SetupSequence(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agregado)  // 1ra anulación
                .ReturnsAsync(agregado); // 2da anulación

            repo.Setup(r => r.UpdateAsync(agregado, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var publicados = new List<IDomainEvent>();
            bus.Setup(b => b.PublishAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
               .Callback<IDomainEvent, CancellationToken>((e, _) => publicados.Add(e))
               .Returns(Task.CompletedTask);

            var input = new RegistrarAnulacionVentaInputDto(tipo, periodo, segmento, compId);

            // Act 1
            var out1 = await uc.ExecuteAsync(input, CancellationToken.None);
            // Act 2 (idempotente)
            var out2 = await uc.ExecuteAsync(input, CancellationToken.None);

            // Assert
            Assert.That(out1.FueIdempotente, Is.False);
            Assert.That(out2.FueIdempotente, Is.True);

            // Solo un Update (primera vez)
            repo.Verify(r => r.UpdateAsync(agregado, It.IsAny<CancellationToken>()), Times.Once);
            // Solo eventos de la primera vez (>=1, normalmente 1)
            Assert.That(publicados.Count, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void Anular_CuandoNoExisteIndicador_DeberiaLanzarNotFound()
        {
            // Arrange
            var repo = new Mock<IIndicadorNegocioRepository>(MockBehavior.Strict);
            var bus  = new Mock<IEventBus>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            var tipo = IndicadorNegocio.TipoIndicador.VentaDiaria;
            var periodo = PeriodoMesActual();
            var segmento = SegmentoSoles();
            var empresa = EmpresaId.From(Guid.NewGuid().ToString());

            tenant.SetupGet(t => t.EmpresaId).Returns(empresa);
            var uc   = new RegistrarAnulacionVentaUseCase(repo.Object, bus.Object, tenant.Object);

            repo.Setup(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()))
                .ReturnsAsync((IndicadorNegocio?)null);

            var input = new RegistrarAnulacionVentaInputDto(tipo, periodo, segmento, Guid.NewGuid());

            // Act + Assert
            Assert.That(async () => await uc.ExecuteAsync(input, CancellationToken.None),
                Throws.Exception.TypeOf<NotFoundException>());

            bus.Verify(b => b.PublishAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()), Times.Never);
            repo.Verify(r => r.UpdateAsync(It.IsAny<IndicadorNegocio>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Anular_ConEmpresaId_UsaOverloadDeRepositorioConTenant()
        {
            // Arrange
            var repo = new Mock<IIndicadorNegocioRepository>(MockBehavior.Strict);
            var bus  = new Mock<IEventBus>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            var tipo = IndicadorNegocio.TipoIndicador.VentaDiaria;
            var periodo = PeriodoMesActual();
            var segmento = SegmentoSoles();
            var empresa = EmpresaId.From(Guid.NewGuid().ToString());

            tenant.SetupGet(t => t.EmpresaId).Returns(empresa);
            var uc   = new RegistrarAnulacionVentaUseCase(repo.Object, bus.Object, tenant.Object);

            var compId = Guid.NewGuid();
            var fecha  = new DateOnly(periodo.Inicio.Year, periodo.Inicio.Month, Math.Min(10, periodo.FinInclusive.Day));
            var agregado = AgregadoConUnaVenta(tipo, periodo, segmento, compId, fecha);

            repo.Setup(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agregado);

            repo.Setup(r => r.UpdateAsync(agregado, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            bus.Setup(b => b.PublishAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

            var input = new RegistrarAnulacionVentaInputDto(tipo, periodo, segmento, compId, empresa);

            // Act
            var output = await uc.ExecuteAsync(input, CancellationToken.None);

            // Assert
            Assert.That(output.FueIdempotente, Is.False);
            repo.Verify(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()), Times.Once);
            repo.Verify(r => r.GetByClaveAsync(tipo, periodo, segmento, It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Anular_CuandoVentaNoExisteEnElIndicador_DeberiaSerIdempotente_SinPersistirNiPublicar()
        {
            // Arrange
            var repo = new Mock<IIndicadorNegocioRepository>(MockBehavior.Strict);
            var bus  = new Mock<IEventBus>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            var tipo = IndicadorNegocio.TipoIndicador.VentaDiaria;
            var periodo = PeriodoMesActual();
            var segmento = SegmentoSoles();
            var empresa = EmpresaId.From(Guid.NewGuid().ToString());

            // Indicador existente pero sin ventas
            var agregado = IndicadorNegocio.Crear(tipo, periodo, segmento);
            tenant.SetupGet(t => t.EmpresaId).Returns(empresa);
            var uc   = new RegistrarAnulacionVentaUseCase(repo.Object, bus.Object, tenant.Object);
            repo.Setup(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agregado);

            var publicados = new List<IDomainEvent>();
            bus.Setup(b => b.PublishAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
               .Callback<IDomainEvent, CancellationToken>((e, _) => publicados.Add(e))
               .Returns(Task.CompletedTask);

            var input = new RegistrarAnulacionVentaInputDto(tipo, periodo, segmento, Guid.NewGuid());

            // Act
            var output = await uc.ExecuteAsync(input, CancellationToken.None);

            // Assert
            Assert.That(output.FueIdempotente, Is.True);
            repo.Verify(r => r.UpdateAsync(It.IsAny<IndicadorNegocio>(), It.IsAny<CancellationToken>()), Times.Never);
            Assert.That(publicados.Count, Is.EqualTo(0));
        }
    }
}
