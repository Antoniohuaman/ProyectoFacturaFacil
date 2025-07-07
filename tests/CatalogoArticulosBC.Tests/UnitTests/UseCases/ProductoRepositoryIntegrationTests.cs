// tests/CatalogoArticulosBC.Tests/IntegrationTests/ProductoRepositoryIntegrationTests.cs
using System.Threading.Tasks;
using NUnit.Framework;
using CatalogoArticulosBC.Domain.ValueObjects;
using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Adapters.Output.Persistence.InMemory;

namespace CatalogoArticulosBC.Tests.IntegrationTests
{
    public class ProductoRepositoryIntegrationTests
    {
        [Test]
        public async Task InMemoryRepository_AddAndGetBySku()
        {
            // Arrange
            var repo = new InMemoryCatalogoArticulosRepository();
            var simple = new ProductoSimple(
                "INT01","Int","Desc",
                new UnidadMedida("UN"), new AfectacionIGV("10%"),
                new CodigoSUNAT("3000"), new CuentaContable("40.03"),
                new CentroCosto("CC03"), new Presupuesto(300m), new Peso(3m), "SERVICIO"
            );

            // Act
            await repo.AddProductoSimpleAsync(simple);
            var obtenido = await repo.GetProductoSimpleBySkuAsync(new SKU("INT01"));

            // Assert
            Assert.That(obtenido, Is.Not.Null);
            Assert.That(obtenido.ProductoId, Is.EqualTo(simple.ProductoId));
        }
    }
}
