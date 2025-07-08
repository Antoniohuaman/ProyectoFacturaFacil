using System;
using System.Threading.Tasks;
using ListaPreciosBC.Application.UseCases;
using ListaPreciosBC.Adapters.Output.Persistence.InMemory;
using ListaPreciosBC.Domain.ValueObjects;
using ListaPreciosBC.Domain.Entities;
using NUnit.Framework;

namespace ListaPreciosBC.Tests.UnitTests
{
    public class ObtenerListaPrecioVigenteUseCaseTests
    {
        [Test]
        public async Task HandleAsync_CuandoExisteListaVigente_DevuelveLista()
        {
            // Arrange
            var repo = new InMemoryListaPrecioRepository();
            var tipoLista = TipoLista.CLIENTE;
            var criterio = new CriterioLista(Guid.NewGuid(), null, null, null);
            var productoId = Guid.NewGuid();
            var fecha = DateTime.UtcNow;

            var lista = new ListaPreciosBC.Domain.Aggregates.ListaPrecio(
                Guid.NewGuid(),
                tipoLista,
                criterio,
                Moneda.PEN,
                new Prioridad(1),
                new PeriodoVigencia(fecha.AddDays(-1), fecha.AddDays(10)),
                fecha.AddDays(-2)
            );
            await repo.AddAsync(lista);

            var useCase = new ObtenerListaPrecioVigenteUseCase(repo);

            // Act
            var resultado = await useCase.HandleAsync(tipoLista, criterio, productoId, fecha);

            // Assert
            Assert.That(resultado, Is.Not.Null);
            Assert.That(resultado!.ListaPrecioId, Is.EqualTo(lista.ListaPrecioId));
        }
    }
}