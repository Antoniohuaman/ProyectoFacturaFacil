using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CatalogoArticulosBC.Application.UseCases.EliminarProductoSimple;
using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.Repositories;
using CatalogoArticulosBC.Domain.ValueObjects;
using Moq;
using NUnit.Framework;
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace CatalogoArticulosBC.Tests.Application.UseCases
{
    [TestFixture]
    public class EliminarProductoSimpleUseCaseTests
    {
        private static Moneda PEN() => Moneda.PEN();

        private static ProductoSimple CrearProductoBien()
        {
            var est = EstablecimientoId.New();
            return new ProductoSimple(
                empresaId: EmpresaId.From("20123456789"),
                moneda: PEN(),
                sku: Sku.Crear("SKU-BIEN-001"),
                nombre: new NombreProducto("AGUA 625 ML"),
                unidadMedida: UnidadDeMedida.NIU,
                afectacionImpuesto: AfectacionImpuesto.Gravado_10,
                tasaImpuesto: TasaImpuesto.IGV18,
                categoria: new Categoria("BEBIDAS"),
                establecimientosAsignados: new List<EstablecimientoId> { est },
                descripcion: "Producto de prueba",
                marca: new Marca("ACME"),
                precioVenta: new PrecioVenta(2.5m, PEN(), AfectacionImpuesto.Gravado_10, incluyeIGV: true),
                codigoSunat: new CodigoSUNAT("12345678"),
                centroDeCosto: null,
                peso: new Peso(0.6m),
                codigoBarras: new CodigoBarras("5901234123457"),
                codigoFabrica: new CodigoFabrica("FAB-001"),
                tipo: TipoProducto.Bien,
                tipoExistencia: TipoExistencia.Mercaderias,
                asignarATodosLosEstablecimientos: false,
                imagenPrincipalId: null
            );
        }

        private static ProductoSimple CrearProductoServicio()
        {
            var est = EstablecimientoId.New();
            return new ProductoSimple(
                empresaId: EmpresaId.From("20123456789"),
                moneda: PEN(),
                sku: Sku.Crear("SKU-SERV-001"),
                nombre: new NombreProducto("SERVICIO MANTENIMIENTO"),
                unidadMedida: UnidadDeMedida.ZZ, // servicios
                afectacionImpuesto: AfectacionImpuesto.Gravado_10, // el agregado sólo valida 0% o 10/18%, por simplicidad usamos gravado
                tasaImpuesto: TasaImpuesto.IGV10,                  // admite 0.10 o 0.18 si grava; 0% si exonerado
                categoria: new Categoria("SERVICIOS"),
                establecimientosAsignados: new List<EstablecimientoId> { est },
                descripcion: "Servicio de prueba",
                marca: null,
                precioVenta: new PrecioVenta(100m, PEN(), AfectacionImpuesto.Gravado_10, incluyeIGV: true),
                codigoSunat: new CodigoSUNAT("87654321"),
                centroDeCosto: null,
                peso: null,
                codigoBarras: null,
                codigoFabrica: null,
                tipo: TipoProducto.Servicio,
                tipoExistencia: TipoExistencia.Servicios,
                asignarATodosLosEstablecimientos: true,
                imagenPrincipalId: null
            );
        }

        [Test]
        public async Task Eliminar_Bien_Exitoso()
        {
            // Arrange
            var repo = new Mock<IProductoRepository>(MockBehavior.Strict);
            var uow  = new Mock<IUnitOfWork>(MockBehavior.Strict);

            var producto = CrearProductoBien();
            repo.Setup(r => r.GetByIdAsync(producto.ProductoId)).ReturnsAsync(producto);
            repo.Setup(r => r.DeleteAsync(producto)).Returns(Task.CompletedTask);
            uow.Setup(x => x.CommitAsync()).Returns(Task.CompletedTask);

            var sut = new EliminarProductoSimpleUseCase(repo.Object, uow.Object);

            // Act
            var output = await sut.ExecuteAsync(new EliminarProductoSimpleInputDto { ProductoId = producto.ProductoId });

            // Assert
            Assert.That(output.Eliminado, Is.True);
            Assert.That(output.ProductoId, Is.EqualTo(producto.ProductoId));
            Assert.That(output.Sku, Is.EqualTo(producto.Sku.Valor));
            Assert.That(output.Nombre, Is.EqualTo(producto.Nombre.Valor));

            repo.VerifyAll();
            uow.VerifyAll();
        }

        [Test]
        public async Task Eliminar_Servicio_Exitoso()
        {
            // Arrange
            var repo = new Mock<IProductoRepository>(MockBehavior.Strict);
            var uow  = new Mock<IUnitOfWork>(MockBehavior.Strict);

            var producto = CrearProductoServicio();
            repo.Setup(r => r.GetByIdAsync(producto.ProductoId)).ReturnsAsync(producto);
            repo.Setup(r => r.DeleteAsync(producto)).Returns(Task.CompletedTask);
            uow.Setup(x => x.CommitAsync()).Returns(Task.CompletedTask);

            var sut = new EliminarProductoSimpleUseCase(repo.Object, uow.Object);

            // Act
            var output = await sut.ExecuteAsync(new EliminarProductoSimpleInputDto { ProductoId = producto.ProductoId });

            // Assert
            Assert.That(output.Eliminado, Is.True);
            Assert.That(output.ProductoId, Is.EqualTo(producto.ProductoId));
            Assert.That(output.Sku, Is.EqualTo(producto.Sku.Valor));
            Assert.That(output.Nombre, Is.EqualTo(producto.Nombre.Valor));

            repo.VerifyAll();
            uow.VerifyAll();
        }

        [Test]
        public void Eliminar_Debe_Fallar_Si_NoExiste()
        {
            // Arrange
            var repo = new Mock<IProductoRepository>(MockBehavior.Strict);
            var uow  = new Mock<IUnitOfWork>(MockBehavior.Strict);

            var id = Guid.NewGuid();
            repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((ProductoSimple?)null);

            var sut = new EliminarProductoSimpleUseCase(repo.Object, uow.Object);

            // Act + Assert
            Assert.ThrowsAsync<NotFoundException>(async () =>
                await sut.ExecuteAsync(new EliminarProductoSimpleInputDto { ProductoId = id }));

            repo.VerifyAll();
            uow.Verify(x => x.CommitAsync(), Times.Never);
        }

        [Test]
        public void Eliminar_Debe_Fallar_Si_IdVacio()
        {
            var repo = new Mock<IProductoRepository>();
            var uow  = new Mock<IUnitOfWork>();
            var sut = new EliminarProductoSimpleUseCase(repo.Object, uow.Object);

            Assert.ThrowsAsync<ArgumentException>(async () =>
                await sut.ExecuteAsync(new EliminarProductoSimpleInputDto { ProductoId = Guid.Empty }));
        }
    }
}
