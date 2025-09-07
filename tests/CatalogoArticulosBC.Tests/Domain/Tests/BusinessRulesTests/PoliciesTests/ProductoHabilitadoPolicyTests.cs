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
    public class ProductoHabilitadoPolicyTests
    {
        [Test]
        public void EstaHabilitado_ProductoHabilitado_ReturnsTrue()
        {
            var producto = new ProductoSimple(
                empresaId: EmpresaId.From("20123456789"),
                moneda: Moneda.PEN(),
                sku: Sku.Crear("SKU-008"),
                nombre: new NombreProducto("Producto 8"),
                unidadMedida: UnidadDeMedida.NIU,
                afectacionImpuesto: AfectacionImpuesto.Gravado_10,
                tasaImpuesto: TasaImpuesto.IGV10,
                categoria: new Categoria("GRAVADO"),
                establecimientosAsignados: new List<EstablecimientoId> { EstablecimientoId.New() }
            );
            var policy = new ProductoHabilitadoPolicy();
            Assert.That(policy.EstaHabilitado(producto), Is.True);
        }

        [Test]
        public void EstaHabilitado_ProductoDeshabilitado_ReturnsFalse()
        {
            var producto = new ProductoSimple(
                empresaId: EmpresaId.From("20123456789"),
                moneda: Moneda.PEN(),
                sku: Sku.Crear("SKU-009"),
                nombre: new NombreProducto("Producto 9"),
                unidadMedida: UnidadDeMedida.NIU,
                afectacionImpuesto: AfectacionImpuesto.Gravado_10,
                tasaImpuesto: TasaImpuesto.IGV10,
                categoria: new Categoria("GRAVADO"),
                establecimientosAsignados: new List<EstablecimientoId> { EstablecimientoId.New() }
            );
            producto.Deshabilitar("Motivo de prueba");
            var policy = new ProductoHabilitadoPolicy();
            Assert.That(policy.EstaHabilitado(producto), Is.False);
        }
    }
}
