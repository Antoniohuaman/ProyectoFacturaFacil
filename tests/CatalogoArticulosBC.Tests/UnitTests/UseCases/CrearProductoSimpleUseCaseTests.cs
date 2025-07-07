// tests/CatalogoArticulosBC.Tests/UnitTests/UseCases/CrearProductoSimpleUseCaseTests.cs
using System;
using System.Threading.Tasks;
using NUnit.Framework;
using CatalogoArticulosBC.Adapters.Output.Persistence.InMemory;
using CatalogoArticulosBC.Application.UseCases;
using CatalogoArticulosBC.Application.DTOs;
using CatalogoArticulosBC.Domain.ValueObjects;
using CatalogoArticulosBC.Domain.Aggregates;

namespace CatalogoArticulosBC.Tests.UnitTests.UseCases
{
    public class CrearProductoSimpleUseCaseTests
    {
        [Test]
        public async Task HandleAsync_CuandoSkuNuevo_DeberiaCrearProductoYCometer()
        {
            // Arrange
            var repo = new InMemoryCatalogoArticulosRepository();
            var uow  = new InMemoryUnitOfWork();
            var useCase = new CrearProductoSimpleUseCase(repo, uow);
            var dto = new CrearProductoSimpleDto(
                sku: "SKU123",
                nombre: "Producto",
                descripcion: "Desc",
                unidadMedida: "UN",
                afectacionIgv: "10%",
                codigoSunat: "1000",
                cuentaContable: "40.01",
                centroCosto: "CC01",
                presupuesto: 100m,
                peso: 1.5m,
                tipo: "SERVICIO" // <-- agrega este argumento(incorporado)
            );

            // Act
            var id = await useCase.HandleAsync(dto);

            // Assert: repositorio contiene el producto
            var creado = await repo.GetProductoSimpleBySkuAsync(new SKU("SKU123"));
            Assert.That(creado, Is.Not.Null);
            Assert.That(creado!.ProductoId, Is.EqualTo(id));
            Assert.That(uow.WasCommitted, Is.True);
        }

        [Test]
        public void HandleAsync_CuandoSkuDuplicado_DeberiaLanzar()
        {
            // Arrange
            var repo = new InMemoryCatalogoArticulosRepository();
            var uow  = new InMemoryUnitOfWork();
            var dto  = new CrearProductoSimpleDto(
                sku: "SKU123",
                nombre: "Prod",
                descripcion: "Desc",
                unidadMedida: "UN",
                afectacionIgv: "10%",
                codigoSunat: "1000",
                cuentaContable: "40.01",
                centroCosto: "CC01",
                presupuesto: 50m,
                peso: 0.5m,
                tipo: "SERVICIO" // <-- agrega este argumento(incorporado)
            );
            // crear primero
            var initUse = new CrearProductoSimpleUseCase(repo, uow);
            Assert.DoesNotThrowAsync(async () => await initUse.HandleAsync(dto));

            var useCase = new CrearProductoSimpleUseCase(repo, uow);

            // Act & Assert
            Assert.That(
                async () => await useCase.HandleAsync(dto),
                Throws.InvalidOperationException.With.Message.Contain("ya existe")
            );
        }
    }
}