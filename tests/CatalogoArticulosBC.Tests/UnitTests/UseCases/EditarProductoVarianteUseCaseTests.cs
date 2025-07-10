using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using CatalogoArticulosBC.Application.UseCases;
using CatalogoArticulosBC.Application.DTOs;
using CatalogoArticulosBC.Adapters.Output.Persistence.InMemory;
using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.ValueObjects;
using CatalogoArticulosBC.Domain.Entities;

namespace CatalogoArticulosBC.Tests.UnitTests.UseCases
{
    public class EditarProductoVarianteUseCaseTests
    {
        [Test]
        public async Task HandleAsync_CuandoVarianteExiste_ActualizaDatosYCommit()
        {
            // Arrange
            var repo = new InMemoryCatalogoArticulosRepository();
            var uow = new InMemoryUnitOfWork();
            var atributosOriginales = new List<AtributoVariante>
            {
                new AtributoVariante("Color", "Rojo"),
                new AtributoVariante("Talla", "M")
            };
            var variante = new ProductoVariante(Guid.NewGuid(), "VAR-001", atributosOriginales);
            await repo.AddProductoVarianteAsync(variante);

            var useCase = new EditarProductoVarianteUseCase(repo, uow);
            var nuevosAtributos = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Color", "Azul"),
                new KeyValuePair<string, string>("Talla", "L")
            };
            var dto = new EditarProductoVarianteDto(variante.ProductoVarianteId, "VAR-002", nuevosAtributos);

            // Act
            await useCase.HandleAsync(dto);

            // Assert
            var actualizado = await repo.GetProductoVarianteByIdAsync(variante.ProductoVarianteId);
            Assert.That(actualizado, Is.Not.Null);
            Assert.That(actualizado.Sku.Value, Is.EqualTo("VAR-002"));
            Assert.That(actualizado.Atributos.Select(a => a.Nombre), Is.EquivalentTo(new[] { "Color", "Talla" }));
            Assert.That(actualizado.Atributos.Select(a => a.Valor), Is.EquivalentTo(new[] { "Azul", "L" }));
            Assert.That(uow.WasCommitted, Is.True);
        }

        [Test]
        public void HandleAsync_CuandoVarianteNoExiste_LanzaExcepcion()
        {
            // Arrange
            var repo = new InMemoryCatalogoArticulosRepository();
            var uow = new InMemoryUnitOfWork();
            var useCase = new EditarProductoVarianteUseCase(repo, uow);
            var dto = new EditarProductoVarianteDto(Guid.NewGuid(), "VAR-003", new List<KeyValuePair<string, string>>());

            // Act & Assert
            Assert.That(
                async () => await useCase.HandleAsync(dto),
                Throws.Exception.With.Message.Contain("no encontrado")
            );
        }
    }
}