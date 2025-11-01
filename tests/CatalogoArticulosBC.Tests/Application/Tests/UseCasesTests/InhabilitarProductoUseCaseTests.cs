using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CatalogoArticulosBC.Application.UseCases.InhabilitarProducto;
using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.Repositories;
using CatalogoArticulosBC.Domain.ValueObjects;
using Moq;
using NUnit.Framework;
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;
using CatalogoArticulosBC.Tests.TestUtils;

namespace CatalogoArticulosBC.Tests.Application.UseCases
{
    [TestFixture]
    public class InhabilitarProductoUseCaseTests
    {
        // Helpers de VOs coherentes con tu dominio
        private static Moneda PEN() => Moneda.PEN();
        private static AfectacionImpuesto Afectacion() => AfectacionImpuesto.Gravado_10;
        private static TasaImpuesto IGV18() => TasaImpuesto.IGV18;
    private static UnidadDeMedida Udm() => UnidadDeMedida.From("NIU");
        private static List<EstablecimientoId> Ests() => new() { EstablecimientoId.New() };
        private static NombreProducto Np(string v) => new(v);

        private static ProductoSimple CrearProducto(
            string sku,
            string nombre,
            bool habilitado = true)
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
                categoriaId: CategoriaId.New(),
                establecimientosAsignados: Ests(),
                descripcion: "desc"
            );

            if (!habilitado)
                p.Deshabilitar("ya inhabilitado");

            return p;
        }

        [Test]
        public async Task Inhabilita_Producto_Habilitado_Y_Persisten_Cambios()
        {
            // Arrange
            var repo = new Mock<IProductoRepository>(MockBehavior.Strict);
            var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var empresaId = EmpresaId.From("20111111111");
            var tenant = TenantTestHelpers.MockTenant(empresaId);

            var producto = CrearProducto("SKU-ABC", "Coca Cola", habilitado: true);

            repo.Setup(r => r.GetByIdAsync(producto.ProductoId, empresaId))
                .ReturnsAsync(producto);
            repo.Setup(r => r.UpdateAsync(producto))
                .Returns(Task.CompletedTask);
            uow.Setup(u => u.CommitAsync())
               .Returns(Task.CompletedTask);

            var sut = new InhabilitarProductoUseCase(repo.Object, uow.Object, tenant.Object);

            // Act
            var result = await sut.ExecuteAsync(new InhabilitarProductoInputDto
            {
                ProductoId = producto.ProductoId,
                Motivo = "Descontinuado temporalmente"
            });

            // Assert
            Assert.That(result.Exitoso, Is.True);
            Assert.That(result.EmpresaId, Is.EqualTo("20111111111"));
            Assert.That(result.ProductoId, Is.EqualTo(producto.ProductoId));
            Assert.That(result.Sku, Is.EqualTo("SKU-ABC"));
            Assert.That(result.Nombre, Is.EqualTo("Coca Cola"));
            Assert.That(result.Motivo, Is.EqualTo("Descontinuado temporalmente"));
            Assert.That(result.Habilitado, Is.False);
            Assert.That(result.YaEstabaInhabilitado, Is.False);

            repo.Verify(r => r.UpdateAsync(producto), Times.Once);
            uow.Verify(u => u.CommitAsync(), Times.Once);
            tenant.VerifyAll();
            repo.VerifyAll();
            uow.VerifyAll();
        }

        [Test]
        public async Task Si_Ya_Estaba_Inhabilitado_No_Persisten_Cambios_Y_Es_Idempotente()
        {
            // Arrange
            var repo = new Mock<IProductoRepository>(MockBehavior.Strict);
            var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var empresaId = EmpresaId.From("20999999999");
            var tenant = TenantTestHelpers.MockTenant(empresaId);

            var producto = CrearProducto("SKU-XYZ", "Agua Sin Gas", habilitado: false);

            repo.Setup(r => r.GetByIdAsync(producto.ProductoId, empresaId))
                .ReturnsAsync(producto);

            var sut = new InhabilitarProductoUseCase(repo.Object, uow.Object, tenant.Object);

            // Act
            var result = await sut.ExecuteAsync(new InhabilitarProductoInputDto
            {
                ProductoId = producto.ProductoId,
                Motivo = "Sigue sin stock"
            });

            // Assert
            Assert.That(result.Exitoso, Is.True);
            Assert.That(result.EmpresaId, Is.EqualTo("20999999999"));
            Assert.That(result.ProductoId, Is.EqualTo(producto.ProductoId));
            Assert.That(result.Habilitado, Is.False);
            Assert.That(result.YaEstabaInhabilitado, Is.True);

            // No se llama a Update ni a Commit
            repo.Verify(r => r.UpdateAsync(It.IsAny<ProductoSimple>()), Times.Never);
            uow.Verify(u => u.CommitAsync(), Times.Never);
            repo.VerifyAll();
            tenant.VerifyAll();
        }

        [Test]
        public void Lanza_NotFound_Si_No_Existe()
        {
            // Arrange
            var repo = new Mock<IProductoRepository>(MockBehavior.Strict);
            var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var empresaId = TenantTestHelpers.AnyEmpresaId();
            var tenant = TenantTestHelpers.MockTenant(empresaId);

            var id = Guid.NewGuid();

            repo.Setup(r => r.GetByIdAsync(id, empresaId)).ReturnsAsync((ProductoSimple?)null);

            var sut = new InhabilitarProductoUseCase(repo.Object, uow.Object, tenant.Object);

            // Act & Assert
            Assert.ThrowsAsync<NotFoundException>(async () =>
                await sut.ExecuteAsync(new InhabilitarProductoInputDto
                {
                    ProductoId = id,
                    Motivo = "X"
                }));

            repo.VerifyAll();
        }

        [Test]
        public void Valida_Entradas_Obligatorias()
        {
            var repo = new Mock<IProductoRepository>(MockBehavior.Strict);
            var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            var sut = new InhabilitarProductoUseCase(repo.Object, uow.Object, tenant.Object);

            // ProductoId vacío
            Assert.ThrowsAsync<BusinessRuleException>(async () =>
                await sut.ExecuteAsync(new InhabilitarProductoInputDto
                {
                    ProductoId = Guid.Empty,
                    Motivo = "algo"
                }));

            // Motivo vacío
            Assert.ThrowsAsync<BusinessRuleException>(async () =>
                await sut.ExecuteAsync(new InhabilitarProductoInputDto
                {
                    ProductoId = Guid.NewGuid(),
                    Motivo = "   "
                }));
        }
    }
}
