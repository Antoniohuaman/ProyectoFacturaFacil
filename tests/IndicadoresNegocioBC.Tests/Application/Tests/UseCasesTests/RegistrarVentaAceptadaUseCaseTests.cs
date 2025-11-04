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
using SharedKernel.ValueObjects;
using SharedKernel.Application.Interfaces;

namespace IndicadoresNegocioBC.Tests.Application.UseCases
{
    [TestFixture]
    public class RegistrarVentaAceptadaUseCaseTests
    {
        // ====== Builders mínimos (ajusta a tus factories reales de VOs si difieren) ======
        private static Moneda PEN() => Moneda.Create("PEN");
        private static Dinero Money(decimal m) => new Dinero(m, PEN());
        private static SegmentoIndicador SegmentoSoles(Guid? empresaId = null)
            => SegmentoIndicador.ParaEmpresa(EmpresaId.From((empresaId ?? Guid.NewGuid()).ToString()), PEN());
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

        private static RegistrarVentaAceptadaInputDto InputBasico(
            IndicadorNegocio.TipoIndicador tipo,
            Periodo periodo,
            SegmentoIndicador segmento,
            string tipoComprobante = "Factura",
            Guid? comprobanteId = null,
            DateOnly? fecha = null,
            decimal subtotal = 100m,
            decimal igvMonto = 18m,
            Guid? clienteId = null,
            bool conVendedor = true,
            EmpresaId? empresaId = null)
        {
            var id = comprobanteId ?? Guid.NewGuid();
            var f = fecha ?? new DateOnly(periodo.Inicio.Year, periodo.Inicio.Month, Math.Min(15, periodo.FinInclusive.Day));
            var sub = Money(subtotal);
            var igv = Money(igvMonto);
            var total = Money(sub.Monto + igv.Monto);

            var items = new[]
            {
                new RegistrarVentaAceptadaInputDto.ItemInput(Guid.Parse("11111111-1111-1111-1111-111111111111"), 1m, sub)
            };

            return new RegistrarVentaAceptadaInputDto(
                tipo: tipo,
                periodo: periodo,
                segmento: segmento,
                tipoComprobante: tipoComprobante,
                comprobanteId: id,
                fecha: f,
                total: total,
                igv: igv,
                items: items,
                establecimientoId: Estab(),
                clienteId: clienteId,
                vendedorId: conVendedor ? Vendedor() : null,
                empresaId: empresaId
            );
        }

