using System;
using System.Threading.Tasks;
using ListaPreciosBC.Application.UseCases;
using ListaPreciosBC.Adapters.Output.Persistence.InMemory;
using ListaPreciosBC.Domain.ValueObjects;
using ListaPreciosBC.Domain.Entities;
using NUnit.Framework;

namespace ListaPreciosBC.Tests.UnitTests
{
    public class InhabilitarListaPrecioUseCaseTests
    {
        [Test]
        public async Task HandleAsync_CuandoListaActiva_DesactivaYCommit()
        {
            // Arrange
            var repo = new InMemoryListaPrecioRepository();
            var uow = new InMemoryUnitOfWork();

            var fechaInicio = DateTime.UtcNow;
            var fechaFin = fechaInicio.AddMonths(1);

            var lista = new ListaPreciosBC.Domain.Aggregates.ListaPrecio(
                Guid.NewGuid(),
                TipoLista.CLIENTE,
                new CriterioLista(Guid.NewGuid(), null, null, null),
                Moneda.PEN,
                new Prioridad(1),
                new PeriodoVigencia(fechaInicio, fechaFin),
                fechaInicio
            );
            await repo.AddAsync(lista);

            var useCase = new InhabilitarListaPrecioUseCase(repo, uow);

            // Act
            await useCase.HandleAsync(lista.ListaPrecioId, "usuario-test");

            // Assert
            Assert.That(lista.Activa, Is.False);
            Assert.That(uow.WasCommitted, Is.True);
        }
    }
}