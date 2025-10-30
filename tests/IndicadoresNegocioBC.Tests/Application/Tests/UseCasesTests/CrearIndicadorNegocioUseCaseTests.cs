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
using SharedKernel.Application.Interfaces;

namespace IndicadoresNegocioBC.Tests.Application.UseCases
{
    [TestFixture]
    public class CrearIndicadorNegocioUseCaseTests
    {
        // ======== Builders simples (AJUSTA a tus factories reales de VOs si difieren) ========

    private static Moneda PEN() => Moneda.PEN();
    private static SegmentoIndicador SegmentoSoles() => SegmentoIndicador.ParaEmpresa(EmpresaId.From("11111111-1111-1111-1111-111111111111"), PEN());
    private static Periodo PeriodoMesActual() => Periodo.PorMes(DateTime.UtcNow.Year, DateTime.UtcNow.Month);
    private static EmpresaId Empresa() => EmpresaId.From("empresa-demo");

        // ======================================================================================

        [Test]
        public async Task Crear_CuandoNoExisteConEmpresa_DeberiaCrear_Persistir_PublicarEvento_YDevolverDto()
        {
            // Arrange
            var repo = new Mock<IIndicadorNegocioRepository>(MockBehavior.Strict);
            var bus = new Mock<IEventBus>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            var tipo = IndicadorNegocio.TipoIndicador.VentaDiaria;
            var periodo = PeriodoMesActual();
            var segmento = SegmentoSoles();
            var empresa = Empresa();
            var ahora = new DateTimeOffset(2025, 9, 8, 10, 0, 0, TimeSpan.Zero);

            tenant.SetupGet(t => t.EmpresaId).Returns(empresa);
            var useCase = new CrearIndicadorNegocioUseCase(repo.Object, bus.Object, tenant.Object);

            repo.Setup(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()))
                .ReturnsAsync((IndicadorNegocio?)null);

