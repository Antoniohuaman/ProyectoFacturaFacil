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
    public class EditarProductoSimpleUseCaseTests
    {
        [Test]
        public async Task HandleAsync_CuandoDatosValidos_ActualizaProductoYCommit()
        {
            // Arrange
            var repo = new InMemoryCatalogoArticulosRepository();
            var uow = new InMemoryUnitOfWork();

            // Producto inicial
            var producto = new ProductoSimple(
                sku: "SKU123",
                nombre: "Original",
                descripcion: "Desc",
                unidadMedida: new UnidadMedida("UN"),
                afectacionIgv: new AfectacionIGV("10%"),
                codigoSunat: new CodigoSUNAT("1000"),
                baseImponibleVentas: new BaseImponibleVentas(40.01m),
                centroCosto: new CentroCosto("CC01"),
                presupuesto: new Presupuesto(100m),
                peso: new Peso(1.5m),
                tipo: TipoProducto.Bien, // O TipoProducto.Servicio
                precio: 10.0m
            );
            await repo.AddProductoSimpleAsync(producto);

            var useCase = new EditarProductoSimpleUseCase(repo, uow);
            var dto = new EditarProductoSimpleDto
            {
                ProductoId = producto.ProductoId,
                NuevoNombre = "Editado",
                NuevaDescripcion = "Nueva Desc",
                NuevoPrecio = 20.0m,
                UsuarioId = "admin"
            };

            // Act
            await useCase.HandleAsync(dto);

            // Assert
            var actualizado = await repo.GetByIdAsync(producto.ProductoId);
            Assert.That(actualizado, Is.Not.Null);
            Assert.That(actualizado!.Nombre, Is.EqualTo("Editado"));
            Assert.That(actualizado.Descripcion, Is.EqualTo("Nueva Desc"));
            Assert.That(actualizado.Precio, Is.EqualTo(20.0m));
            Assert.That(uow.WasCommitted, Is.True);
        }

        [Test]
        public void HandleAsync_CuandoProductoNoExiste_LanzaExcepcion()
        {
            // Arrange
            var repo = new InMemoryCatalogoArticulosRepository();
            var uow = new InMemoryUnitOfWork();
            var useCase = new EditarProductoSimpleUseCase(repo, uow);

            var dto = new EditarProductoSimpleDto
            {
                ProductoId = Guid.NewGuid(),
                NuevoNombre = "Editado",
                NuevaDescripcion = "Nueva Desc",
                NuevoPrecio = 20.0m,
                UsuarioId = "admin"
            };

            // Act & Assert
            Assert.That(
                async () => await useCase.HandleAsync(dto),
                Throws.Exception.With.Message.Contain("no encontrado")
            );
        }
    }
}