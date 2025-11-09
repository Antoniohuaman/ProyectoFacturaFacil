using System;
using System.Runtime.CompilerServices; // RuntimeHelpers.GetUninitializedObject (no obsoleto)
using System.Threading;
using System.Threading.Tasks;
using ComprobantesElectronicosBC.Application.UseCases.AnularComprobante;
using ComprobantesElectronicosBC.Domain.Aggregates;
using ComprobantesElectronicosBC.Domain.Repositories;
using IUnitOfWork = ComprobantesElectronicosBC.Application.Interfaces.IUnitOfWork;
using Moq;
using SharedKernel.Exceptions;

namespace ComprobantesElectronicosBC.Tests.Application
{
    public class AnularComprobanteUseCaseTests
    {
        // Dummy seguro del agregado sin invocar constructores (evita APIs obsoletas).
        private static ComprobanteElectronico DummyAgg()
            => (ComprobanteElectronico)RuntimeHelpers.GetUninitializedObject(typeof(ComprobanteElectronico));

        [Test]
        public async Task Anular_ok_persiste_y_retorna_output()
        {
            // Arrange
            var repo = new Mock<IComprobanteRepository>(MockBehavior.Strict);
            var uow  = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var an   = new Mock<IComprobanteAnulador>(MockBehavior.Strict);

            var id = Guid.NewGuid();
            var input = new AnularComprobanteInputDto
            {
                ComprobanteId = id,
                Motivo = "Error de emisión",
                AnuladoEnUtc = DateTimeOffset.UtcNow
            };

            var original = DummyAgg();
            repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(original);

            var anulado = DummyAgg();
            var output = new AnularComprobanteOutputDto
            {
                ComprobanteId = id,
                TipoComprobante = "01",
                Serie = "F001",
                Numero = 999,
                Estado = "Anulado",
                AnuladoEnUtc = DateTimeOffset.UtcNow
            };

            an.Setup(a => a.AnularAsync(original, input, It.IsAny<CancellationToken>()))
              .ReturnsAsync((anulado, output));

            repo.Setup(r => r.UpdateAsync(anulado, original.Version, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

                uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

            var sut = new AnularComprobanteUseCase(repo.Object, uow.Object, an.Object);

            // Act
            var result = await sut.ExecuteAsync(input);

            // Assert
            Assert.That(result.EstaAnulado, Is.True);
            Assert.That(result.Serie, Is.EqualTo("F001"));
            Assert.That(result.Numero, Is.EqualTo(999));
            Assert.That(result.TipoComprobante, Is.EqualTo("01"));
            repo.VerifyAll();
            uow.VerifyAll();
            an.VerifyAll();
        }

        [Test]
        public void Anular_sin_motivo_lanza_argument_exception()
        {
            var repo = new Mock<IComprobanteRepository>();
            var uow  = new Mock<IUnitOfWork>();
            var an   = new Mock<IComprobanteAnulador>();

            var sut = new AnularComprobanteUseCase(repo.Object, uow.Object, an.Object);

            var inputVacio = new AnularComprobanteInputDto
            {
                ComprobanteId = Guid.NewGuid(),
                Motivo = "   " // solo espacios
            };

            Assert.ThrowsAsync<ArgumentException>(() => sut.ExecuteAsync(inputVacio));
        }

        [Test]
        public void Anular_excede_longitud_motivo_lanza_argument_exception()
        {
            var repo = new Mock<IComprobanteRepository>();
            var uow  = new Mock<IUnitOfWork>();
            var an   = new Mock<IComprobanteAnulador>();

            var sut = new AnularComprobanteUseCase(repo.Object, uow.Object, an.Object);

            var largo = new string('x', AnularComprobanteUseCase.MaxMotivoLength + 1);
            var input = new AnularComprobanteInputDto
            {
                ComprobanteId = Guid.NewGuid(),
                Motivo = largo
            };

            Assert.ThrowsAsync<ArgumentException>(() => sut.ExecuteAsync(input));
        }

        [Test]
        public void Anular_comprobante_inexistente_lanza_NotFound()
        {
            var repo = new Mock<IComprobanteRepository>(MockBehavior.Strict);
            var uow  = new Mock<IUnitOfWork>();
            var an   = new Mock<IComprobanteAnulador>();

            var id = Guid.NewGuid();
            repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ComprobanteElectronico?)null);

            var sut = new AnularComprobanteUseCase(repo.Object, uow.Object, an.Object);

            var input = new AnularComprobanteInputDto { ComprobanteId = id, Motivo = "No corresponde" };

            Assert.ThrowsAsync<NotFoundException>(() => sut.ExecuteAsync(input));
            repo.VerifyAll();
        }

        [Test]
        public void Anular_con_regla_negocio_no_permitida_propagada_desde_adaptador()
        {
            var repo = new Mock<IComprobanteRepository>(MockBehavior.Strict);
            var uow  = new Mock<IUnitOfWork>();
            var an   = new Mock<IComprobanteAnulador>(MockBehavior.Strict);

            var id = Guid.NewGuid();
            var original = DummyAgg();

            repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(original);

            var input = new AnularComprobanteInputDto
            {
                ComprobanteId = id,
                Motivo = "Intento de anular ya anulado"
            };

            an.Setup(a => a.AnularAsync(original, input, It.IsAny<CancellationToken>()))
              .ThrowsAsync(new BusinessRuleException("El comprobante ya está anulado."));

            var sut = new AnularComprobanteUseCase(repo.Object, uow.Object, an.Object);

            Assert.ThrowsAsync<BusinessRuleException>(() => sut.ExecuteAsync(input));

            repo.VerifyAll();
            an.VerifyAll();
        }
    }
}
