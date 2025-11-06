using System;
using System.Runtime.CompilerServices; // RuntimeHelpers.GetUninitializedObject (no obsoleto)
using System.Threading;
using System.Threading.Tasks;
using ComprobantesElectronicosBC.Application.UseCases.CorregirComprobante;
using ComprobantesElectronicosBC.Domain.Aggregates;
using ComprobantesElectronicosBC.Domain.Repositories;
using IUnitOfWork = ComprobantesElectronicosBC.Application.Interfaces.IUnitOfWork;
using Moq;
using SharedKernel.Exceptions;

namespace ComprobantesElectronicosBC.Tests.Application
{
    public class CorregirComprobanteUseCaseTests
    {
        // Crea un agregado dummy sin tocar constructores/lógicas internas.
        private static ComprobanteElectronico DummyAgg()
            => (ComprobanteElectronico)RuntimeHelpers.GetUninitializedObject(typeof(ComprobanteElectronico));

        [Test]
        public async Task Corregir_sin_cambiar_serie_numero_persiste_y_retorna_output()
        {
            // Arrange
            var repo = new Mock<IComprobanteRepository>(MockBehavior.Strict);
            var uow  = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var corr = new Mock<IComprobanteCorrector>(MockBehavior.Strict);

            var id = Guid.NewGuid();
            var input = new CorregirComprobanteInputDto
            {
                ComprobanteId = id,
                Observaciones = "Actualizar observaciones"
                // Serie/Numero null => no debe consultarse ExistsSerieNumeroAsync
            };

            var original = DummyAgg();
            repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(original);

            var actualizado = DummyAgg();
            var output = new CorregirComprobanteOutputDto
            {
                ComprobanteId = id,
                TipoComprobante = "03",
                Serie = "B001",
                Numero = 123,
                Estado = "Borrador"
            };

            corr.Setup(c => c.CorregirAsync(original, input, It.IsAny<CancellationToken>()))
                .ReturnsAsync((actualizado, output));

            repo.Setup(r => r.UpdateAsync(actualizado, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

                uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

            var sut = new CorregirComprobanteUseCase(repo.Object, uow.Object, corr.Object);

            // Act
            var result = await sut.ExecuteAsync(input);

            // Assert
            Assert.That(result.CorreccionAplicada, Is.True);
            Assert.That(result.Serie, Is.EqualTo("B001"));
            Assert.That(result.Numero, Is.EqualTo(123));
            Assert.That(result.TipoComprobante, Is.EqualTo("03"));
            repo.VerifyAll();
            uow.VerifyAll();
            corr.VerifyAll();
        }

        [Test]
        public async Task Corregir_cambiando_serie_y_numero_valida_unicidad()
        {
            // Arrange
            var repo = new Mock<IComprobanteRepository>(MockBehavior.Strict);
            var uow  = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var corr = new Mock<IComprobanteCorrector>(MockBehavior.Strict);

            var id = Guid.NewGuid();
            var input = new CorregirComprobanteInputDto
            {
                ComprobanteId = id,
                Serie = "F001",
                Numero = 456
            };

            var original = DummyAgg();
            repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(original);

            // Debe comprobar unicidad
            repo.Setup(r => r.ExistsSerieNumeroAsync("F001", 456, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var actualizado = DummyAgg();
            var output = new CorregirComprobanteOutputDto
            {
                ComprobanteId = id,
                TipoComprobante = "01",
                Serie = "F001",
                Numero = 456,
                Estado = "Borrador"
            };

            corr.Setup(c => c.CorregirAsync(original, input, It.IsAny<CancellationToken>()))
                .ReturnsAsync((actualizado, output));

            repo.Setup(r => r.UpdateAsync(actualizado, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

                uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

            var sut = new CorregirComprobanteUseCase(repo.Object, uow.Object, corr.Object);

            // Act
            var result = await sut.ExecuteAsync(input);

            // Assert
            Assert.That(result.Serie, Is.EqualTo("F001"));
            Assert.That(result.Numero, Is.EqualTo(456));
            repo.VerifyAll();
            uow.VerifyAll();
            corr.VerifyAll();
        }

        [Test]
        public void Corregir_con_serie_sin_numero_o_viceversa_lanza_argument_exception()
        {
            var repo = new Mock<IComprobanteRepository>();
            var uow  = new Mock<IUnitOfWork>();
            var corr = new Mock<IComprobanteCorrector>();

            var sut = new CorregirComprobanteUseCase(repo.Object, uow.Object, corr.Object);

            // Solo serie
            var input1 = new CorregirComprobanteInputDto
            {
                ComprobanteId = Guid.NewGuid(),
                Serie = "F001",
                Numero = null
            };
            Assert.ThrowsAsync<ArgumentException>(() => sut.ExecuteAsync(input1));

            // Solo número
            var input2 = new CorregirComprobanteInputDto
            {
                ComprobanteId = Guid.NewGuid(),
                Serie = null,
                Numero = 1
            };
            Assert.ThrowsAsync<ArgumentException>(() => sut.ExecuteAsync(input2));
        }

        [Test]
        public void Corregir_con_colision_de_serie_numero_lanza_regla()
        {
            var repo = new Mock<IComprobanteRepository>();
            var uow  = new Mock<IUnitOfWork>();
            var corr = new Mock<IComprobanteCorrector>();

            var id = Guid.NewGuid();
            var input = new CorregirComprobanteInputDto
            {
                ComprobanteId = id,
                Serie = "B001",
                Numero = 77
            };

            var original = DummyAgg();
            repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(original);

            repo.Setup(r => r.ExistsSerieNumeroAsync("B001", 77, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var sut = new CorregirComprobanteUseCase(repo.Object, uow.Object, corr.Object);

            Assert.ThrowsAsync<BusinessRuleException>(() => sut.ExecuteAsync(input));
            repo.Verify(r => r.ExistsSerieNumeroAsync("B001", 77, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void Corregir_comprobante_inexistente_lanza_NotFound()
        {
            var repo = new Mock<IComprobanteRepository>(MockBehavior.Strict);
            var uow  = new Mock<IUnitOfWork>();
            var corr = new Mock<IComprobanteCorrector>();

            var id = Guid.NewGuid();
            repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ComprobanteElectronico?)null);

            var sut = new CorregirComprobanteUseCase(repo.Object, uow.Object, corr.Object);

            var input = new CorregirComprobanteInputDto { ComprobanteId = id };
            Assert.ThrowsAsync<NotFoundException>(() => sut.ExecuteAsync(input));

            repo.VerifyAll();
        }
    }
}
