using NUnit.Framework;
using CatalogoArticulosBC.Domain.Policies;
using CatalogoArticulosBC.Domain.Aggregates;
// ...existing code...
using System;
using SharedKernel.ValueObjects;
using CatalogoArticulosBC.Domain.ValueObjects;
using System.Collections.Generic;

namespace CatalogoArticulosBC.Tests.Domain.Tests.BusinessRulesTests.PoliciesTests
{
    [TestFixture]
    public class CodigoBarrasUnicoPolicyTests
    {
        [Test]
        public void EsCodigoUnico_CodigoNoExiste_ReturnsTrue()
        {
            var producto = new ProductoSimple(
                empresaId: EmpresaId.From("20123456789"),
                moneda: Moneda.PEN(),
                sku: Sku.Crear("SKU-003"),
                nombre: new NombreProducto("Producto 3"),
                unidadMedida: UnidadDeMedida.NIU,
                afectacionImpuesto: AfectacionImpuesto.Gravado_10,
                tasaImpuesto: TasaImpuesto.IGV10,
                categoriaId: CategoriaId.New(),
                establecimientosAsignados: new List<EstablecimientoId> { EstablecimientoId.New() },
                codigoBarras: new CodigoBarras("1234567890128")
            );
            var existentes = new List<ProductoSimple>();
            var policy = new CodigoBarrasUnicoPolicy();
            Assert.That(policy.EsCodigoUnico(producto, existentes), Is.True);
        }

        [Test]
        public void EsCodigoUnico_CodigoYaExiste_ReturnsFalse()
        {
            var codigoValido = "1234567890128";
            var codigoBarras = new CodigoBarras(codigoValido);
            var productoExistente = new ProductoSimple(
                empresaId: EmpresaId.From("20123456789"),
                moneda: Moneda.PEN(),
                sku: Sku.Crear("SKU-004"),
                nombre: new NombreProducto("Producto 4"),
                unidadMedida: UnidadDeMedida.NIU,
                afectacionImpuesto: AfectacionImpuesto.Gravado_10,
                tasaImpuesto: TasaImpuesto.IGV10,
                categoriaId: CategoriaId.New(),
                establecimientosAsignados: new List<EstablecimientoId> { EstablecimientoId.New() },
                codigoBarras: codigoBarras
            );
            var producto = new ProductoSimple(
                empresaId: EmpresaId.From("20123456789"),
                moneda: Moneda.PEN(),
                sku: Sku.Crear("SKU-005"),
                nombre: new NombreProducto("Producto 5"),
                unidadMedida: UnidadDeMedida.NIU,
                afectacionImpuesto: AfectacionImpuesto.Gravado_10,
                tasaImpuesto: TasaImpuesto.IGV10,
                categoriaId: CategoriaId.New(),
                establecimientosAsignados: new List<EstablecimientoId> { EstablecimientoId.New() },
                codigoBarras: codigoBarras
            );
            // Aserción extra para depuración
            TestContext.WriteLine($"Existente: {productoExistente.CodigoBarras.Valor}, Nuevo: {producto.CodigoBarras.Valor}");
            Assert.That(producto.CodigoBarras, Is.EqualTo(productoExistente.CodigoBarras));
            var existentes = new List<ProductoSimple> { productoExistente };
            var policy = new CodigoBarrasUnicoPolicy();
            Assert.That(policy.EsCodigoUnico(producto, existentes), Is.False);
        }
    }
}
