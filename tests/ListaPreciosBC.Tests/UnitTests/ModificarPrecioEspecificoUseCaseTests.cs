using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ListaPreciosBC.Application.DTOs;
using ListaPreciosBC.Application.Interfaces;
using ListaPreciosBC.Application.UseCases;
using ListaPreciosBC.Domain.Entities;
using ListaPreciosBC.Domain.ValueObjects;
using ListaPreciosBC.Domain.Aggregates;
using ListaPreciosBC.Domain.Events;
using Moq;
using NUnit.Framework;

namespace ListaPreciosBC.Tests
{
    public class ModificarPrecioEspecificoUseCaseTests
    {
        [Test]
        public async Task EjecutarAsync_ModificaPrecioYRegistraHistorialYPublicaEvento()
        {
            // Arrange
            var repoMock = new Mock<IListaPrecioRepository>();
            var eventBusMock = new Mock<IEventBus>();

            var precioId = Guid.NewGuid();
            var listaId = Guid.NewGuid();
            var precio = new PrecioEspecifico(
                precioId,
                listaId,
                10m,
                Moneda.PEN,
                Prioridad.Alta,
                new PeriodoVigencia(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(10))
            );
            repoMock.Setup(r => r.ObtenerPrecioEspecificoPorIdAsync(precioId)).ReturnsAsync(precio);
            var criterio = new CriterioLista(null, null, null, null);
            var lista = new ListaPrecio(
                listaId,
                TipoLista.PROMOCION,                // Usa un valor válido de tu enum TipoLista
                criterio,        // Usa un valor válido de tu enum CriterioLista
                Moneda.PEN,                       // O el que corresponda
                Prioridad.Alta,                   // O el que corresponda
                new PeriodoVigencia(DateTime.Today.AddDays(-5), DateTime.Today.AddDays(10)),
                DateTime.Today.AddDays(-5)        // Fecha de creación
            );
            repoMock.Setup(r => r.GetByIdAsync(listaId)).ReturnsAsync(lista);

            var dto = new ModificarPrecioEspecificoDto
            {
                PrecioEspecificoId = precioId,
                NuevoValor = 20m,
                NuevaMoneda = "USD",
                NuevaFechaInicio = DateTime.Today,
                NuevaFechaFin = DateTime.Today.AddDays(5),
                UsuarioId = "user1",
                Motivo = "Ajuste"
            };

            var useCase = new ModificarPrecioEspecificoUseCase(repoMock.Object, eventBusMock.Object);

            // Act
            await useCase.EjecutarAsync(dto);

            // Assert
            repoMock.Verify(r => r.AgregarHistorialAsync(It.IsAny<HistorialPrecio>()), Times.Once);
            repoMock.Verify(r => r.ActualizarPrecioEspecificoAsync(It.IsAny<PrecioEspecifico>()), Times.Once);
            eventBusMock.Verify(e => e.PublishAsync(It.IsAny<object>()), Times.Once);

            Assert.That(precio.Valor, Is.EqualTo(20m));
            Assert.That(precio.Moneda, Is.EqualTo(Moneda.USD));
            Assert.That(precio.Vigencia.FechaInicio, Is.EqualTo(DateTime.Today));
            Assert.That(precio.Vigencia.FechaFin, Is.EqualTo(DateTime.Today.AddDays(5)));
        }
    }
}