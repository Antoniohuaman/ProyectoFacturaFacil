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

namespace ConfiguracionSistemaBC.Tests.UseCases
{
    [TestFixture]
    public class EliminarSerieComprobanteUseCaseTests
    {
    private Mock<ISerieComprobanteRepository> _seriesRepo = null!;
    private Mock<IUnitOfWork> _uow = null!;
    private Mock<ITenantContext> _tenant = null!;

        private EmpresaId _empresa = null!;
        private EstablecimientoId _est = null!;

        [SetUp]
        public void SetUp()
        {
            _seriesRepo = new Mock<ISerieComprobanteRepository>();
            _uow = new Mock<IUnitOfWork>();
            _tenant = new Mock<ITenantContext>();

            // Empresa desde RUC “20600893409” (canonizado en EmpresaId)
            _empresa = EmpresaId.From("20600893409");
            _tenant.Setup(t => t.EmpresaId).Returns(_empresa);

            _est = EstablecimientoId.New();
        }

        private static SerieComprobante NuevaSerie(EmpresaId emp, EstablecimientoId est, string tipo, string serie, int corr = 1)
            => SerieComprobante.Crear(
                emp,
                TipoComprobanteCodigo.From(tipo),
                SerieCodigo.From(serie),
                est,
                TipoOperacion.Default,
                Correlativo.From(corr),
                esPorDefecto: false,
                habilitada: true);

        [Test]
        public async Task Elimina_OK_cuando_nunca_fue_usada()
        {
            // Arrange
            var s = NuevaSerie(_empresa, _est, "01", "FE01", 1);
            var input = new EliminarSerieComprobanteInputDto
            {
                SerieComprobanteId = s.Id.ToString("D"),
                ExpectedVersion = s.Version
            };

            _seriesRepo.Setup(r => r.GetByIdAsync(s.Id, It.IsAny<CancellationToken>())).ReturnsAsync(s);

            var sut = new EliminarSerieComprobanteUseCase(_seriesRepo.Object, _uow.Object, _tenant.Object);

            // Act
            var outDto = await sut.HandleAsync(input);

            // Assert
            Assert.That(outDto.Eliminado, Is.True);
            Assert.That(outDto.SerieComprobanteId, Is.EqualTo(s.Id));
            Assert.That(outDto.EmpresaId, Is.EqualTo(_empresa.Value));
            Assert.That(outDto.Serie, Is.EqualTo("FE01"));

            _seriesRepo.Verify(r => r.DeleteAsync(s.Id, s.Version, It.IsAny<CancellationToken>()), Times.Once);
            _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void Falla_si_la_serie_no_pertenece_a_la_empresa_del_contexto()
        {
            var otraEmpresa = EmpresaId.From("20123456789");
            var s = NuevaSerie(otraEmpresa, _est, "01", "FE01", 1);

            _seriesRepo.Setup(r => r.GetByIdAsync(s.Id, It.IsAny<CancellationToken>())).ReturnsAsync(s);

            var input = new EliminarSerieComprobanteInputDto
            {
                SerieComprobanteId = s.Id.ToString("D"),
                ExpectedVersion = s.Version
            };

            var sut = new EliminarSerieComprobanteUseCase(_seriesRepo.Object, _uow.Object, _tenant.Object);

            Assert.That(async () => await sut.HandleAsync(input),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("no pertenece"));
            _seriesRepo.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public void Falla_si_la_serie_ya_fue_usada()
        {
            var s = NuevaSerie(_empresa, _est, "03", "BE01", 1);
            // Marcar como usada (reserva primer correlativo)
            _ = s.ReservarSiguiente();

            _seriesRepo.Setup(r => r.GetByIdAsync(s.Id, It.IsAny<CancellationToken>())).ReturnsAsync(s);

            var input = new EliminarSerieComprobanteInputDto
            {
                SerieComprobanteId = s.Id.ToString("D"),
                ExpectedVersion = s.Version
            };

            var sut = new EliminarSerieComprobanteUseCase(_seriesRepo.Object, _uow.Object, _tenant.Object);

            Assert.That(async () => await sut.HandleAsync(input),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("ya fue usada"));
            _seriesRepo.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public void Falla_si_guid_invalido()
        {
            var sut = new EliminarSerieComprobanteUseCase(_seriesRepo.Object, _uow.Object, _tenant.Object);

            var input = new EliminarSerieComprobanteInputDto
            {
                SerieComprobanteId = "not-a-guid",
                ExpectedVersion = 0
            };

            Assert.That(async () => await sut.HandleAsync(input),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Propaga_excepcion_de_concurrencia_del_repo()
        {
            var s = NuevaSerie(_empresa, _est, "01", "FE01", 1);


            _seriesRepo.Setup(r => r.GetByIdAsync(s.Id, It.IsAny<CancellationToken>())).ReturnsAsync(s);
            _seriesRepo.Setup(r => r.DeleteAsync(s.Id, It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Throws(new InvalidOperationException("Versión no coincide"));

            var sut = new EliminarSerieComprobanteUseCase(_seriesRepo.Object, _uow.Object, _tenant.Object);

            var input = new EliminarSerieComprobanteInputDto
            {
                SerieComprobanteId = s.Id.ToString("D"),
                ExpectedVersion = 999 // versión errónea
            };

            Assert.That(async () => await sut.HandleAsync(input),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("Versión"));
        }
    }
}