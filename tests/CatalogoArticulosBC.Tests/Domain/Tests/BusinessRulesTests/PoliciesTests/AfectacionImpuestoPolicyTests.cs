using NUnit.Framework;
using CatalogoArticulosBC.Domain.Policies;
using CatalogoArticulosBC.Domain.Aggregates;
// ...existing code...
using SharedKernel.ValueObjects;
using CatalogoArticulosBC.Domain.ValueObjects;
using System;
using System.Collections.Generic;

namespace CatalogoArticulosBC.Tests.Domain.Tests.BusinessRulesTests.PoliciesTests
{
    [TestFixture]
    public class AfectacionImpuestoPolicyTests
    {
        [Test]
        public void EsAfectadoPorImpuesto_CategoriaGravado_ReturnsTrue()
        {
            var producto = new ProductoSimple(
                empresaId: EmpresaId.From("20123456789"),
                moneda: Moneda.PEN(),
                sku: Sku.Crear("SKU-001"),
                nombre: new NombreProducto("Producto 1"),
                unidadMedida: UnidadDeMedida.NIU,
                afectacionImpuesto: AfectacionImpuesto.Gravado_10,
                tasaImpuesto: TasaImpuesto.IGV10,
                categoria: new Categoria("GRAVADO"),
                establecimientosAsignados: new List<EstablecimientoId> { EstablecimientoId.New() }
            );
            var policy = new AfectacionImpuestoPolicy();
            Assert.That(policy.EsAfectadoPorImpuesto(producto), Is.True);
        }

        [Test]
        public void EsAfectadoPorImpuesto_CategoriaNoGravado_ReturnsFalse()
        {
            var producto = new ProductoSimple(
                empresaId: EmpresaId.From("20123456789"),
                moneda: Moneda.PEN(),
                sku: Sku.Crear("SKU-002"),
                nombre: new NombreProducto("Producto 2"),
                unidadMedida: UnidadDeMedida.NIU,
                afectacionImpuesto: AfectacionImpuesto.Gravado_10,
                tasaImpuesto: TasaImpuesto.IGV10,
                categoria: new Categoria("EXONERADO"),
                establecimientosAsignados: new List<EstablecimientoId> { EstablecimientoId.New() }
            );
            var policy = new AfectacionImpuestoPolicy();
            Assert.That(policy.EsAfectadoPorImpuesto(producto), Is.False);
        }
    }
}
