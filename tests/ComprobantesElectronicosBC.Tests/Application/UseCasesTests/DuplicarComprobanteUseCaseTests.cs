using System;
using System.Runtime.CompilerServices; // RuntimeHelpers.GetUninitializedObject (no obsoleto)
using System.Threading;
using System.Threading.Tasks;
using ComprobantesElectronicosBC.Application.UseCases.DuplicarComprobante;
using ComprobantesElectronicosBC.Domain.Aggregates;
using ComprobantesElectronicosBC.Domain.Repositories;
using Moq;
using SharedKernel.Exceptions;

namespace ComprobantesElectronicosBC.Tests.Application
{
    public class DuplicarComprobanteUseCaseTests
    {
        // Creamos un agregado "dummy" sin ejecutar ctor ni lógicas internas del dominio.
        private static ComprobanteElectronico DummyAgg()
            => (ComprobanteElectronico)RuntimeHelpers.GetUninitializedObject(typeof(ComprobanteElectronico));

        [Test]
        public async Task Duplicar_sin_overrides_crea_borrador_y_persiste()
        {
            // Arrange
            var repo = new Mock<IComprobanteRepository>(MockBehavior.Strict);
            var uow  = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var dup  = new Mock<IComprobanteDuplicator>(MockBehavior.Strict);

            var sourceId = Guid.NewGuid();
            var input = new DuplicarComprobanteInputDto
            {
                SourceId = sourceId,
                Serie = null,
                Numero = null,
                NuevaFechaEmision = null
            };

            var original = DummyAgg();
            repo.Setup(r => r.GetByIdAsync(sourceId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(original);

            // No se debe consultar ExistsSerieNumeroAsync cuando no hay overrides
            var nuevo = DummyAgg();
            var output = new DuplicarComprobanteOutputDto
            {
                NuevoId = Guid.NewGuid(),
                TipoComprobante = "01",
                Serie = "F001",
                Numero = 999,
                Estado = "Borrador"
            };

            dup.Setup(d => d.DuplicarAsync(original, input, It.IsAny<CancellationToken>()))
               .ReturnsAsync((nuevo, output));

            repo.Setup(r => r.AddAsync(nuevo, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(1);

            var sut = new DuplicarComprobanteUseCase(repo.Object, uow.Object, dup.Object);

            // Act
            var result = await sut.ExecuteAsync(input);

            // Assert
            Assert.That(result.EsDuplicado, Is.True);
            Assert.That(result.Estado, Is.EqualTo("Borrador"));
            Assert.That(result.TipoComprobante, Is.EqualTo("01"));
            Assert.That(result.Serie, Is.EqualTo("F001"));
            Assert.That(result.Numero, Is.EqualTo(999));
            Assert.That(result.NuevoId, Is.Not.EqualTo(Guid.Empty));

            repo.VerifyAll();
            uow.VerifyAll();
            dup.VerifyAll();
        }

        [Test]
        public async Task Duplicar_con_serie_y_numero_fijos_valida_colision_y_persiste()
        {
            // Arrange
            var repo = new Mock<IComprobanteRepository>(MockBehavior.Strict);
            var uow  = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var dup  = new Mock<IComprobanteDuplicator>(MockBehavior.Strict);

            var sourceId = Guid.NewGuid();
            var input = new DuplicarComprobanteInputDto
            {
                SourceId = sourceId,
                Serie = "F001",
                Numero = 25
            };

            var original = DummyAgg();
            repo.Setup(r => r.GetByIdAsync(sourceId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(original);

            // Verificación de unicidad previa (no existe colisión)
            repo.Setup(r => r.ExistsSerieNumeroAsync("F001", 25, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var nuevo = DummyAgg();
            var output = new DuplicarComprobanteOutputDto
            {
                NuevoId = Guid.NewGuid(),
                TipoComprobante = "01",
                Serie = "F001",
                Numero = 25,
                Estado = "Borrador"
            };

            dup.Setup(d => d.DuplicarAsync(original, input, It.IsAny<CancellationToken>()))
               .ReturnsAsync((nuevo, output));

            repo.Setup(r => r.AddAsync(nuevo, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(1);

            var sut = new DuplicarComprobanteUseCase(repo.Object, uow.Object, dup.Object);

            // Act
            var result = await sut.ExecuteAsync(input);

            // Assert
            Assert.That(result.Serie, Is.EqualTo("F001"));
            Assert.That(result.Numero, Is.EqualTo(25));
            repo.VerifyAll();
            uow.VerifyAll();
            dup.VerifyAll();
        }

        [Test]
        public void Duplicar_con_serie_y_numero_fijos_con_colision_lanza_regla()
        {
            // Arrange
            var repo = new Mock<IComprobanteRepository>(MockBehavior.Strict);
            var uow  = new Mock<IUnitOfWork>();
            var dup  = new Mock<IComprobanteDuplicator>();

            var sourceId = Guid.NewGuid();
            var input = new DuplicarComprobanteInputDto
            {
                SourceId = sourceId,
                Serie = "B001",
                Numero = 7
            };

            var original = DummyAgg();
            repo.Setup(r => r.GetByIdAsync(sourceId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(original);

            repo.Setup(r => r.ExistsSerieNumeroAsync("B001", 7, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var sut = new DuplicarComprobanteUseCase(repo.Object, uow.Object, dup.Object);

            // Act + Assert
            Assert.ThrowsAsync<BusinessRuleException>(() => sut.ExecuteAsync(input));
            repo.Verify(r => r.ExistsSerieNumeroAsync("B001", 7, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void Duplicar_con_solo_serie_o_solo_numero_lanza_argument_exception()
        {
            // Arrange
            var repo = new Mock<IComprobanteRepository>();
            var uow  = new Mock<IUnitOfWork>();
            var dup  = new Mock<IComprobanteDuplicator>();

            var sut = new DuplicarComprobanteUseCase(repo.Object, uow.Object, dup.Object);

            // Solo serie
            var input1 = new DuplicarComprobanteInputDto { SourceId = Guid.NewGuid(), Serie = "F001", Numero = null };
            Assert.ThrowsAsync<ArgumentException>(() => sut.ExecuteAsync(input1));

            // Solo número
            var input2 = new DuplicarComprobanteInputDto { SourceId = Guid.NewGuid(), Serie = null, Numero = 1 };
            Assert.ThrowsAsync<ArgumentException>(() => sut.ExecuteAsync(input2));
        }

        [Test]
        public void Duplicar_origen_inexistente_lanza_NotFound()
        {
            // Arrange
            var repo = new Mock<IComprobanteRepository>(MockBehavior.Strict);
            var uow  = new Mock<IUnitOfWork>();
            var dup  = new Mock<IComprobanteDuplicator>();

            var sourceId = Guid.NewGuid();
            repo.Setup(r => r.GetByIdAsync(sourceId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ComprobanteElectronico?)null);

            var sut = new DuplicarComprobanteUseCase(repo.Object, uow.Object, dup.Object);

            var input = new DuplicarComprobanteInputDto { SourceId = sourceId };

            // Act + Assert
            Assert.ThrowsAsync<NotFoundException>(() => sut.ExecuteAsync(input));
            repo.VerifyAll();
        }
    }
}
