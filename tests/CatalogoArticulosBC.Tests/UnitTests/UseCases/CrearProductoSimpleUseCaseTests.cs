using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using CatalogoArticulosBC.Application.UseCases;
using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.Events;
using CatalogoArticulosBC.Domain.Repositories;
using CatalogoArticulosBC.Domain.Specifications;
using CatalogoArticulosBC.Domain.ValueObjects;

namespace CatalogoArticulosBC.Application.Tests.UseCases
{
    [TestFixture]
    public class CrearProductoSimpleUseCaseTests
    {
        private CrearProductoSimpleUseCase _useCase;
        private FakeProductoRepository _repository;
        private FakeSkuValidator _skuValidator;
        private FakeEventBus _eventBus;

        [SetUp]
        public void SetUp()
        {
            _repository = new FakeProductoRepository();
            _skuValidator = new FakeSkuValidator(isUnique: true);
            _eventBus = new FakeEventBus();
            _useCase = new CrearProductoSimpleUseCase(_repository, _skuValidator, _eventBus);
        }

        [Test]
        public async Task Handle_ValidDto_CreatesProductAndPublishesEvent()
        {
            // Arrange
            var dto = new CrearProductoSimpleDto
            {
                Sku = "SKU123",
                Nombre = "Producto de prueba",
                UnidadMedida = "Unidad",
                AfectacionIgvCodigo = "10",
                Categoria = "General",
                AlmacenesAsignados = new List<Guid> { Guid.NewGuid() }
            };

            // Act
            var newId = await _useCase.Handle(dto);

            // Assert repository add
            Assert.That(_repository.LastAdded, Is.Not.Null);
            Assert.That(_repository.LastAdded.ProductoId, Is.EqualTo(newId));

            // Assert event published
            Assert.That(_eventBus.PublishedEvents, Has.Exactly(1).InstanceOf<ProductoCreado>());
        }

        [Test]
        public void Handle_EmptyAlmacenes_ThrowsArgumentException()
        {
            // Arrange
            var dto = new CrearProductoSimpleDto
            {
                Sku = "SKU123",
                Nombre = "Producto sin almacenes",
                UnidadMedida = "Unidad",
                AfectacionIgvCodigo = "10",
                Categoria = "General",
                AlmacenesAsignados = new List<Guid>()
            };

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(() => _useCase.Handle(dto));
            Assert.That(ex.Message, Does.Contain("Debe asignar al menos un almacén"));
        }

        [Test]
        public void Handle_DuplicateSku_ThrowsArgumentException()
        {
            // Arrange
            _skuValidator.IsUnique = false;
            var dto = new CrearProductoSimpleDto
            {
                Sku = "DUPSKU",
                Nombre = "Producto duplicado",
                UnidadMedida = "Unidad",
                AfectacionIgvCodigo = "10",
                Categoria = "General",
                AlmacenesAsignados = new List<Guid> { Guid.NewGuid() }
            };

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(() => _useCase.Handle(dto));
            Assert.That(ex.Message, Does.Contain("ya existe en el catálogo"));
        }

        // Fakes and stubs
        private class FakeProductoRepository : IProductoRepository
        {
            public ProductoSimple? LastAdded { get; private set; }
            public Task AddAsync(ProductoSimple producto)
            {
                LastAdded = producto;
                return Task.CompletedTask;
            }
            public Task DeleteAsync(ProductoSimple producto) => throw new NotImplementedException();
            public Task<IReadOnlyCollection<ProductoSimple>> GetAllAsync() => throw new NotImplementedException();
            public Task<ProductoSimple?> GetByIdAsync(Guid id) => throw new NotImplementedException();
            public Task<ProductoSimple?> GetBySkuAsync(SKU sku) => throw new NotImplementedException();
            public Task UpdateAsync(ProductoSimple producto) => throw new NotImplementedException();
        }

        private class FakeSkuValidator : IValidadorUnicidadSku
        {
            public bool IsUnique { get; set; }
            public FakeSkuValidator(bool isUnique) => IsUnique = isUnique;
            public bool EsUnico(string sku) => IsUnique;
        }

        private class FakeEventBus : IEventBus
        {
            public List<IDomainEvent> PublishedEvents { get; } = new List<IDomainEvent>();
            public Task Publish(IDomainEvent domainEvent)
            {
                PublishedEvents.Add(domainEvent);
                return Task.CompletedTask;
            }
        }
    }
}