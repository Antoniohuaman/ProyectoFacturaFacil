using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CatalogoArticulosBC.Application.UseCases.EditarProductoSimple;
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
    public class EditarProductoSimpleUseCaseTests
    {
        private static Moneda PEN() => Moneda.PEN();

        private static ProductoSimple CrearProductoBaseBien(out EstablecimientoId e1)
        {
            e1 = EstablecimientoId.New();
            var producto = new ProductoSimple(
                empresaId: EmpresaId.From("20123456789"),
                moneda: PEN(),
                sku: Sku.Crear("SKU-BASE-001"),
                nombre: new NombreProducto("AGUA 625 ML"),
                unidadMedida: UnidadDeMedida.NIU,
                afectacionImpuesto: AfectacionImpuesto.Gravado_10,
                tasaImpuesto: TasaImpuesto.IGV18,
                categoria: new Categoria("BEBIDAS"),
                establecimientosAsignados: new List<EstablecimientoId> { e1 },
                descripcion: "Producto base",
                marca: new Marca("ACME"),
                precioVenta: new PrecioVenta(2.5m, PEN(), AfectacionImpuesto.Gravado_10, incluyeIGV: true),
                codigoSunat: new CodigoSUNAT("12345678"),
                centroDeCosto: null,
                peso: new Peso(0.6m),
                codigoBarras: new CodigoBarras("5901234123457"), // EAN-13 válido
                codigoFabrica: new CodigoFabrica("FAB-001"),
                tipo: TipoProducto.Bien,
                tipoExistencia: TipoExistencia.Mercaderias,
                asignarATodosLosEstablecimientos: false,
                imagenPrincipalId: null
            );
            return producto;
        }

        [Test]
        public async Task Editar_Bien_Completo_Y_CambiarSku_Exitoso()
        {
            // Arrange
            var repo = new Mock<IProductoRepository>(MockBehavior.Strict);
            var uow  = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var empresaId = TenantTestHelpers.AnyEmpresaId();
            var ten  = TenantTestHelpers.MockTenant(empresaId);

            var producto = CrearProductoBaseBien(out var est1);
            var prodId = producto.ProductoId;

            var input = new EditarProductoSimpleInputDto
            {
                ProductoId = prodId,
                NuevoSku = "SKU-EDIT-001",
                Nombre = "AGUA MINERAL 625 ML",
                UnidadMedidaCodigo = "NIU",
                AfectacionImpuestoCodigo = "10",   // gravado IGV
                TasaImpuestoPorcentaje = 18m,
                CategoriaNombre = "BEBIDAS ISOTÓNICAS",
                Descripcion = "Nueva presentación",
                MarcaNombre = "ACME PREMIUM",
                PrecioVentaMonto = 3.00m,
                PrecioIncluyeIGV = true,
                CodigoSunat = "87654321",
                CodigoBarras = "5901234123457", // seguimos con uno válido
                CodigoFabrica = "FAB-999",
                CentroDeCostoCodigo = "CC-VENTAS",
                CentroDeCostoNombre = "Ventas Retail",
                PesoKg = 0.62m,
                TipoProducto = TipoProducto.Bien,
                TipoExistencia = TipoExistencia.Mercaderias,
                EstablecimientosAsignados = new List<Guid> { (Guid)est1 }, // conservamos el mismo
                AsignarATodosLosEstablecimientos = false,
                ImagenPrincipalId = null
            };

            repo.Setup(x => x.GetByIdAsync(prodId, empresaId)).ReturnsAsync(producto);
            repo.Setup(x => x.ExisteSkuAsync(It.Is<Sku>(s => s.Valor == "SKU-EDIT-001"), empresaId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            repo.Setup(x => x.UpdateAsync(producto)).Returns(Task.CompletedTask);
            uow.Setup(x => x.CommitAsync()).Returns(Task.CompletedTask);

            var sut = new EditarProductoSimpleUseCase(repo.Object, uow.Object, ten.Object);

            // Act
            var output = await sut.ExecuteAsync(input);

            // Assert
            Assert.That(output.ProductoId, Is.EqualTo(prodId));
            Assert.That(output.Sku, Is.EqualTo("SKU-EDIT-001"));
            Assert.That(output.Nombre, Is.EqualTo("AGUA MINERAL 625 ML"));
            Assert.That(output.Categoria, Is.EqualTo("BEBIDAS ISOTÓNICAS"));
            Assert.That(output.AfectacionImpuestoCodigo, Is.EqualTo("10"));
            Assert.That(output.TasaImpuestoFraccion, Is.EqualTo(0.18m));
            Assert.That(output.PrecioVentaMonto, Is.EqualTo(3.00m));
            Assert.That(output.PrecioIncluyeIGV, Is.True);
            Assert.That(output.MonedaCodigo, Is.EqualTo("PEN"));
            Assert.That(output.TipoExistencia, Is.EqualTo(TipoExistencia.Mercaderias));
            Assert.That(output.EstablecimientosAsignados.Single(), Is.EqualTo((Guid)est1));

            // Eventos: ProductoActualizado y SkuActualizado deberían estar presentes
            Assert.That(producto.DomainEvents.Any(e => e.GetType().Name == "ProductoActualizado"), Is.True);
            Assert.That(producto.DomainEvents.Any(e => e.GetType().Name == "SkuActualizado"), Is.True);

            repo.VerifyAll();
            uow.VerifyAll();
            ten.VerifyAll();
        }

        [Test]
        public async Task Editar_Servicio_Exonerado_SinSkuChange_Exitoso()
        {
            // Arrange
            var repo = new Mock<IProductoRepository>(MockBehavior.Strict);
            var uow  = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var empresaId = TenantTestHelpers.AnyEmpresaId();
            var ten  = TenantTestHelpers.MockTenant(empresaId);

            var producto = CrearProductoBaseBien(out var est1); // partimos del mismo y lo convertimos en servicio
            var prodId = producto.ProductoId;

            var input = new EditarProductoSimpleInputDto
            {
                ProductoId = prodId,
                NuevoSku = null, // no cambiamos
                Nombre = "SERVICIO DE INSTALACIÓN",
                UnidadMedidaCodigo = "ZZ",          // servicio
                AfectacionImpuestoCodigo = "20",    // exonerado
                TasaImpuestoPorcentaje = 0m,        // coherente con exonerado
                CategoriaNombre = "SERVICIOS",
                Descripcion = "Instalación en sitio",
                MarcaNombre = null,
                PrecioVentaMonto = 150m,
                PrecioIncluyeIGV = true,            // no afecta (exonerado)
                CodigoSunat = "12345678",
                CodigoBarras = null,
                CodigoFabrica = null,
                CentroDeCostoCodigo = null,
                CentroDeCostoNombre = null,
                PesoKg = null,
                TipoProducto = TipoProducto.Servicio,
                TipoExistencia = TipoExistencia.Servicios,
                EstablecimientosAsignados = new List<Guid> { (Guid)est1 },
                AsignarATodosLosEstablecimientos = true,
                ImagenPrincipalId = null
            };

            repo.Setup(x => x.GetByIdAsync(prodId, empresaId)).ReturnsAsync(producto);
            // No se consulta ExisteSkuAsync porque no cambia el SKU
            repo.Setup(x => x.UpdateAsync(producto)).Returns(Task.CompletedTask);
            uow.Setup(x => x.CommitAsync()).Returns(Task.CompletedTask);

            var sut = new EditarProductoSimpleUseCase(repo.Object, uow.Object, ten.Object);

            // Act
            var output = await sut.ExecuteAsync(input);

            // Assert
            Assert.That(output.Sku, Is.EqualTo("SKU-BASE-001"));
            Assert.That(output.Nombre, Is.EqualTo("SERVICIO DE INSTALACIÓN"));
            Assert.That(output.AfectacionImpuestoCodigo, Is.EqualTo("20"));
            Assert.That(output.TasaImpuestoFraccion, Is.EqualTo(0m));
            Assert.That(output.TipoProducto, Is.EqualTo(TipoProducto.Servicio));
            Assert.That(output.TipoExistencia, Is.EqualTo(TipoExistencia.Servicios));
            Assert.That(producto.DomainEvents.Any(e => e.GetType().Name == "ProductoActualizado"), Is.True);
            Assert.That(producto.DomainEvents.Any(e => e.GetType().Name == "SkuActualizado"), Is.False);

            repo.VerifyAll();
            uow.VerifyAll();
            // ten.VerifyAll(); // No se accede a EmpresaId en este test
        }

        [Test]
        public void Editar_DebeFallar_SiEstablecimientosVacios()
        {
            var repo = new Mock<IProductoRepository>();
            var uow  = new Mock<IUnitOfWork>();
            var ten  = TenantTestHelpers.MockTenant();

            var producto = CrearProductoBaseBien(out _);
            repo.Setup(x => x.GetByIdAsync(producto.ProductoId, ten.Object.EmpresaId)).ReturnsAsync(producto);

            var sut = new EditarProductoSimpleUseCase(repo.Object, uow.Object, ten.Object);

            var input = new EditarProductoSimpleInputDto
            {
                ProductoId = producto.ProductoId,
                Nombre = "X",
                UnidadMedidaCodigo = "NIU",
                AfectacionImpuestoCodigo = "10",
                TasaImpuestoPorcentaje = 18m,
                CategoriaNombre = "Y",
                EstablecimientosAsignados = new List<Guid>() // vacío
            };

            Assert.ThrowsAsync<ArgumentException>(async () => await sut.ExecuteAsync(input));
        }

        [Test]
        public async Task Editar_DebeFallar_SiSkuDuplicado()
        {
            // Arrange
            var repo = new Mock<IProductoRepository>(MockBehavior.Strict);
            var uow  = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var empresaId = TenantTestHelpers.AnyEmpresaId();
            var ten  = TenantTestHelpers.MockTenant(empresaId);

            var producto = CrearProductoBaseBien(out var est1);
            var prodId = producto.ProductoId;

            var input = new EditarProductoSimpleInputDto
            {
                ProductoId = prodId,
                NuevoSku = "SKU-EXISTENTE",
                Nombre = "AGUA 625 ML",
                UnidadMedidaCodigo = "NIU",
                AfectacionImpuestoCodigo = "10",
                TasaImpuestoPorcentaje = 18m,
                CategoriaNombre = "BEBIDAS",
                EstablecimientosAsignados = new List<Guid> { (Guid)est1 }
            };

            repo.Setup(x => x.GetByIdAsync(prodId, empresaId)).ReturnsAsync(producto);
            repo.Setup(x => x.ExisteSkuAsync(It.Is<Sku>(s => s.Valor == "SKU-EXISTENTE"), empresaId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true); // ya existe

            var sut = new EditarProductoSimpleUseCase(repo.Object, uow.Object, ten.Object);

            // Act + Assert
            Assert.ThrowsAsync<BusinessRuleException>(async () => await sut.ExecuteAsync(input));

            repo.VerifyAll();
            ten.VerifyAll();
        }

        [Test]
        public async Task Editar_DebePropagarError_DeIncoherenciaAfectacion_Tasa()
        {
            var repo = new Mock<IProductoRepository>();
            var uow  = new Mock<IUnitOfWork>();
            var ten  = TenantTestHelpers.MockTenant();

            var producto = CrearProductoBaseBien(out var est1);
            repo.Setup(x => x.GetByIdAsync(producto.ProductoId, ten.Object.EmpresaId)).ReturnsAsync(producto);

            var sut = new EditarProductoSimpleUseCase(repo.Object, uow.Object, ten.Object);

            // Incoherente: gravado "10" pero tasa 0%
            var input = new EditarProductoSimpleInputDto
            {
                ProductoId = producto.ProductoId,
                Nombre = "Z",
                UnidadMedidaCodigo = "NIU",
                AfectacionImpuestoCodigo = "10",
                TasaImpuestoPorcentaje = 0m,
                CategoriaNombre = "CAT",
                EstablecimientosAsignados = new List<Guid> { (Guid)est1 }
            };

            Assert.ThrowsAsync<ArgumentException>(async () => await sut.ExecuteAsync(input));
        }
    }
}
