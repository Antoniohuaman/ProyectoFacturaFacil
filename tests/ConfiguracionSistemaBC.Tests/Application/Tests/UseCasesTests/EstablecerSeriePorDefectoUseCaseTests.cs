using System;
using System.Threading;
using System.Threading.Tasks;
using ConfiguracionSistemaBC.Application.UseCases;
using ConfiguracionSistemaBC.Domain.Aggregates;
using ConfiguracionSistemaBC.Domain.Repositories;
using ConfiguracionSistemaBC.Application.Interfaces; // IUnitOfWork
using ConfiguracionSistemaBC.Domain.ValueObjects;
using Moq;
using NUnit.Framework;
using SharedKernel.Application.Interfaces;
using SharedKernel.ValueObjects;
using SharedKernel.Exceptions;

namespace ConfiguracionSistemaBC.Tests.UseCases
{
    [TestFixture]
    public class EstablecerSeriePorDefectoUseCaseTests
    {
    private Mock<ISerieComprobanteRepository> _repo = null!;
    private Mock<IUnitOfWork> _uow = null!;
    private Mock<ITenantContext> _tenant = null!;
        private EmpresaId _empresa = null!;
        private EstablecimientoId _est = null!;

        [SetUp]
        public void SetUp()
        {
            _repo = new Mock<ISerieComprobanteRepository>();
            _uow = new Mock<IUnitOfWork>();
            _tenant = new Mock<ITenantContext>();

            _empresa = EmpresaId.From("20600893409");
            _tenant.Setup(t => t.EmpresaId).Returns(_empresa);
            _est = EstablecimientoId.New();
        }

        private static SerieComprobante NuevaSerie(EmpresaId emp, EstablecimientoId est, string tipo, string serie, int corr = 1, bool porDefecto = false, bool habilitada = true)
            => SerieComprobante.Crear(
                emp,
                TipoComprobanteCodigo.From(tipo),
                SerieCodigo.From(serie),
                est,
                TipoOperacion.Default,
                Correlativo.From(corr),
                esPorDefecto: porDefecto,
                habilitada: habilitada);

        [Test]
        public async Task MarcaComoDefault_y_desmarca_anterior()
        {
            // Arrange
            var anterior = NuevaSerie(_empresa, _est, "01", "FA01", 10, porDefecto: true);
            var candidata = NuevaSerie(_empresa, _est, "01", "FE01", 1, porDefecto: false);

            _repo.Setup(r => r.GetByIdAsync(candidata.Id, It.IsAny<CancellationToken>())).ReturnsAsync(candidata);
            _repo.Setup(r => r.GetDefaultByTipoAsync(_empresa, candidata.Tipo, It.IsAny<CancellationToken>())).ReturnsAsync(anterior);

            var sut = new EstablecerSeriePorDefectoUseCase(_repo.Object, _uow.Object, _tenant.Object);

            var input = new EstablecerSeriePorDefectoInputDto
            {
                SerieComprobanteId = candidata.Id.ToString("D"),
                ExpectedVersion = candidata.Version
            };

            // Act
            var dto = await sut.HandleAsync(input);

            // Assert
            Assert.That(dto.EmpresaId, Is.EqualTo(_empresa.Value));
            Assert.That(dto.SerieComprobanteId, Is.EqualTo(candidata.Id));
            Assert.That(dto.YaEraPorDefecto, Is.False);
            Assert.That(dto.AnteriorSeriePorDefectoId, Is.EqualTo(anterior.Id));

            _repo.Verify(r => r.UnsetDefaultForTipoAsync(_empresa, candidata.Tipo, It.IsAny<CancellationToken>()), Times.Once);
            _repo.Verify(r => r.UpdateAsync(candidata, input.ExpectedVersion, It.IsAny<CancellationToken>()), Times.Once);
            _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Idempotente_si_ya_es_default_No_hace_nada()
        {
            // Arrange
            var yaDefault = NuevaSerie(_empresa, _est, "03", "BE01", 1, porDefecto: true);

            _repo.Setup(r => r.GetByIdAsync(yaDefault.Id, It.IsAny<CancellationToken>())).ReturnsAsync(yaDefault);
            _repo.Setup(r => r.GetDefaultByTipoAsync(_empresa, yaDefault.Tipo, It.IsAny<CancellationToken>())).ReturnsAsync(yaDefault); // ya es la default

            var sut = new EstablecerSeriePorDefectoUseCase(_repo.Object, _uow.Object, _tenant.Object);

            var input = new EstablecerSeriePorDefectoInputDto
            {
                SerieComprobanteId = yaDefault.Id.ToString("D"),
                ExpectedVersion = yaDefault.Version
            };

            // Act
            var dto = await sut.HandleAsync(input);

            // Assert
            Assert.That(dto.YaEraPorDefecto, Is.True);
            _repo.Verify(r => r.UnsetDefaultForTipoAsync(It.IsAny<EmpresaId>(), It.IsAny<TipoComprobanteCodigo>(), It.IsAny<CancellationToken>()), Times.Never);
            _repo.Verify(r => r.UpdateAsync(It.IsAny<SerieComprobante>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public void Falla_si_serie_pertenece_a_otra_empresa()
        {
            var otraEmpresa = EmpresaId.From("20123456789");
            var candidata = NuevaSerie(otraEmpresa, _est, "01", "FE01", 1);

            _repo.Setup(r => r.GetByIdAsync(candidata.Id, It.IsAny<CancellationToken>())).ReturnsAsync(candidata);

            var sut = new EstablecerSeriePorDefectoUseCase(_repo.Object, _uow.Object, _tenant.Object);

            var input = new EstablecerSeriePorDefectoInputDto
            {
                SerieComprobanteId = candidata.Id.ToString("D"),
                ExpectedVersion = candidata.Version
            };

            Assert.That(async () => await sut.HandleAsync(input),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("no pertenece"));
        }

        [Test]
        public void Falla_si_la_serie_esta_inhabilitada()
        {
            var inhabilitada = NuevaSerie(_empresa, _est, "01", "FE01", 1, porDefecto: false, habilitada: false);

            _repo.Setup(r => r.GetByIdAsync(inhabilitada.Id, It.IsAny<CancellationToken>())).ReturnsAsync(inhabilitada);
            _repo.Setup(r => r.GetDefaultByTipoAsync(_empresa, inhabilitada.Tipo, It.IsAny<CancellationToken>())).ReturnsAsync((SerieComprobante?)null);

            var sut = new EstablecerSeriePorDefectoUseCase(_repo.Object, _uow.Object, _tenant.Object);

            var input = new EstablecerSeriePorDefectoInputDto
            {
                SerieComprobanteId = inhabilitada.Id.ToString("D"),
                ExpectedVersion = inhabilitada.Version
            };

            Assert.That(async () => await sut.HandleAsync(input),
                Throws.TypeOf<BusinessRuleException>().With.Message.Contains("por defecto"));
        }

        [Test]
        public void Falla_si_guid_invalido()
        {
            var sut = new EstablecerSeriePorDefectoUseCase(_repo.Object, _uow.Object, _tenant.Object);

            var input = new EstablecerSeriePorDefectoInputDto
            {
                SerieComprobanteId = "no-guid",
                ExpectedVersion = 0
            };

            Assert.That(async () => await sut.HandleAsync(input),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }
    }
}
