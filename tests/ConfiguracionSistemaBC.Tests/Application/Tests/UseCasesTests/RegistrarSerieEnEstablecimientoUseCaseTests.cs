using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using ConfiguracionSistemaBC.Application.UseCases;
using ConfiguracionSistemaBC.Domain.Aggregates;
using ConfiguracionSistemaBC.Domain.Repositories;
using ConfiguracionSistemaBC.Domain.ValueObjects;
using NUnit.Framework;
using Moq;
using SharedKernel.Application.Interfaces;
using SharedKernel.ValueObjects;

namespace ConfiguracionSistemaBC.Tests.UseCases
{
    [TestFixture]
    public class RegistrarSerieEnEstablecimientoUseCaseTests
    {
    private Mock<ISerieComprobanteRepository> _seriesRepo = null!;
    private Mock<IConfiguracionEmpresaRepository> _configRepo = null!;
    private Mock<IUnitOfWork> _uow = null!;
    private Mock<ITenantContext> _tenant = null!;

    private EmpresaId _empresaId = null!;
    private EstablecimientoId _estId = null!;

        [SetUp]
        public void SetUp()
        {
            _seriesRepo = new Mock<ISerieComprobanteRepository>(MockBehavior.Strict);
            _configRepo = new Mock<IConfiguracionEmpresaRepository>(MockBehavior.Strict);
            _uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            _tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            // Empresa “opaca” desde RUC 20600893409
            _empresaId = EmpresaId.From("20600893409");
            _tenant.SetupGet(t => t.EmpresaId).Returns(_empresaId);

            _estId = EstablecimientoId.New();
        }

        private RegistrarSerieEnEstablecimientoUseCase CreateSut()
            => new(_seriesRepo.Object, _configRepo.Object, _uow.Object, _tenant.Object);

        [Test]
        public async Task Registra_Serie_OK_y_persiste()
        {
            // Arrange
            var input = new RegistrarSerieEnEstablecimientoInputDto
            {
                EstablecimientoId = _estId.ToString(),
                TipoComprobante = "01",
                Serie = "FE01",
                TipoOperacion = "0101",
                CorrelativoInicial = "1",
                EsPorDefecto = false,
                Habilitada = true
            };

            _configRepo.Setup(r => r.EstablecimientoExisteAsync(_empresaId, _estId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _seriesRepo.Setup(r => r.ExistsByTipoSerieAsync(_empresaId, TipoComprobanteCodigo.Factura, SerieCodigo.From("FE01"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _seriesRepo.Setup(r => r.AddAsync(It.IsAny<SerieComprobante>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _uow.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var sut = CreateSut();

            // Act
            var outDto = await sut.HandleAsync(input);

            // Assert
            Assert.That(outDto.EmpresaId, Is.EqualTo(_empresaId.Value));
            Assert.That(outDto.EstablecimientoId, Is.EqualTo(_estId.Value));
            Assert.That(outDto.TipoComprobante, Is.EqualTo("01"));
            Assert.That(outDto.Serie, Is.EqualTo("FE01"));
            Assert.That(outDto.SiguienteCorrelativo, Is.EqualTo(1));
            Assert.That(outDto.Habilitada, Is.True);
            Assert.That(outDto.EsPorDefecto, Is.False);

            _seriesRepo.Verify(r => r.AddAsync(It.Is<SerieComprobante>(s =>
                s.EmpresaId == _empresaId &&
                s.Tipo == TipoComprobanteCodigo.Factura &&
                s.Serie == SerieCodigo.From("FE01") &&
                s.EstablecimientoId == _estId &&
                s.Siguiente == Correlativo.From(1)
            ), It.IsAny<CancellationToken>()), Times.Once);

            _uow.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void Falla_si_Establecimiento_no_existe()
        {
            // Arrange
            var input = new RegistrarSerieEnEstablecimientoInputDto
            {
                EstablecimientoId = _estId.ToString(),
                TipoComprobante = "03",
                Serie = "BE01",
                CorrelativoInicial = "1"
            };

            _configRepo.Setup(r => r.EstablecimientoExisteAsync(_empresaId, _estId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var sut = CreateSut();

            // Act + Assert
            Assert.That(async () => await sut.HandleAsync(input),
                Throws.TypeOf<KeyNotFoundException>());
        }

        [Test]
        public void Falla_por_unicidad_si_ya_existe_la_serie()
        {
            // Arrange
            var input = new RegistrarSerieEnEstablecimientoInputDto
            {
                EstablecimientoId = _estId.ToString(),
                TipoComprobante = "01",
                Serie = "FE01",
                CorrelativoInicial = "1"
            };

            _configRepo.Setup(r => r.EstablecimientoExisteAsync(_empresaId, _estId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _seriesRepo.Setup(r => r.ExistsByTipoSerieAsync(_empresaId, TipoComprobanteCodigo.Factura, SerieCodigo.From("FE01"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var sut = CreateSut();

            // Act + Assert
            Assert.That(async () => await sut.HandleAsync(input),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("Ya existe"));
        }

        [Test]
        public void Falla_si_piden_por_defecto_pero_inhabilitada()
        {
            // Arrange
            var input = new RegistrarSerieEnEstablecimientoInputDto
            {
                EstablecimientoId = _estId.ToString(),
                TipoComprobante = "03",
                Serie = "BE01",
                CorrelativoInicial = "1",
                EsPorDefecto = true,
                Habilitada = false
            };

            _configRepo.Setup(r => r.EstablecimientoExisteAsync(_empresaId, _estId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var sut = CreateSut();

            // Act + Assert
            Assert.That(async () => await sut.HandleAsync(input),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("por defecto"));
        }

        [Test]
        public void Falla_si_prefijo_de_serie_no_coincide_con_tipo()
        {
            // BE01 con tipo Factura (01) debe fallar
            var input = new RegistrarSerieEnEstablecimientoInputDto
            {
                EstablecimientoId = _estId.ToString(),
                TipoComprobante = "01",
                Serie = "BE01",
                CorrelativoInicial = "1"
            };

            _configRepo.Setup(r => r.EstablecimientoExisteAsync(_empresaId, _estId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            var sut = CreateSut();

            Assert.That(async () => await sut.HandleAsync(input),
                Throws.Exception); // la excepción específica proviene del VO/aggregate (ArgumentException)
        }

        [Test]
        public async Task Desmarca_otras_por_defecto_si_esPorDefecto_true()
        {
            var input = new RegistrarSerieEnEstablecimientoInputDto
            {
                EstablecimientoId = _estId.ToString(),
                TipoComprobante = "03",
                Serie = "BE02",
                CorrelativoInicial = "10",
                EsPorDefecto = true
            };

            _configRepo.Setup(r => r.EstablecimientoExisteAsync(_empresaId, _estId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _seriesRepo.Setup(r => r.ExistsByTipoSerieAsync(_empresaId, TipoComprobanteCodigo.Boleta, SerieCodigo.From("BE02"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _seriesRepo.Setup(r => r.UnsetDefaultForTipoAsync(It.IsAny<EmpresaId>(), It.IsAny<TipoComprobanteCodigo>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _seriesRepo.Setup(r => r.AddAsync(It.IsAny<SerieComprobante>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _uow.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var sut = CreateSut();
            await sut.HandleAsync(input);

            _seriesRepo.Verify(r => r.UnsetDefaultForTipoAsync(_empresaId, TipoComprobanteCodigo.Boleta, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
