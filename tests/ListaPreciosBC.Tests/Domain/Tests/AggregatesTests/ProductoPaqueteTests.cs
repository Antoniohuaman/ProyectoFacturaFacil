using System;
using System.Collections.Generic;
using System.Linq;
using ListaPreciosBC.Domain.Aggregates;
using ListaPreciosBC.Domain.Events;
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
            var empresaId = EmpresaId.From("EMP-TEST");
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
            var empresaId = EmpresaId.From("EMP-TEST");
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
            var empresaId = EmpresaId.From("EMP-TEST");
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
            var empresaId = EmpresaId.From("EMP-TEST");
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

        [Test]
        public void CrearPaquete_DisparaEventoPaqueteCreadoConDatos()
        {
            var empresaId = EmpresaId.From("EMP-TEST");
            var paqueteId = Guid.NewGuid();
            var nombre = NombrePaquete.Crear("Canasta Especial");
            var descuento = PorcentajeDescuentoPaquete.Crear(15m);

            var productos = new List<ProductoPaquete.LineaProductoPaquete>
            {
                ProductoPaquete.CrearLinea(default(ProductoId)!, default(UnidadDeMedida)!, CantidadProductoPaquete.Crear(1), 100m)
            };

            var paquete = ProductoPaquete.Crear(
                empresaId,
                paqueteId,
                nombre,
                descuento,
                "Descripcion",
                productos);

            var evento = paquete.DomainEvents.OfType<PaqueteCreado>().Single();
            Assert.That(evento.EmpresaId, Is.EqualTo(empresaId));
            Assert.That(evento.PaqueteId, Is.EqualTo(paqueteId));
            Assert.That(evento.Nombre, Is.EqualTo(nombre));
            Assert.That(evento.Descripcion, Is.EqualTo("Descripcion"));
            Assert.That(evento.Descuento, Is.EqualTo(descuento));
        }

        [Test]
        public void ActualizarDatos_DisparaEventoPaqueteActualizado()
        {
            var empresaId = EmpresaId.From("EMP-TEST");
            var paquete = ProductoPaquete.Crear(
                empresaId,
                Guid.NewGuid(),
                NombrePaquete.Crear("Inicial"),
                PorcentajeDescuentoPaquete.Crear(5m),
                "Desc",
                new[]
                {
                    ProductoPaquete.CrearLinea(default(ProductoId)!, default(UnidadDeMedida)!, CantidadProductoPaquete.Crear(1), 10m)
                });

            paquete.ClearDomainEvents();

            var nuevoNombre = NombrePaquete.Crear("Actualizado");
            var nuevoDescuento = PorcentajeDescuentoPaquete.Crear(12.5m);
            var nuevosProductos = new[]
            {
                ProductoPaquete.CrearLinea(default(ProductoId)!, default(UnidadDeMedida)!, CantidadProductoPaquete.Crear(3), 8m)
            };

            paquete.ActualizarDatos(nuevoNombre, "Nueva desc", nuevoDescuento, nuevosProductos, DateTime.UtcNow);

            var evento = paquete.DomainEvents.OfType<PaqueteActualizado>().Single();
            Assert.That(evento.EmpresaId, Is.EqualTo(empresaId));
            Assert.That(evento.PaqueteId, Is.EqualTo(paquete.Id));
            Assert.That(evento.Nombre, Is.EqualTo(nuevoNombre));
            Assert.That(evento.Descripcion, Is.EqualTo("Nueva desc"));
            Assert.That(evento.Descuento, Is.EqualTo(nuevoDescuento));
        }

        [Test]
        public void MarcarComoEliminado_DisparaEventoSoloUnaVez()
        {
            var empresaId = EmpresaId.From("EMP-TEST");
            var paquete = ProductoPaquete.Crear(
                empresaId,
                Guid.NewGuid(),
                NombrePaquete.Crear("Inicial"),
                PorcentajeDescuentoPaquete.Crear(5m),
                null,
                new[]
                {
                    ProductoPaquete.CrearLinea(default(ProductoId)!, default(UnidadDeMedida)!, CantidadProductoPaquete.Crear(1), 10m)
                });

            paquete.ClearDomainEvents();

            paquete.MarcarComoEliminado(DateTime.UtcNow);
            paquete.MarcarComoEliminado(DateTime.UtcNow);

            var eventos = paquete.DomainEvents.OfType<PaqueteEliminado>().ToList();
            Assert.That(eventos, Has.Count.EqualTo(1));
            var evento = eventos[0];
            Assert.That(evento.EmpresaId, Is.EqualTo(empresaId));
            Assert.That(evento.PaqueteId, Is.EqualTo(paquete.Id));
            Assert.That(evento.Nombre, Is.EqualTo(paquete.Nombre));
            Assert.That(evento.Descuento, Is.EqualTo(paquete.Descuento));
        }
    }
}
