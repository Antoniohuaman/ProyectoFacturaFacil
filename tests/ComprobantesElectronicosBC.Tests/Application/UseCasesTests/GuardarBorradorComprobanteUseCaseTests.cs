using System;
using System.Runtime.CompilerServices; // <- en vez de System.Runtime.Serialization
using System.Threading;
using System.Threading.Tasks;
using ComprobantesElectronicosBC.Application.UseCases.GuardarBorrador;
using ComprobantesElectronicosBC.Domain.Aggregates;
using ComprobantesElectronicosBC.Domain.Repositories;
using IUnitOfWork = ComprobantesElectronicosBC.Application.Interfaces.IUnitOfWork;
using Moq;
using SharedKernel.Exceptions;

namespace ComprobantesElectronicosBC.Tests.Application
{
    public class GuardarBorradorUseCaseTests
    {
        // Helper: crea una instancia "uninitialized" sin llamar ctor (no obsoleto).
        private static ComprobanteElectronico DummyAgg()
            => (ComprobanteElectronico)RuntimeHelpers.GetUninitializedObject(typeof(ComprobanteElectronico));

        [Test]
        public async Task Crear_nuevo_borrador_sin_conflicto_crea_y_persiste()
        {
            // Arrange
            var repo = new Mock<IComprobanteRepository>(MockBehavior.Strict);
            var uow  = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var fac  = new Mock<IComprobanteDraftFactory>(MockBehavior.Strict);

            var input = new GuardarBorradorInputDto
            {
                Id = null,
                TipoComprobante = "01", // Factura
                Serie = "F001",
                Numero = 1,
                EmpresaId = "EMP-001",
                TenantId = "TEN-001"
            };

            repo.Setup(r => r.GetBySerieNumeroAsync("F001", 1, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ComprobanteElectronico?)null);

            var nuevo = DummyAgg();
            fac.Setup(f => f.CrearAsync(input, It.IsAny<CancellationToken>()))
               .ReturnsAsync(nuevo);

            repo.Setup(r => r.AddAsync(nuevo, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

                uow.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

            var sut = new GuardarBorradorUseCase(repo.Object, uow.Object, fac.Object);

            // Act
            var outDto = await sut.ExecuteAsync(input);

            // Assert
            Assert.That(outDto.EsNuevo, Is.True);
            Assert.That(outDto.Estado, Is.EqualTo("Borrador"));
            Assert.That(outDto.Serie, Is.EqualTo("F001"));
            Assert.That(outDto.Numero, Is.EqualTo(1));

            repo.VerifyAll();
            uow.VerifyAll();
            fac.VerifyAll();
        }

        [Test]
        public void Crear_con_serie_numero_duplicado_lanza_BusinessRuleException()
        {
            // Arrange
            var repo = new Mock<IComprobanteRepository>();
            var uow  = new Mock<IUnitOfWork>();
            var fac  = new Mock<IComprobanteDraftFactory>();

            var input = new GuardarBorradorInputDto
            {
                Id = null,
                TipoComprobante = "03", // Boleta
                Serie = "B001",
                Numero = 12,
                EmpresaId = "EMP-001",
                TenantId = "TEN-001"
            };

            repo.Setup(r => r.GetBySerieNumeroAsync("B001", 12, It.IsAny<CancellationToken>()))
                .ReturnsAsync(DummyAgg());

            var sut = new GuardarBorradorUseCase(repo.Object, uow.Object, fac.Object);

            // Act + Assert
            Assert.ThrowsAsync<BusinessRuleException>(() => sut.ExecuteAsync(input));
        }

        [Test]
        public async Task Actualizar_borrador_existente_aplica_y_persiste()
        {
            // Arrange
            var repo = new Mock<IComprobanteRepository>(MockBehavior.Strict);
            var uow  = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var fac  = new Mock<IComprobanteDraftFactory>(MockBehavior.Strict);

            var id = Guid.NewGuid();
            var input = new GuardarBorradorInputDto
            {
                Id = id,
                TipoComprobante = "01",
                Serie = "F001",
                Numero = 5,
                EmpresaId = "EMP-001",
                TenantId = "TEN-001"
            };

            var existente = DummyAgg();
            repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existente);

            // Sin colisión (null)
            repo.Setup(r => r.GetBySerieNumeroAsync("F001", 5, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ComprobanteElectronico?)null);

            var actualizado = DummyAgg();
            fac.Setup(f => f.AplicarAsync(existente, input, It.IsAny<CancellationToken>()))
               .ReturnsAsync(actualizado);

            repo.Setup(r => r.UpdateAsync(actualizado, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

                uow.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

            var sut = new GuardarBorradorUseCase(repo.Object, uow.Object, fac.Object);

            // Act
            var outDto = await sut.ExecuteAsync(input);

            // Assert
            Assert.That(outDto.EsNuevo, Is.False);
            Assert.That(outDto.Estado, Is.EqualTo("Borrador"));
            Assert.That(outDto.Serie, Is.EqualTo("F001"));
            Assert.That(outDto.Numero, Is.EqualTo(5));

            repo.VerifyAll();
            uow.VerifyAll();
            fac.VerifyAll();
        }

        [Test]
        public void Actualizar_borrador_inexistente_lanza_NotFound()
        {
            // Arrange
            var repo = new Mock<IComprobanteRepository>();
            var uow  = new Mock<IUnitOfWork>();
            var fac  = new Mock<IComprobanteDraftFactory>();

            var id = Guid.NewGuid();
            var input = new GuardarBorradorInputDto
            {
                Id = id,
                TipoComprobante = "01",
                Serie = "F001",
                Numero = 100,
                EmpresaId = "EMP-001",
                TenantId = "TEN-001"
            };

            repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ComprobanteElectronico?)null);

            var sut = new GuardarBorradorUseCase(repo.Object, uow.Object, fac.Object);

            // Act + Assert
            Assert.ThrowsAsync<NotFoundException>(() => sut.ExecuteAsync(input));
        }

        [Test]
        public void Serie_incompatible_con_tipo_lanza_excepcion()
        {
            // Arrange
            var repo = new Mock<IComprobanteRepository>();
            var uow  = new Mock<IUnitOfWork>();
            var fac  = new Mock<IComprobanteDraftFactory>();

            var input = new GuardarBorradorInputDto
            {
                Id = null,
                TipoComprobante = "01", // Factura
                Serie = "B001",         // incompatible
                Numero = null,
                EmpresaId = "EMP-001",
                TenantId = "TEN-001"
            };

            var sut = new GuardarBorradorUseCase(repo.Object, uow.Object, fac.Object);

            // Act + Assert
            Assert.ThrowsAsync<InvalidOperationException>(() => sut.ExecuteAsync(input));
        }
    }
}
