using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CatalogoArticulosBC.Application.UseCases.ConsultarProducto;
using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.Entities;
using CatalogoArticulosBC.Domain.Repositories;
using CatalogoArticulosBC.Domain.ValueObjects;
using Moq;
using NUnit.Framework;
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;
using CatalogoArticulosBC.Tests.TestUtils;

namespace CatalogoArticulosBC.Tests.Application
{
    [TestFixture]
    public class ConsultarProductoUseCaseTests
    {
        // -------- Helpers de dominio coherentes --------
        private static Moneda PEN() => Moneda.PEN();
        private static AfectacionImpuesto Afectacion() => AfectacionImpuesto.Gravado_10;
        private static TasaImpuesto IGV18() => TasaImpuesto.IGV18;
        private static UnidadDeMedida Udm() => UnidadDeMedida.From("NIU");
        private static Categoria Cat(string nombre = "BEBIDAS") => new(nombre);
        private static List<EstablecimientoId> Ests() => new() { EstablecimientoId.New() };
        private static NombreProducto Np(string v) => new(v);

        private static ProductoSimple CrearProducto(EmpresaId empresaId, string sku, string nombre, bool habilitado = true)
        {
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

        private static List<MultimediaProducto> CrearMultimedia(Guid productoId)
        {
            return new List<MultimediaProducto>
            {
                new MultimediaProducto(Guid.NewGuid(), "image/jpeg", "Imagen", "foto1.jpg", "/p/foto1.jpg", "frontal", 10_240),
                new MultimediaProducto(Guid.NewGuid(), "application/pdf", "FichaTecnica", "ficha.pdf", "/p/ficha.pdf", "v1", 25_600)
            };
        }

        [Test]
        public async Task Consulta_por_Id_y_devuelve_detalle_con_multimedia()
        {
            // Arrange
            var repo = new Mock<IProductoRepository>(MockBehavior.Strict);
            var empresaId = EmpresaId.From("20123456789");
            var tenant = TenantTestHelpers.MockTenant(empresaId);

            var producto = CrearProducto(empresaId, "SKU-001", "Coca Cola");
            var media = CrearMultimedia(producto.ProductoId);

            repo.Setup(r => r.GetByIdAsync(producto.ProductoId, empresaId))
                .ReturnsAsync(producto);
            repo.Setup(r => r.GetMultimediaByProductoIdAsync(producto.ProductoId))
                .ReturnsAsync(media);

            var sut = new ConsultarProductoUseCase(repo.Object, tenant.Object);

            // Act
            var output = await sut.ExecuteAsync(new ConsultarProductoInputDto
            {
                ProductoId = producto.ProductoId,
                IncluirMultimedia = true
            });

            // Assert
            Assert.That(output.EmpresaId, Is.EqualTo("20123456789"));
            Assert.That(output.ProductoId, Is.EqualTo(producto.ProductoId));
            Assert.That(output.Sku, Is.EqualTo("SKU-001"));
            Assert.That(output.Nombre, Is.EqualTo("Coca Cola"));
            Assert.That(output.Habilitado, Is.True);
            Assert.That(output.Multimedia, Has.Count.EqualTo(2));
            Assert.That(output.Multimedia.First().TipoMime, Is.EqualTo("image/jpeg"));

            repo.VerifyAll();
            tenant.VerifyAll();
        }

        [Test]
        public async Task Consulta_por_Sku_y_no_incluye_multimedia_si_flag_es_false()
        {
            // Arrange
            var repo = new Mock<IProductoRepository>(MockBehavior.Strict);
            var empresaId = EmpresaId.From("20987654321");
            var tenant = TenantTestHelpers.MockTenant(empresaId);

            var producto = CrearProducto(empresaId, "SKU-ABC", "Agua Sin Gas");

            repo.Setup(r => r.GetBySkuAsync(It.Is<Sku>(s => s.Valor == "SKU-ABC"), empresaId))
                .ReturnsAsync(producto);

            var sut = new ConsultarProductoUseCase(repo.Object, tenant.Object);

            // Act
            var output = await sut.ExecuteAsync(new ConsultarProductoInputDto
            {
                Sku = "SKU-ABC",
                IncluirMultimedia = false
            });

            // Assert
            Assert.That(output.ProductoId, Is.EqualTo(producto.ProductoId));
            Assert.That(output.Sku, Is.EqualTo("SKU-ABC"));
            Assert.That(output.Nombre, Is.EqualTo("Agua Sin Gas"));
            Assert.That(output.Multimedia, Is.Empty);

            repo.VerifyAll();
            tenant.VerifyAll();
        }

        [Test]
        public async Task Consulta_por_Nombre_devuelve_detalle_basico()
        {
            // Arrange
            var repo = new Mock<IProductoRepository>(MockBehavior.Strict);
            var empresaId = EmpresaId.From("20987654321");
            var tenant = TenantTestHelpers.MockTenant(empresaId);

            var producto = CrearProducto(empresaId, "SKU-NOM", "Sprite");

            repo.Setup(r => r.GetByNombreAsync("Sprite", empresaId))
                .ReturnsAsync(producto);
            repo.Setup(r => r.GetMultimediaByProductoIdAsync(producto.ProductoId))
                .ReturnsAsync(new List<MultimediaProducto>());

            var sut = new ConsultarProductoUseCase(repo.Object, tenant.Object);

            // Act
            var output = await sut.ExecuteAsync(new ConsultarProductoInputDto
            {
                Nombre = "Sprite",
                IncluirMultimedia = true
            });

            // Assert
            Assert.That(output.EmpresaId, Is.EqualTo("20987654321"));
            Assert.That(output.Sku, Is.EqualTo("SKU-NOM"));
            Assert.That(output.Nombre, Is.EqualTo("Sprite"));
            Assert.That(output.Habilitado, Is.True);

            repo.VerifyAll();
            tenant.VerifyAll();
        }

        [Test]
        public void Lanza_BusinessRule_si_no_envia_ningun_identificador()
        {
            var repo = new Mock<IProductoRepository>(MockBehavior.Strict);
            var empresaId = TenantTestHelpers.AnyEmpresaId();
            var tenant = TenantTestHelpers.MockTenant(empresaId);
            var sut = new ConsultarProductoUseCase(repo.Object, tenant.Object);

            Assert.ThrowsAsync<BusinessRuleException>(async () =>
                await sut.ExecuteAsync(new ConsultarProductoInputDto()));
        }

        [Test]
        public void Lanza_NotFound_si_no_existe_el_producto()
        {
            var repo = new Mock<IProductoRepository>(MockBehavior.Strict);
            var empresaId = TenantTestHelpers.AnyEmpresaId();
            var tenant = TenantTestHelpers.MockTenant(empresaId);

            repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), empresaId))
                .ReturnsAsync((ProductoSimple?)null);
            var sut = new ConsultarProductoUseCase(repo.Object, tenant.Object);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await sut.ExecuteAsync(new ConsultarProductoInputDto
                {
                    ProductoId = Guid.NewGuid()
                }));

            repo.VerifyAll();
        }
    }
}
