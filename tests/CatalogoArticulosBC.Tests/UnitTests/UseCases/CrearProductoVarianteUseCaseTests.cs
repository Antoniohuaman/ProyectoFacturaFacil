using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using CatalogoArticulosBC.Adapters.Output.Persistence.InMemory;
using CatalogoArticulosBC.Application.UseCases;
using CatalogoArticulosBC.Application.DTOs;
using CatalogoArticulosBC.Domain.ValueObjects;
using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.Entities;

namespace CatalogoArticulosBC.Tests.UnitTests.UseCases
{
    public class CrearProductoVarianteUseCaseTests
    {
        [Test]
        public async Task HandleAsync_CuandoPadreExiste_DeberiaCrearVarianteYCometer()
        {
            // Arrange
            var repo = new InMemoryCatalogoArticulosRepository();
            var uow  = new InMemoryUnitOfWork();
            var padre = new ProductoSimple(
                "PADRE01","Padre","Desc",
                new UnidadMedida("UN"),
                new AfectacionIGV("10%"),
                new CodigoSUNAT("2000"),
                new BaseImponibleVentas(40.02m),
                new CentroCosto("CC02"),
                new Presupuesto(200m),
                new Peso(2m),
                TipoProducto.Bien,
                100.50m
            );
            await repo.AddProductoSimpleAsync(padre);
            var useCase = new CrearProductoVarianteUseCase(repo, uow);
            var atributos = new List<KeyValuePair<string,string>>
            {
                new("Color","Rojo"), new("Talla","M")
            };
            var dto = new CrearProductoVarianteDto("PADRE01-ROJO-M", padre.ProductoId, atributos);

            // Act
            var varId = await useCase.HandleAsync(dto);

            // Assert
            var variante = await repo.GetProductoVarianteBySkuAsync(new SKU("PADRE01-ROJO-M"));
            Assert.That(variante, Is.Not.Null);
            Assert.That(uow.WasCommitted, Is.True);
        }

        [Test]
        public void HandleAsync_CuandoPadreNoExiste_DeberiaLanzar()
        {
            // Arrange
            var repo = new InMemoryCatalogoArticulosRepository();
            var uow  = new InMemoryUnitOfWork();
            var useCase = new CrearProductoVarianteUseCase(repo, uow);
            var dto = new CrearProductoVarianteDto("PADREX-ROJO-M", Guid.NewGuid(), new List<KeyValuePair<string,string>>());

            // Act & Assert
            Assert.That(
                async () => await useCase.HandleAsync(dto),
                Throws.InvalidOperationException.With.Message.Contain("no existe")
            );
        }

        [Test]
        public async Task HandleAsync_CuandoPadreInactivo_DeberiaLanzar()
        {
            // Arrange
            var repo = new InMemoryCatalogoArticulosRepository();
            var uow  = new InMemoryUnitOfWork();
            var padre = new ProductoSimple(
                "PADRE02","PadreInactivo","Desc",
                new UnidadMedida("UN"),
                new AfectacionIGV("10%"),
                new CodigoSUNAT("2000"),
                new BaseImponibleVentas(40.02m),
                new CentroCosto("CC02"),
                new Presupuesto(200m),
                new Peso(2m),
                TipoProducto.Bien,
                100.50m
            );
            padre.Deshabilitar("prueba");
            await repo.AddProductoSimpleAsync(padre);
            var useCase = new CrearProductoVarianteUseCase(repo, uow);
            var atributos = new List<KeyValuePair<string,string>>
            {
                new("Color","Azul"), new("Talla","L")
            };
            var dto = new CrearProductoVarianteDto("PADRE02-AZUL-L", padre.ProductoId, atributos);

            // Act & Assert
            Assert.That(
                async () => await useCase.HandleAsync(dto),
                Throws.TypeOf<InvalidStateException>()
            );
        }

        [Test]
        public async Task HandleAsync_CuandoVarianteDuplicada_DeberiaLanzar()
        {
            // Arrange
            var repo = new InMemoryCatalogoArticulosRepository();
            var uow  = new InMemoryUnitOfWork();
            var padre = new ProductoSimple(
                "PADRE03","PadreDup","Desc",
                new UnidadMedida("UN"),
                new AfectacionIGV("10%"),
                new CodigoSUNAT("2000"),
                new BaseImponibleVentas(40.02m),
                new CentroCosto("CC02"),
                new Presupuesto(200m),
                new Peso(2m),
                TipoProducto.Bien,
                100.50m
            );
            await repo.AddProductoSimpleAsync(padre);
            var useCase = new CrearProductoVarianteUseCase(repo, uow);
            var atributos = new List<KeyValuePair<string,string>>
            {
                new("Color","Verde"), new("Talla","S")
            };
            var dto1 = new CrearProductoVarianteDto("PADRE03-VERDE-S", padre.ProductoId, atributos);
            await useCase.HandleAsync(dto1);

            var dto2 = new CrearProductoVarianteDto("PADRE03-VERDE-S2", padre.ProductoId, atributos);

            // Act & Assert
            Assert.That(
                async () => await useCase.HandleAsync(dto2),
                Throws.TypeOf<VarianteDuplicadaException>()
            );
        }
    }
}