using System;
using System.Collections.Generic;
using ListaPreciosBC.Domain.Aggregates;
using ListaPreciosBC.Domain.ValueObjects;
using NUnit.Framework;
using SharedKernel.ValueObjects;

namespace ListaPreciosBC.Tests.Domain.Tests.AggregatesTests
{
    [TestFixture]
    public class ProductoPaqueteTests
    {
        [Test]
        public void CrearPaquete_ConProductos_CalculaSubtotalDescuentoYTotalCorrectamente()
        {
            var empresaId = default(EmpresaId)!;
            var paqueteId = Guid.NewGuid();
            var nombre = NombrePaquete.Crear("Canasta Navideña");
            var descuento = PorcentajeDescuentoPaquete.Crear(20m);
            const string descripcion = "Paquete de prueba";

            var productos = new List<ProductoPaquete.LineaProductoPaquete>
            {
                ProductoPaquete.CrearLinea(
                    default(ProductoId)!,
                    default(UnidadDeMedida)!,
                    CantidadProductoPaquete.Crear(1),
                    100m),
                ProductoPaquete.CrearLinea(
                    default(ProductoId)!,
                    default(UnidadDeMedida)!,
                    CantidadProductoPaquete.Crear(10),
                    5m)
            };

            var paquete = ProductoPaquete.Crear(
                empresaId,
                paqueteId,
                nombre,
                descuento,
                descripcion,
                productos);

            Assert.That(paquete.Subtotal, Is.EqualTo(150m));
            Assert.That(paquete.DescuentoMonto, Is.EqualTo(30m));
            Assert.That(paquete.Total, Is.EqualTo(120m));
            Assert.That(paquete.Productos.Count, Is.EqualTo(2));
        }

        [Test]
        public void CrearPaquete_SinProductos_LanzaInvalidOperationException()
        {
            var empresaId = default(EmpresaId)!;
            var paqueteId = Guid.NewGuid();
            var nombre = NombrePaquete.Crear("Canasta Vacía");
            var descuento = PorcentajeDescuentoPaquete.Crear(10m);

            Assert.That(
                () => ProductoPaquete.Crear(
                    empresaId,
                    paqueteId,
                    nombre,
                    descuento,
                    descripcion: null,
                    productos: Array.Empty<ProductoPaquete.LineaProductoPaquete>()),
                Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void ReemplazarProductos_ConNuevaLista_ActualizaTotales()
        {
            var empresaId = default(EmpresaId)!;
            var paqueteId = Guid.NewGuid();
            var nombre = NombrePaquete.Crear("Canasta");
            var descuento = PorcentajeDescuentoPaquete.Crear(0m);

            var productosIniciales = new[]
            {
                ProductoPaquete.CrearLinea(
                    default(ProductoId)!,
                    default(UnidadDeMedida)!,
                    CantidadProductoPaquete.Crear(1),
                    50m)
            };

            var paquete = ProductoPaquete.Crear(
                empresaId,
                paqueteId,
                nombre,
                descuento,
                descripcion: null,
                productos: productosIniciales);

            var nuevosProductos = new[]
            {
                ProductoPaquete.CrearLinea(
                    default(ProductoId)!,
                    default(UnidadDeMedida)!,
                    CantidadProductoPaquete.Crear(2),
                    10m)
            };

            paquete.ReemplazarProductos(nuevosProductos);

            Assert.That(paquete.Subtotal, Is.EqualTo(20m));
            Assert.That(paquete.Productos.Count, Is.EqualTo(1));
        }

        [Test]
        public void ReemplazarProductos_ConListaVacia_LanzaInvalidOperationException()
        {
            var empresaId = default(EmpresaId)!;
            var paqueteId = Guid.NewGuid();
            var nombre = NombrePaquete.Crear("Canasta");
            var descuento = PorcentajeDescuentoPaquete.Crear(5m);

            var productosIniciales = new[]
            {
                ProductoPaquete.CrearLinea(
                    default(ProductoId)!,
                    default(UnidadDeMedida)!,
                    CantidadProductoPaquete.Crear(1),
                    10m)
            };

            var paquete = ProductoPaquete.Crear(
                empresaId,
                paqueteId,
                nombre,
                descuento,
                descripcion: null,
                productos: productosIniciales);

            Assert.That(
                () => paquete.ReemplazarProductos(Array.Empty<ProductoPaquete.LineaProductoPaquete>()),
                Throws.TypeOf<InvalidOperationException>());
        }
    }
}
