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
    public class AgregarPrecioEspecificoUseCaseTests
    {
        [Test]
        public async Task HandleAsync_CuandoDatosValidos_AgregaPrecioYCommit()
        {
            // Arrange
             // Arrange
    var repo = new InMemoryListaPrecioRepository();
    var uow = new InMemoryUnitOfWork();

    var fechaInicio = DateTime.UtcNow;
    var fechaFin = fechaInicio.AddMonths(1);

    var vigenciaLista = new PeriodoVigencia(fechaInicio, fechaFin);
    var lista = new ListaPreciosBC.Domain.Aggregates.ListaPrecio(
        Guid.NewGuid(),
        TipoLista.CLIENTE,
        new CriterioLista(Guid.NewGuid(), null, null, null),
        Moneda.PEN,
        new Prioridad("Alta"),
        vigenciaLista,
        fechaInicio
    );
    await repo.AddAsync(lista);

    var useCase = new AgregarPrecioEspecificoUseCase(repo, uow);

    // La vigencia del precio debe estar dentro de la vigencia de la lista
    var vigenciaPrecio = new PeriodoVigencia(
        fechaInicio.AddDays(1),
        fechaFin.AddDays(-1)
    );

    var dto = new AgregarPrecioEspecificoDto
    {
        ListaPrecioId = lista.ListaPrecioId,
        Valor = 100,
        Moneda = Moneda.PEN,
        Prioridad = new Prioridad("Alta"),
        Vigencia = vigenciaPrecio,
        UsuarioId = "usuario-test"
    };

    // Act
    var precioId = await useCase.HandleAsync(dto);

    // Assert
    Assert.That(precioId, Is.Not.EqualTo(Guid.Empty));
    Assert.That(uow.WasCommitted, Is.True);
        }
    }
}