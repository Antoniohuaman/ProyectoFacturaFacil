using System;
using System.Threading.Tasks;
using NUnit.Framework;
using CatalogoArticulosBC.Adapters.Output.Persistence.InMemory;
using CatalogoArticulosBC.Application.UseCases;
using CatalogoArticulosBC.Application.DTOs;
using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.ValueObjects;

namespace CatalogoArticulosBC.Tests.UnitTests.UseCases
{
    public class EliminarProductoSimpleUseCaseTests
    {
        [Test]
        public async Task HandleAsync_CuandoProductoExiste_EliminaYCommit()
        {
            // Arrange
            var repo = new InMemoryCatalogoArticulosRepository();
            var uow = new InMemoryUnitOfWork();
            var producto = new ProductoSimple(
                "SKU-DEL", "Eliminar", "Desc",
                new UnidadMedida("UN"),
                new AfectacionIGV("10%"),
                new CodigoSUNAT("1000"),
                new BaseImponibleVentas(40.01m),
                new CentroCosto("CC01"),
                new Presupuesto(50m),
                new Peso(0.5m),
                tipo: TipoProducto.Bien, // O TipoProducto.Servicio
                10m
            );
            await repo.AddProductoSimpleAsync(producto);

            var useCase = new EliminarProductoSimpleUseCase(repo, uow);
            var dto = new EliminarProductoSimpleDto(producto.ProductoId, "admin");

            // Act
            await useCase.HandleAsync(dto);

            // Assert
            var eliminado = await repo.GetByIdAsync(producto.ProductoId);
            Assert.That(eliminado, Is.Null);
            Assert.That(uow.WasCommitted, Is.True);
        }

        [Test]
        public void HandleAsync_CuandoProductoNoExiste_LanzaExcepcion()
        {
            // Arrange
            var repo = new InMemoryCatalogoArticulosRepository();
            var uow = new InMemoryUnitOfWork();
            var useCase = new EliminarProductoSimpleUseCase(repo, uow);
            var dto = new EliminarProductoSimpleDto(Guid.NewGuid(), "admin");

            // Act & Assert
            Assert.That(
                async () => await useCase.HandleAsync(dto),
                Throws.Exception.With.Message.Contain("no encontrado")
            );
        }
    }
}