            repo.Setup(r => r.AddAsync(It.IsAny<IndicadorNegocio>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

                IndicadoresNegocioBC.Domain.Events.IndicadorNegocioEvents.IndicadorNegocioCreado? capturado = null;
                bus.Setup(b => b.PublishAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
                    .Callback<IDomainEvent, CancellationToken>((e, _) => capturado = (IndicadoresNegocioBC.Domain.Events.IndicadorNegocioEvents.IndicadorNegocioCreado)e)
                    .Returns(Task.CompletedTask);

            var input = new CrearIndicadorNegocioInputDto(tipo, periodo, segmento, empresa, ahora);

            // Act
            var output = await useCase.ExecuteAsync(input, CancellationToken.None);

            // Assert
            Assert.That(output, Is.Not.Null);
            Assert.That(output.IndicadorId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(output.Tipo, Is.EqualTo(tipo));
            Assert.That(output.Periodo, Is.EqualTo(periodo));
            Assert.That(output.Segmento, Is.EqualTo(segmento));
            Assert.That(output.Estado, Is.EqualTo(EstadoIndicador.Creado));
            Assert.That(output.CreadoEn, Is.EqualTo(ahora));
            Assert.That(output.Version, Is.EqualTo(0));

            repo.Verify(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()), Times.Once);
            repo.Verify(r => r.AddAsync(It.IsAny<IndicadorNegocio>(), It.IsAny<CancellationToken>()), Times.Once);
            bus.Verify(b => b.PublishAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()), Times.Once);

            Assert.That(capturado, Is.Not.Null);
            Assert.That(capturado!.IndicadorId, Is.EqualTo(output.IndicadorId));
            Assert.That(capturado.Tipo, Is.EqualTo(tipo));
            Assert.That(capturado.Periodo, Is.EqualTo(periodo));
            Assert.That(capturado.Segmento, Is.EqualTo(segmento));
            Assert.That(capturado.CreadoEn, Is.EqualTo(ahora));
            Assert.That(capturado.Version, Is.EqualTo(0));
        }

        [Test]
        public async Task Crear_CuandoNoExisteSinEmpresaEnInput_DeberiaUsarTenant()
        {
            // Arrange
            var repo = new Mock<IIndicadorNegocioRepository>(MockBehavior.Strict);
            var bus = new Mock<IEventBus>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            var tipo = IndicadorNegocio.TipoIndicador.RankingProductos;
            var periodo = PeriodoMesActual();
            var segmento = SegmentoSoles();
            var empresa = Empresa();

            tenant.SetupGet(t => t.EmpresaId).Returns(empresa);
            var useCase = new CrearIndicadorNegocioUseCase(repo.Object, bus.Object, tenant.Object);

            repo.Setup(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()))
                .ReturnsAsync((IndicadorNegocio?)null);

            repo.Setup(r => r.AddAsync(It.IsAny<IndicadorNegocio>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            bus.Setup(b => b.PublishAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

            var input = new CrearIndicadorNegocioInputDto(tipo, periodo, segmento, empresaId: null, ahoraUtc: null);

            // Act
            var output = await useCase.ExecuteAsync(input, CancellationToken.None);

            // Assert
            Assert.That(output.Tipo, Is.EqualTo(tipo));
            repo.Verify(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()), Times.Once);
            // Verifica que NO se usó el overload sin EmpresaId
            repo.Verify(r => r.GetByClaveAsync(tipo, periodo, segmento, It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public void Crear_CuandoYaExiste_DeberiaLanzarBusinessRuleException_YNoPersistirNiPublicar()
        {
            // Arrange
            var repo = new Mock<IIndicadorNegocioRepository>(MockBehavior.Strict);
            var bus = new Mock<IEventBus>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            var tipo = IndicadorNegocio.TipoIndicador.TicketPromedio;
            var periodo = PeriodoMesActual();
            var segmento = SegmentoSoles();
            var empresa = Empresa();
            tenant.SetupGet(t => t.EmpresaId).Returns(empresa);
            var useCase = new CrearIndicadorNegocioUseCase(repo.Object, bus.Object, tenant.Object);

            var ya = IndicadoresNegocioBC.Domain.Aggregates.IndicadorNegocio.Crear(tipo, periodo, segmento);

            repo.Setup(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ya);

            var input = new CrearIndicadorNegocioInputDto(tipo, periodo, segmento, empresa);

            // Act + Assert
            Assert.That(
                async () => await useCase.ExecuteAsync(input, CancellationToken.None),
                Throws.Exception.TypeOf<BusinessRuleException>()
                     .With.Message.Contains("Ya existe un IndicadorNegocio"));

            repo.Verify(r => r.AddAsync(It.IsAny<IndicadorNegocio>(), It.IsAny<CancellationToken>()), Times.Never);
            bus.Verify(b => b.PublishAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Crear_RespetaAhoraUtc_Determinista()
        {
            // Arrange
            var repo = new Mock<IIndicadorNegocioRepository>(MockBehavior.Strict);
            var bus = new Mock<IEventBus>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            var tipo = IndicadorNegocio.TipoIndicador.RankingClientes;
            var periodo = PeriodoMesActual();
            var segmento = SegmentoSoles();
            var ahora = new DateTimeOffset(2024, 12, 31, 23, 59, 0, TimeSpan.Zero);

            var empresa = Empresa();
            tenant.SetupGet(t => t.EmpresaId).Returns(empresa);
            var useCase = new CrearIndicadorNegocioUseCase(repo.Object, bus.Object, tenant.Object);

            repo.Setup(r => r.GetByClaveAsync(tipo, periodo, segmento, empresa, It.IsAny<CancellationToken>()))
                .ReturnsAsync((IndicadorNegocio?)null);

            repo.Setup(r => r.AddAsync(It.IsAny<IndicadorNegocio>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            bus.Setup(b => b.PublishAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

            var input = new CrearIndicadorNegocioInputDto(tipo, periodo, segmento, empresaId: null, ahoraUtc: ahora);

            // Act
            var output = await useCase.ExecuteAsync(input, CancellationToken.None);

            // Assert
            Assert.That(output.CreadoEn, Is.EqualTo(ahora));
        }
    }
}
