using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using CatalogoArticulosBC.Application.UseCases;
using CatalogoArticulosBC.Application.DTOs;
using CatalogoArticulosBC.Adapters.Output.Persistence.InMemory;
using CatalogoArticulosBC.Domain.ValueObjects;
using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.Entities;

namespace CatalogoArticulosBC.Tests.UnitTests.UseCases
{
    public class CrearProductoComboUseCaseTests
    {
        [Test]
        public async Task HandleAsync_CuandoDatosValidos_DeberiaCrearComboYCometer()
        {
            // Arrange
            var repo = new InMemoryCatalogoArticulosRepository();
            var uow = new InMemoryUnitOfWork();

            // Crear productos simples y variantes activos
            var prod1 = new ProductoSimple(
                "SKU1", "Prod1", "Desc1",
                new UnidadMedida("UN"),
                new AfectacionIGV("10%"),
                new CodigoSUNAT("1000"),
                new BaseImponibleVentas(10m),
                new CentroCosto("CC01"),
                new Presupuesto(100m),
                new Peso(2m),
                TipoProducto.Bien,
                50m
            );
            var prod2 = new ProductoSimple(
                "SKU2", "Prod2", "Desc2",
                new UnidadMedida("UN"),
                new AfectacionIGV("10%"),
                new CodigoSUNAT("1001"),
                new BaseImponibleVentas(20m),
                new CentroCosto("CC02"),
                new Presupuesto(200m),
                new Peso(3m),
                TipoProducto.Bien,
                60m
            );
            await repo.AddProductoSimpleAsync(prod1);
            await repo.AddProductoSimpleAsync(prod2);

            var useCase = new CrearProductoComboUseCase(repo, uow);

            var componentes = new List<ComponenteComboDto>
            {
                new ComponenteComboDto(prod1.ProductoId, 2),
                new ComponenteComboDto(prod2.ProductoId, 1)
            };

            var dto = new CrearProductoComboDto(
                "COMBO-001",
                "Combo Test",
                componentes,
                120m,
                "UN",
                "10%",
                "ACTIVO"
            );

            // Act
            var comboId = await useCase.HandleAsync(dto);

            // Assert
            var combo = await repo.GetProductoComboBySkuAsync(new SKU("COMBO-001"));
            Assert.That(combo, Is.Not.Null);
            Assert.That(combo!.Nombre, Is.EqualTo("Combo Test"));
            Assert.That(combo.Componentes.Count, Is.EqualTo(2));
            Assert.That(combo.PesoTotal, Is.EqualTo(2 * 2m + 1 * 3m)); // 2*prod1.Peso + 1*prod2.Peso
            Assert.That(uow.WasCommitted, Is.True);
        }

        [Test]
        public async Task HandleAsync_CuandoSKUYaExiste_DeberiaLanzar()
        {
            // Arrange
            var repo = new InMemoryCatalogoArticulosRepository();
            var uow = new InMemoryUnitOfWork();

            var prod = new ProductoSimple(
                "SKU1", "Prod1", "Desc1",
                new UnidadMedida("UN"),
                new AfectacionIGV("10%"),
                new CodigoSUNAT("1000"),
                new BaseImponibleVentas(10m),
                new CentroCosto("CC01"),
                new Presupuesto(100m),
                new Peso(1m),
                TipoProducto.Bien,
                50m
            );
            await repo.AddProductoSimpleAsync(prod);

            var comboExistente = new ProductoCombo(
                "COMBO-001",
                "Combo Existente",
                new List<ComponenteCombo> { new ComponenteCombo(prod.ProductoId, 1) },
                50m,
                new UnidadMedida("UN"),
                new AfectacionIGV("10%"),
                1m,
                "ACTIVO"
            );
            await repo.AddProductoComboAsync(comboExistente);

            var useCase = new CrearProductoComboUseCase(repo, uow);

            var dto = new CrearProductoComboDto(
                "COMBO-001",
                "Combo Nuevo",
                new List<ComponenteComboDto> { new ComponenteComboDto(prod.ProductoId, 1) },
                60m,
                "UN",
                "10%",
                "ACTIVO"
            );

            // Act & Assert
            Assert.That(
                async () => await useCase.HandleAsync(dto),
                Throws.TypeOf<SKUYaExisteException>()
            );
        }

        [Test]
        public async Task HandleAsync_CuandoComponenteInactivo_DeberiaLanzar()
        {
            // Arrange
            var repo = new InMemoryCatalogoArticulosRepository();
            var uow = new InMemoryUnitOfWork();

            var prod = new ProductoSimple(
                "SKU1", "Prod1", "Desc1",
                new UnidadMedida("UN"),
                new AfectacionIGV("10%"),
                new CodigoSUNAT("1000"),
                new BaseImponibleVentas(10m),
                new CentroCosto("CC01"),
                new Presupuesto(100m),
                new Peso(1m),
                TipoProducto.Bien,
                50m
            );
            prod.Deshabilitar("test");
            await repo.AddProductoSimpleAsync(prod);

            var useCase = new CrearProductoComboUseCase(repo, uow);

            var dto = new CrearProductoComboDto(
                "COMBO-002",
                "Combo Test",
                new List<ComponenteComboDto> { new ComponenteComboDto(prod.ProductoId, 1) },
                60m,
                "UN",
                "10%",
                "ACTIVO"
            );

            // Act & Assert
            Assert.That(
                async () => await useCase.HandleAsync(dto),
                Throws.TypeOf<ProductoPadreInvalidoException>()
            );
        }

        [Test]
        public void CrearProductoComboDto_CuandoCantidadInvalida_DeberiaLanzar()
        {
            var prodId = Guid.NewGuid();
            Assert.Throws<ArgumentException>(() =>
                new CrearProductoComboDto(
                    "COMBO-003",
                    "Combo Test",
                    new List<ComponenteComboDto> { new ComponenteComboDto(prodId, 0) },
                    60m,
                    "UN",
                    "10%",
                    "ACTIVO"
                )
            );
        }

        [Test]
        public void CrearProductoComboDto_CuandoComponentesDuplicados_DeberiaLanzar()
        {
            var prodId = Guid.NewGuid();
            Assert.Throws<ArgumentException>(() =>
                new CrearProductoComboDto(
                    "COMBO-004",
                    "Combo Test",
                    new List<ComponenteComboDto>
                    {
                        new ComponenteComboDto(prodId, 1),
                        new ComponenteComboDto(prodId, 2)
                    },
                    60m,
                    "UN",
                    "10%",
                    "ACTIVO"
                )
            );
        }
    }
}