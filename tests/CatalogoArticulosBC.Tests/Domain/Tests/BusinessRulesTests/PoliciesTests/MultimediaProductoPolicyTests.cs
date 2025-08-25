using NUnit.Framework;
using CatalogoArticulosBC.Domain.Policies;
using CatalogoArticulosBC.Domain.Aggregates;
// ...existing code...
using CatalogoArticulosBC.Domain.Entities;
using System;
using SharedKernel.ValueObjects;
using CatalogoArticulosBC.Domain.ValueObjects;
using System.Collections.Generic;

namespace CatalogoArticulosBC.Tests.Domain.Tests.BusinessRulesTests.PoliciesTests
{
    [TestFixture]
    public class MultimediaProductoPolicyTests
    {
        [Test]
        public void TieneMultimediaValida_ConMultimedia_ReturnsTrue()
        {
            var producto = new ProductoSimple(
                moneda: Moneda.PEN(),
                sku: Sku.Crear("SKU-006"),
                nombre: new NombreProducto("Producto 6"),
                unidadMedida: UnidadDeMedida.NIU,
                afectacionImpuesto: AfectacionImpuesto.Gravado_10,
                categoria: new Categoria("GRAVADO"),
                almacenesAsignados: new List<Guid> { Guid.NewGuid() }
            );
            var multimedia = new MultimediaProducto(
                multimediaId: Guid.NewGuid(),
                tipoMime: "image/jpeg",
                tipoAdjunto: "ImagenPrincipal",
                nombreArchivo: "img1.jpg",
                ruta: "/imagenes/img1.jpg",
                comentario: "Foto principal",
                tamano: 1024
            );
            producto.AgregarMultimedia(multimedia);
            var policy = new MultimediaProductoPolicy();
            Assert.That(policy.TieneMultimediaValida(producto), Is.True);
        }

        [Test]
        public void TieneMultimediaValida_SinMultimedia_ReturnsFalse()
        {
            var producto = new ProductoSimple(
                moneda: Moneda.PEN(),
                sku: Sku.Crear("SKU-007"),
                nombre: new NombreProducto("Producto 7"),
                unidadMedida: UnidadDeMedida.NIU,
                afectacionImpuesto: AfectacionImpuesto.Gravado_10,
                categoria: new Categoria("GRAVADO"),
                almacenesAsignados: new List<Guid> { Guid.NewGuid() }
            );
            var policy = new MultimediaProductoPolicy();
            Assert.That(policy.TieneMultimediaValida(producto), Is.False);
        }
    }
}
