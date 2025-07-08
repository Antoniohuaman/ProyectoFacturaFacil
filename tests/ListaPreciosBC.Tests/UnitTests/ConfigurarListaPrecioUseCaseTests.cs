using System;
using System.Threading.Tasks;
using ListaPreciosBC.Application.DTOs;
using ListaPreciosBC.Application.UseCases;
using ListaPreciosBC.Adapters.Output.Persistence.InMemory;
using ListaPreciosBC.Domain.ValueObjects;
using ListaPreciosBC.Domain.Entities;
using NUnit.Framework;


namespace ListaPreciosBC.Tests.UnitTests
{
    public class ConfigurarListaPrecioUseCaseTests
    {
        [Test]
        public async Task HandleAsync_CuandoDatosValidos_CreaListaYCommit()
        {
            // Arrange
            var repo = new InMemoryListaPrecioRepository();
            var uow = new InMemoryUnitOfWork();
            var useCase = new ConfigurarListaPrecioUseCase(repo, uow);

            var dto = new ConfigurarListaPrecioDto
            {
                TipoLista = TipoLista.CLIENTE,
                Criterio = new CriterioLista(
                    clienteId: Guid.NewGuid(),
                    canalVenta: null,
                    rangoVolumen: null,
                    periodoVigencia: null
                ),
                Moneda = Moneda.PEN,
                Prioridad = new Prioridad(1),
                Vigencia = new PeriodoVigencia(DateTime.UtcNow, DateTime.UtcNow.AddMonths(1)),
                UsuarioId = "usuario-test"
            };

            // Act
            var listaPrecioId = await useCase.HandleAsync(dto);

            // Assert
            Assert.That(listaPrecioId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(uow.WasCommitted, Is.True);
        }
    }
}