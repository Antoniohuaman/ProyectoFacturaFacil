using System;
using System.Threading.Tasks;
using ListaPreciosBC.Application.UseCases;
using ListaPreciosBC.Adapters.Output.Persistence.InMemory;
using ListaPreciosBC.Domain.ValueObjects;
using ListaPreciosBC.Domain.Entities;
using NUnit.Framework;

namespace ListaPreciosBC.Tests.UnitTests
{
    public class ActualizarVigenciaListaPrecioUseCaseTests
    {
        [Test]
        public async Task HandleAsync_CuandoDatosValidos_ActualizaVigenciaYCommit()
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
                new Prioridad("Alta"),
                new PeriodoVigencia(fechaInicio, fechaFin),
                fechaInicio
            );
            await repo.AddAsync(lista);

            var useCase = new ActualizarVigenciaListaPrecioUseCase(repo, uow);

            var nuevaVigencia = new PeriodoVigencia(fechaInicio.AddDays(2), fechaFin.AddDays(2));

            // Act
            await useCase.HandleAsync(lista.ListaPrecioId, nuevaVigencia);

            // Assert
            Assert.That(lista.Vigencia.FechaInicio, Is.EqualTo(nuevaVigencia.FechaInicio));
            Assert.That(lista.Vigencia.FechaFin, Is.EqualTo(nuevaVigencia.FechaFin));
            Assert.That(uow.WasCommitted, Is.True);
        }
    }
}