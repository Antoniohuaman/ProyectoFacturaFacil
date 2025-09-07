using System;
using System.Threading;
using System.Threading.Tasks;
using CatalogoArticulosBC.Application.UseCases.EliminarTodosLosProductos;
using CatalogoArticulosBC.Domain.Repositories;
using Moq;
using NUnit.Framework;
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace CatalogoArticulosBC.Tests.Application.UseCases
{
    [TestFixture]
    public class EliminarTodosLosProductosUseCaseTests
    {
        private static EmpresaId EMP(string v = "20123456789") => EmpresaId.From(v);

        [Test]
        public async Task EliminarTodos_Exitoso_DevuelveConteoYConfirmaCommit()
        {
            // Arrange
            var repo   = new Mock<IProductoRepository>(MockBehavior.Strict);
            var uow    = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            var empresa = EMP();
            tenant.Setup(t => t.EmpresaId).Returns(empresa);

            repo.Setup(r => r.DeleteAllAsync(
                    It.Is<EmpresaId>(e => e.Value == empresa.Value),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(42);

            uow.Setup(x => x.CommitAsync()).Returns(Task.CompletedTask);

            var sut = new EliminarTodosLosProductosUseCase(repo.Object, uow.Object, tenant.Object);

            // Act
            var output = await sut.ExecuteAsync(new EliminarTodosLosProductosInputDto { Confirmar = true });

            // Assert
            Assert.That(output.Exitoso, Is.True);
            Assert.That(output.EmpresaId, Is.EqualTo(empresa.Value));
            Assert.That(output.CantidadEliminada, Is.EqualTo(42));
            // No validamos estrictamente la marca de tiempo; basta con que no sea default
            Assert.That(output.EjecutadoEnUtc, Is.Not.EqualTo(default(DateTimeOffset)));

            repo.VerifyAll();
            uow.VerifyAll();
            tenant.VerifyAll();
        }

        [Test]
        public async Task EliminarTodos_SinProductos_Exitoso_ContadorCero()
        {
            // Arrange
            var repo   = new Mock<IProductoRepository>(MockBehavior.Strict);
            var uow    = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            var empresa = EMP("20999999999");
            tenant.Setup(t => t.EmpresaId).Returns(empresa);

            repo.Setup(r => r.DeleteAllAsync(
                    It.Is<EmpresaId>(e => e.Value == empresa.Value),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            uow.Setup(x => x.CommitAsync()).Returns(Task.CompletedTask);

            var sut = new EliminarTodosLosProductosUseCase(repo.Object, uow.Object, tenant.Object);

            // Act
            var output = await sut.ExecuteAsync(new EliminarTodosLosProductosInputDto { Confirmar = true });

            // Assert
            Assert.That(output.Exitoso, Is.True);
            Assert.That(output.EmpresaId, Is.EqualTo(empresa.Value));
            Assert.That(output.CantidadEliminada, Is.EqualTo(0));

            repo.VerifyAll();
            uow.VerifyAll();
            tenant.VerifyAll();
        }

        [Test]
        public void EliminarTodos_DebeFallar_SiNoConfirma()
        {
            // Arrange
            var repo   = new Mock<IProductoRepository>(MockBehavior.Strict);
            var uow    = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            var sut = new EliminarTodosLosProductosUseCase(repo.Object, uow.Object, tenant.Object);

            // Act + Assert
            Assert.ThrowsAsync<BusinessRuleException>(async () =>
                await sut.ExecuteAsync(new EliminarTodosLosProductosInputDto { Confirmar = false }));

            // No debe tocar repo ni uow
            repo.Verify(r => r.DeleteAllAsync(It.IsAny<EmpresaId>(), It.IsAny<CancellationToken>()), Times.Never);
            uow.Verify(x => x.CommitAsync(), Times.Never);
        }

        [Test]
        public void EliminarTodos_DebeFallar_SiInputNulo()
        {
            // Arrange
            var repo   = new Mock<IProductoRepository>();
            var uow    = new Mock<IUnitOfWork>();
            var tenant = new Mock<ITenantContext>();

            var sut = new EliminarTodosLosProductosUseCase(repo.Object, uow.Object, tenant.Object);

            // Act + Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () => await sut.ExecuteAsync(null!));
        }
    }
}
