using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CatalogoArticulosBC.Application.UseCases.HabilitarProducto;
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
    public class HabilitarProductoUseCaseTests
    {
        // -------- Helpers coherentes con el dominio --------
        private static Moneda PEN() => Moneda.PEN();
        private static AfectacionImpuesto Afectacion() => AfectacionImpuesto.Gravado_10;
        private static TasaImpuesto IGV18() => TasaImpuesto.IGV18;
        private static UnidadDeMedida Udm() => UnidadDeMedida.From("NIU");
        private static Categoria Cat(string nombre = "BEBIDAS") => new(nombre);
        private static List<EstablecimientoId> Ests() => new() { EstablecimientoId.New() };
        private static NombreProducto Np(string v) => new(v);

        private static ProductoSimple CrearProducto(string sku, string nombre, bool habilitado)
        {
            var empresaId = EmpresaId.From("20111111111");
            var p = new ProductoSimple(
                empresaId: empresaId,
                moneda: PEN(),
                sku: Sku.Crear(sku),
                nombre: Np(nombre),
                unidadMedida: Udm(),
                afectacionImpuesto: Afectacion(),
                tasaImpuesto: IGV18(),
                categoria: Cat(),
                establecimientosAsignados: Ests(),
                descripcion: "desc"
            );

            if (!habilitado)
                p.Deshabilitar("stock cero");

            return p;
        }

        [Test]
        public async Task Habilita_Producto_Inhabilitado_Y_Persisten_Cambios()
        {
            // Arrange
            var repo = new Mock<IProductoRepository>(MockBehavior.Strict);
            var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            tenant.Setup(t => t.EmpresaId).Returns(EmpresaId.From("20111111111"));

            var producto = CrearProducto("SKU-001", "Coca Cola", habilitado: false);

            repo.Setup(r => r.GetByIdAsync(producto.ProductoId))
                .ReturnsAsync(producto);
            repo.Setup(r => r.UpdateAsync(producto))
                .Returns(Task.CompletedTask);
            uow.Setup(u => u.CommitAsync())
               .Returns(Task.CompletedTask);

            var sut = new HabilitarProductoUseCase(repo.Object, uow.Object, tenant.Object);

            // Act
            var result = await sut.ExecuteAsync(new HabilitarProductoInputDto
            {
                ProductoId = producto.ProductoId,
                Usuario = "operador@tienda",
                Motivo = "Se repone stock"
            });

            // Assert
            Assert.That(result.Exitoso, Is.True);
            Assert.That(result.EmpresaId, Is.EqualTo("20111111111"));
            Assert.That(result.ProductoId, Is.EqualTo(producto.ProductoId));
            Assert.That(result.Sku, Is.EqualTo("SKU-001"));
            Assert.That(result.Nombre, Is.EqualTo("Coca Cola"));
            Assert.That(result.Usuario, Is.EqualTo("operador@tienda"));
            Assert.That(result.Motivo, Is.EqualTo("Se repone stock"));
            Assert.That(result.Habilitado, Is.True);
            Assert.That(result.YaEstabaHabilitado, Is.False);

            repo.Verify(r => r.UpdateAsync(producto), Times.Once);
            uow.Verify(u => u.CommitAsync(), Times.Once);
            tenant.VerifyAll();
            repo.VerifyAll();
            uow.VerifyAll();
        }

        [Test]
        public async Task Si_Ya_Estaba_Habilitado_No_Persisten_Cambios_Y_Es_Idempotente()
        {
            // Arrange
            var repo = new Mock<IProductoRepository>(MockBehavior.Strict);
            var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            tenant.Setup(t => t.EmpresaId).Returns(EmpresaId.From("20999999999"));

            var producto = CrearProducto("SKU-999", "Agua Sin Gas", habilitado: true);

            repo.Setup(r => r.GetByIdAsync(producto.ProductoId))
                .ReturnsAsync(producto);

            var sut = new HabilitarProductoUseCase(repo.Object, uow.Object, tenant.Object);

            // Act
            var result = await sut.ExecuteAsync(new HabilitarProductoInputDto
            {
                ProductoId = producto.ProductoId,
                Usuario = "admin",
                Motivo = "Auditoría"
            });

            // Assert
            Assert.That(result.Exitoso, Is.True);
            Assert.That(result.EmpresaId, Is.EqualTo("20999999999"));
            Assert.That(result.ProductoId, Is.EqualTo(producto.ProductoId));
            Assert.That(result.Habilitado, Is.True);
            Assert.That(result.YaEstabaHabilitado, Is.True);

            repo.Verify(r => r.UpdateAsync(It.IsAny<ProductoSimple>()), Times.Never);
            uow.Verify(u => u.CommitAsync(), Times.Never);
            tenant.VerifyAll();
            repo.VerifyAll();
        }

        [Test]
        public void Lanza_NotFound_Si_No_Existe()
        {
            // Arrange
            var repo = new Mock<IProductoRepository>(MockBehavior.Strict);
            var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            var id = Guid.NewGuid();

            repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((ProductoSimple?)null);

            var sut = new HabilitarProductoUseCase(repo.Object, uow.Object, tenant.Object);

            // Act & Assert
            Assert.ThrowsAsync<NotFoundException>(async () =>
                await sut.ExecuteAsync(new HabilitarProductoInputDto
                {
                    ProductoId = id,
                    Usuario = "admin"
                }));

            repo.VerifyAll();
        }

        [Test]
        public void Valida_Entradas_Obligatorias()
        {
            var repo = new Mock<IProductoRepository>(MockBehavior.Strict);
            var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            var sut = new HabilitarProductoUseCase(repo.Object, uow.Object, tenant.Object);

            // ProductoId vacío
            Assert.ThrowsAsync<BusinessRuleException>(async () =>
                await sut.ExecuteAsync(new HabilitarProductoInputDto
                {
                    ProductoId = Guid.Empty,
                    Usuario = "operador"
                }));

            // Usuario vacío
            Assert.ThrowsAsync<BusinessRuleException>(async () =>
                await sut.ExecuteAsync(new HabilitarProductoInputDto
                {
                    ProductoId = Guid.NewGuid(),
                    Usuario = "   "
                }));
        }
    }
}