        [Test]
        public async Task RegistrarVenta_CuandoExisteYNoHayVentaPrevia_DeberiaActualizar_Publicar2Eventos_YDevolverTotales()
        {
            // Arrange
            var repo = new Mock<IIndicadorNegocioRepository>(MockBehavior.Strict);
            var bus  = new Mock<IEventBus>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            var tipo = IndicadorNegocio.TipoIndicador.VentaDiaria;
            var periodo = PeriodoMesActual();
            var segmento = SegmentoSoles();
            var empresa = EmpresaId.From(Guid.NewGuid().ToString());

            var agregado = IndicadorNegocio.Crear(tipo, periodo, segmento); // Estado = Creado, Version = 0

            tenant.SetupGet(t => t.EmpresaId).Returns(empresa);
            var uc   = new RegistrarVentaAceptadaUseCase(repo.Object, bus.Object, tenant.Object);

            repo.Setup(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agregado);

            repo.Setup(r => r.UpdateAsync(agregado, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var publicados = new List<IDomainEvent>();
            bus.Setup(b => b.PublishAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
               .Callback<IDomainEvent, CancellationToken>((e, _) => publicados.Add(e))
               .Returns(Task.CompletedTask);

            var input = InputBasico(tipo, periodo, segmento);

            // Act
            var output = await uc.ExecuteAsync(input, CancellationToken.None);

            // Assert
            Assert.That(output, Is.Not.Null);
            Assert.That(output.FueIdempotente, Is.False);
            Assert.That(output.Estado, Is.EqualTo(EstadoIndicador.Actualizado));
            Assert.That(output.TotalComprobantes, Is.EqualTo(1));
            Assert.That(output.TotalVentas.Moneda, Is.EqualTo(segmento.Moneda));
            Assert.That(output.TotalVentas.Monto, Is.GreaterThan(0m));
            Assert.That(output.Version, Is.EqualTo(1));

            repo.Verify(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()), Times.Once);
            repo.Verify(r => r.UpdateAsync(agregado, It.IsAny<CancellationToken>()), Times.Once);
            repo.Verify(r => r.AddAsync(It.IsAny<IndicadorNegocio>(), It.IsAny<CancellationToken>()), Times.Never);

            // Se publican 2 eventos: VentaAceptadaRegistrada + IndicadorNegocioActualizado (CREADO->ACTUALIZADO)
            Assert.That(publicados.Count, Is.EqualTo(2));
            Assert.That(publicados[0], Is.TypeOf<IndicadoresNegocioBC.Domain.Events.IndicadorNegocioEvents.VentaAceptadaRegistrada>());
            Assert.That(publicados[1], Is.TypeOf<IndicadoresNegocioBC.Domain.Events.IndicadorNegocioEvents.IndicadorNegocioActualizado>());
        }

        [Test]
        public async Task RegistrarVenta_CuandoNoExiste_DeberiaCrear_Add_Publicar2Eventos()
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
            var uc   = new RegistrarVentaAceptadaUseCase(repo.Object, bus.Object, tenant.Object);

            repo.Setup(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()))
                .ReturnsAsync((IndicadorNegocio?)null);

            repo.Setup(r => r.AddAsync(It.IsAny<IndicadorNegocio>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var publicados = new List<IDomainEvent>();
            bus.Setup(b => b.PublishAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
               .Callback<IDomainEvent, CancellationToken>((e, _) => publicados.Add(e))
               .Returns(Task.CompletedTask);

            var input = InputBasico(tipo, periodo, segmento);

            // Act
            var output = await uc.ExecuteAsync(input, CancellationToken.None);

            // Assert
            Assert.That(output.Estado, Is.EqualTo(EstadoIndicador.Actualizado));
            Assert.That(output.TotalComprobantes, Is.EqualTo(1));
            Assert.That(output.Version, Is.EqualTo(1));

            repo.Verify(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()), Times.Once);
            repo.Verify(r => r.AddAsync(It.IsAny<IndicadorNegocio>(), It.IsAny<CancellationToken>()), Times.Once);
            repo.Verify(r => r.UpdateAsync(It.IsAny<IndicadorNegocio>(), It.IsAny<CancellationToken>()), Times.Never);

            Assert.That(publicados.Count, Is.EqualTo(2));
            Assert.That(publicados[0], Is.TypeOf<IndicadoresNegocioBC.Domain.Events.IndicadorNegocioEvents.VentaAceptadaRegistrada>());
            Assert.That(publicados[1], Is.TypeOf<IndicadoresNegocioBC.Domain.Events.IndicadorNegocioEvents.IndicadorNegocioActualizado>());
        }

        [Test]
        public async Task RegistrarVenta_DosVecesMismoComprobante_DeberiaSerIdempotente_SegundaNoPersisteNiPublica()
        {
            // Arrange
            var repo = new Mock<IIndicadorNegocioRepository>(MockBehavior.Strict);
            var bus  = new Mock<IEventBus>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            var tipo = IndicadorNegocio.TipoIndicador.VentaDiaria;
            var periodo = PeriodoMesActual();
            var segmento = SegmentoSoles();
            var empresa = EmpresaId.From(Guid.NewGuid().ToString());

            var agregado = IndicadorNegocio.Crear(tipo, periodo, segmento);
            tenant.SetupGet(t => t.EmpresaId).Returns(empresa);
            var uc   = new RegistrarVentaAceptadaUseCase(repo.Object, bus.Object, tenant.Object);

            repo.SetupSequence(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agregado)   // primera llamada
                .ReturnsAsync(agregado);  // segunda llamada

            repo.Setup(r => r.UpdateAsync(agregado, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var publicados = new List<IDomainEvent>();
            bus.Setup(b => b.PublishAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
               .Callback<IDomainEvent, CancellationToken>((e, _) => publicados.Add(e))
               .Returns(Task.CompletedTask);

            var compraId = Guid.NewGuid();
            var input = InputBasico(tipo, periodo, segmento, comprobanteId: compraId);

            // Act 1
            var out1 = await uc.ExecuteAsync(input, CancellationToken.None);
            // Act 2 (mismo comprobante)
            var out2 = await uc.ExecuteAsync(input, CancellationToken.None);

            // Assert 1 (primera vez)
            Assert.That(out1.FueIdempotente, Is.False);
            // Assert 2 (segunda vez idempotente)
            Assert.That(out2.FueIdempotente, Is.True);

            // Persistencia: solo un Update (primera vez)
            repo.Verify(r => r.UpdateAsync(agregado, It.IsAny<CancellationToken>()), Times.Once);

            // Eventos: solo los de la primera (2 eventos)
            Assert.That(publicados.Count, Is.EqualTo(2));
            Assert.That(publicados[0], Is.TypeOf<IndicadoresNegocioBC.Domain.Events.IndicadorNegocioEvents.VentaAceptadaRegistrada>());
            Assert.That(publicados[1], Is.TypeOf<IndicadoresNegocioBC.Domain.Events.IndicadorNegocioEvents.IndicadorNegocioActualizado>());
        }

        [Test]
        public void RegistrarVenta_MonedaDistinta_DeberiaFallarSinPersistirNiPublicar()
        {
            // Arrange
            var repo = new Mock<IIndicadorNegocioRepository>(MockBehavior.Strict);
            var bus  = new Mock<IEventBus>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            var tipo = IndicadorNegocio.TipoIndicador.VentaDiaria;
            var periodo = PeriodoMesActual();
            var segmento = SegmentoSoles();
            var empresa = EmpresaId.From(Guid.NewGuid().ToString());

            var agregado = IndicadorNegocio.Crear(tipo, periodo, segmento);
            tenant.SetupGet(t => t.EmpresaId).Returns(empresa);
            var uc   = new RegistrarVentaAceptadaUseCase(repo.Object, bus.Object, tenant.Object);

            repo.Setup(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agregado);

            var usd = Moneda.Create("USD");
            var input = new RegistrarVentaAceptadaInputDto(
                tipo, periodo, segmento,
                tipoComprobante: "Factura",
                comprobanteId: Guid.NewGuid(),
                fecha: new DateOnly(periodo.Inicio.Year, periodo.Inicio.Month, periodo.Inicio.Day),
                total: new Dinero(118m, usd),   // moneda distinta
                igv:   new Dinero(18m, usd),
                items: new[] { new RegistrarVentaAceptadaInputDto.ItemInput(Guid.NewGuid(), 1m, new Dinero(100m, usd)) },
                establecimientoId: Estab()
            );

            // Act + Assert
            Assert.That(async () => await uc.ExecuteAsync(input, CancellationToken.None),
                Throws.InvalidOperationException);

            repo.Verify(r => r.UpdateAsync(It.IsAny<IndicadorNegocio>(), It.IsAny<CancellationToken>()), Times.Never);
            repo.Verify(r => r.AddAsync(It.IsAny<IndicadorNegocio>(), It.IsAny<CancellationToken>()), Times.Never);
            bus.Verify(b => b.PublishAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public void RegistrarVenta_FechaFueraDePeriodo_DeberiaFallar()
        {
            // Arrange
            var repo = new Mock<IIndicadorNegocioRepository>(MockBehavior.Strict);
            var bus  = new Mock<IEventBus>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            var tipo = IndicadorNegocio.TipoIndicador.VentaDiaria;
            var periodo = PeriodoMesActual();
            var segmento = SegmentoSoles();
            var empresa = EmpresaId.From(Guid.NewGuid().ToString());

            var agregado = IndicadorNegocio.Crear(tipo, periodo, segmento);
            tenant.SetupGet(t => t.EmpresaId).Returns(empresa);
            var uc   = new RegistrarVentaAceptadaUseCase(repo.Object, bus.Object, tenant.Object);

            repo.Setup(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agregado);

            var fechaFuera = periodo.FinInclusive.AddDays(1); // fuera del periodo
            var input = InputBasico(tipo, periodo, segmento, fecha: fechaFuera);

            // Act + Assert
            Assert.That(async () => await uc.ExecuteAsync(input, CancellationToken.None),
                Throws.InvalidOperationException);

            repo.Verify(r => r.UpdateAsync(It.IsAny<IndicadorNegocio>(), It.IsAny<CancellationToken>()), Times.Never);
            bus.Verify(b => b.PublishAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task RegistrarVenta_ConEmpresaId_UsaOverloadDeRepositorioConTenant()
        {
            // Arrange
            var repo = new Mock<IIndicadorNegocioRepository>(MockBehavior.Strict);
            var bus  = new Mock<IEventBus>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            var tipo = IndicadorNegocio.TipoIndicador.VentaDiaria;
            var periodo = PeriodoMesActual();
            var segmento = SegmentoSoles();
            var empresa = EmpresaId.From("empresa-demo");

            tenant.SetupGet(t => t.EmpresaId).Returns(empresa);
            var uc   = new RegistrarVentaAceptadaUseCase(repo.Object, bus.Object, tenant.Object);

            var agregado = IndicadorNegocio.Crear(tipo, periodo, segmento);
            repo.Setup(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agregado);

            repo.Setup(r => r.UpdateAsync(agregado, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            bus.Setup(b => b.PublishAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

            var input = InputBasico(tipo, periodo, segmento, empresaId: empresa);

            // Act
            var output = await uc.ExecuteAsync(input, CancellationToken.None);

            // Assert
            Assert.That(output.Estado, Is.EqualTo(EstadoIndicador.Actualizado));
            repo.Verify(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()), Times.Once);
            // No usa el overload sin empresa
            repo.Verify(r => r.GetByClaveAsync(tipo, periodo, segmento, It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
