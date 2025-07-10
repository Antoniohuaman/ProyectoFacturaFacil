using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using CatalogoArticulosBC.Adapters.Output.Persistence.InMemory;
using CatalogoArticulosBC.Application.UseCases;
using CatalogoArticulosBC.Application.DTOs;
using CatalogoArticulosBC.Domain.ValueObjects;
using CatalogoArticulosBC.Domain.Aggregates;

namespace CatalogoArticulosBC.Tests.UnitTests.UseCases
{
    public class CrearProductoComboUseCaseTests
    {
        [Test]
        public async Task HandleAsync_CuandoComboNuevo_DeberiaCrearComboYCometer()
        {
            // Arrange
            var repo = new InMemoryCatalogoArticulosRepository();
            var uow  = new InMemoryUnitOfWork();

            // Simula un producto simple activo para usar como componente
            var producto = new ProductoSimple(
                sku: "SKU1",
                nombre: "Producto Base",
                descripcion: "Desc",
                unidadMedida: new UnidadMedida("UND"),
                afectacionIgv: new AfectacionIGV("10%"),
                codigoSunat: new CodigoSUNAT("123"),
                cuentaContable: new CuentaContable("456"),
                centroCosto: new CentroCosto("789"),
                presupuesto: new Presupuesto(100m),
                peso: new Peso(1m),
                tipo: "TIPO",
                precio: 10
            );
            await repo.AddProductoSimpleAsync(producto);

            var componenteId = producto.ProductoId;
            var dto = new CrearProductoComboDto(
                skuCombo: "COMBO1",
                nombreCombo: "Combo Test",
                componentes: new List<ComponenteComboDto> { new ComponenteComboDto(componenteId, 2) },
                precioCombo: 100,
                unidadMedida: "UND",
                afectacionIGV: "10",
                estado: "ACTIVO"
            );

            var useCase = new CrearProductoComboUseCase(repo, uow);

            // Act
            var id = await useCase.HandleAsync(dto);

            // Assert: repositorio contiene el combo
            var creado = await repo.GetProductoComboBySkuAsync(new SKU("COMBO1"));
            Assert.That(creado, Is.Not.Null);
            Assert.That(creado!.ProductoComboId, Is.EqualTo(id));
            Assert.That(uow.WasCommitted, Is.True);
            Assert.That(creado.PesoTotal, Is.EqualTo(2)); // 2 * 1 (cantidad * peso del producto base)
        }

        [Test]
        public async Task HandleAsync_CuandoSkuDuplicado_DeberiaLanzar()
        {
            // Arrange
            var repo = new InMemoryCatalogoArticulosRepository();
            var uow  = new InMemoryUnitOfWork();

            // Simula un producto simple activo para usar como componente
            var producto = new ProductoSimple(
                sku: "SKU1",
                nombre: "Producto Base",
                descripcion: "Desc",
                unidadMedida: new UnidadMedida("UND"),
                afectacionIgv: new AfectacionIGV("10"),
                codigoSunat: new CodigoSUNAT("123"),
                cuentaContable: new CuentaContable("456"),
                centroCosto: new CentroCosto("789"),
                presupuesto: new Presupuesto(100m),
                peso: new Peso(1m),
                tipo: "TIPO",
                precio: 10
            );
            await repo.AddProductoSimpleAsync(producto);

            var componenteId = producto.ProductoId;
            var dto = new CrearProductoComboDto(
                skuCombo: "COMBO1",
                nombreCombo: "Combo Test",
                componentes: new List<ComponenteComboDto> { new ComponenteComboDto(componenteId, 2) },
                precioCombo: 100,
                unidadMedida: "UND",
                afectacionIGV: "10",
                estado: "ACTIVO"
            );

            var useCase = new CrearProductoComboUseCase(repo, uow);

            // Crear el primer combo
            await useCase.HandleAsync(dto);

            // Act & Assert: intentar crear otro combo con el mismo SKU debe lanzar excepción
            var ex = Assert.ThrowsAsync<SKUYaExisteException>(async () => await useCase.HandleAsync(dto));
            Assert.That(ex!.Message, Does.Contain("ya existe"));
        }
    }
}