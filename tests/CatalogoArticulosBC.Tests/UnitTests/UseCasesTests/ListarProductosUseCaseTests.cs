using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CatalogoArticulosBC.Application.UseCases.ListarProductos;
using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.Filters;
using CatalogoArticulosBC.Domain.Repositories;
using CatalogoArticulosBC.Domain.ValueObjects;
using Moq;
using NUnit.Framework;
using SharedKernel.Application.Interfaces;
using SharedKernel.ValueObjects;

namespace CatalogoArticulosBC.Tests.Application.UseCases
{
    [TestFixture]
    public class ListarProductosUseCaseTests
    {
        // -------- Helpers de dominio coherentes --------
        private static Moneda PEN() => Moneda.PEN();
        private static AfectacionImpuesto Afectacion() => AfectacionImpuesto.Gravado_10;
        private static TasaImpuesto IGV18() => TasaImpuesto.IGV18;
        private static UnidadDeMedida Udm() => UnidadDeMedida.From("NIU");
        private static Categoria Cat(string nombre = "BEBIDAS") => new(nombre);
        private static List<EstablecimientoId> Ests() => new() { EstablecimientoId.New() };
        private static NombreProducto Np(string v) => new(v);

        private static ProductoSimple P(EmpresaId empresaId, string sku, string nombre, string categoria, bool habilitado = true)
        {
            var p = new ProductoSimple(
                empresaId: empresaId,
                moneda: PEN(),
                sku: Sku.Crear(sku),
                nombre: Np(nombre),
                unidadMedida: Udm(),
                afectacionImpuesto: Afectacion(),
                tasaImpuesto: IGV18(),
                categoria: new Categoria(categoria),
                establecimientosAsignados: Ests(),
                descripcion: "desc"
            );
            if (!habilitado) p.Deshabilitar("test");
            return p;
        }

        private static List<ProductoSimple> Seed(EmpresaId empresaId)
        {
            return new List<ProductoSimple>
            {
                P(empresaId, "SKU-003","Cerveza","BEBIDAS", true),
                P(empresaId, "SKU-001","Agua","BEBIDAS", true),
                P(empresaId, "SKU-002","Cola","BEBIDAS", false),
                P(empresaId, "SKU-010","Arroz","ABARROTES", true),
                P(empresaId, "SKU-020","Azúcar","ABARROTES", true),
                P(empresaId, "SKU-021","Aceite","ABARROTES", false),
                P(empresaId, "SKU-100","Café","BEBIDAS", true),
            };
        }

        [Test]
        public async Task Devuelve_pagina_1_orden_por_nombre_asc_por_defecto()
        {
            // Arrange
            var repo = new Mock<IProductoRepository>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            var empresaId = EmpresaId.From("20123456789");
            tenant.Setup(t => t.EmpresaId).Returns(empresaId);

            var data = Seed(empresaId);

            // Simulamos que el repositorio aplica solo los filtros, no el ordenamiento
            repo.Setup(r => r.BuscarPorFiltroAsync(It.IsAny<FiltroProducto>()))
                .ReturnsAsync((FiltroProducto f) =>
                {
                    IEnumerable<ProductoSimple> q = data;
                    if (!string.IsNullOrWhiteSpace(f.Nombre))
                        q = q.Where(p => p.Nombre!.Valor.Contains(f.Nombre!, StringComparison.OrdinalIgnoreCase));
                    if (f.Categoria != null)
                        q = q.Where(p => p.Categoria!.Nombre == f.Categoria.Nombre);
                    if (f.Habilitado.HasValue)
                        q = q.Where(p => p.Habilitado == f.Habilitado.Value);
                    if (f.PrecioMin.HasValue)
                        q = q.Where(p => p.PrecioVenta != null && p.PrecioVenta.Monto >= f.PrecioMin.Value);
                    if (f.PrecioMax.HasValue)
                        q = q.Where(p => p.PrecioVenta != null && p.PrecioVenta.Monto <= f.PrecioMax.Value);
                    return q;
                });

            var sut = new ListarProductosUseCase(repo.Object, tenant.Object);

            // Act
            var output = await sut.ExecuteAsync(new ListarProductosInputDto
            {
                Page = 1,
                PageSize = 2 // para poder verificar paginación
            });

            // Assert (default: ordenar por nombre asc)
            Assert.That(output.EmpresaId, Is.EqualTo("20123456789"));
            Assert.That(output.Page, Is.EqualTo(1));
            Assert.That(output.PageSize, Is.EqualTo(2));
            Assert.That(output.TotalItems, Is.EqualTo(data.Count));
            Assert.That(output.TotalPages, Is.EqualTo((int)Math.Ceiling(data.Count / 2.0)));
            Assert.That(output.OrdenarPor, Is.EqualTo("nombre"));
            Assert.That(output.Direccion, Is.EqualTo("asc"));

            // Nombres en orden ascendente: "Aceite","Agua","Arroz","Azúcar","Café","Cerveza","Cola"
            var expectedFirstTwo = new[] { "Aceite", "Agua" };
            Assert.That(output.Items.Select(i => i.Nombre).ToArray(), Is.EqualTo(expectedFirstTwo));

            repo.VerifyAll();
            tenant.VerifyAll();
        }

        [Test]
        public async Task Filtra_por_nombre_y_categoria_y_estado()
        {
            var repo = new Mock<IProductoRepository>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            var empresaId = EmpresaId.From("20999999999");
            tenant.Setup(t => t.EmpresaId).Returns(empresaId);

            var data = Seed(empresaId);

            repo.Setup(r => r.BuscarPorFiltroAsync(It.IsAny<FiltroProducto>()))
                .ReturnsAsync((FiltroProducto f) =>
                {
                    IEnumerable<ProductoSimple> q = data;
                    if (!string.IsNullOrWhiteSpace(f.Nombre))
                        q = q.Where(p => p.Nombre!.Valor.Contains(f.Nombre!, StringComparison.OrdinalIgnoreCase));
                    if (f.Categoria != null)
                        q = q.Where(p => p.Categoria!.Nombre == f.Categoria.Nombre);
                    if (f.Habilitado.HasValue)
                        q = q.Where(p => p.Habilitado == f.Habilitado.Value);
                    return q;
                });

            var sut = new ListarProductosUseCase(repo.Object, tenant.Object);

            var output = await sut.ExecuteAsync(new ListarProductosInputDto
            {
                Nombre = "a",            // contiene "a"
                Categoria = "BEBIDAS",
                Habilitado = true,
                Page = 1,
                PageSize = 10,
                OrdenarPor = "nombre",
                Direccion = "asc"
            });

            // De seed en BEBIDAS habilitados con "a": Agua, Café, Cerveza (ordenados alfabéticamente)
            var expected = new[] { "Agua", "Café", "Cerveza" };
            Assert.That(output.Items.Select(i => i.Nombre).ToArray(), Is.EqualTo(expected));
            Assert.That(output.TotalItems, Is.EqualTo(expected.Length));
            Assert.That(output.EmpresaId, Is.EqualTo("20999999999"));

            repo.VerifyAll();
            tenant.VerifyAll();
        }

        [Test]
        public async Task Ordena_por_sku_desc_y_pagina_2()
        {
            var repo = new Mock<IProductoRepository>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            var empresaId = EmpresaId.From("20111111111");
            tenant.Setup(t => t.EmpresaId).Returns(empresaId);

            var data = Seed(empresaId);

            repo.Setup(r => r.BuscarPorFiltroAsync(It.IsAny<FiltroProducto>()))
                .ReturnsAsync(data);

            var sut = new ListarProductosUseCase(repo.Object, tenant.Object);

            var output = await sut.ExecuteAsync(new ListarProductosInputDto
            {
                OrdenarPor = "sku",
                Direccion = "desc",
                Page = 2,
                PageSize = 3
            });

            // SKUs desc: 100, 021, 020, 010, 003, 002, 001
            // Page 2 (size 3) -> índices 3..5 -> 010, 003, 002
            var expected = new[] { "SKU-010", "SKU-003", "SKU-002" };
            Assert.That(output.Items.Select(i => i.Sku).ToArray(), Is.EqualTo(expected));
            Assert.That(output.TotalItems, Is.EqualTo(7));
            Assert.That(output.TotalPages, Is.EqualTo(3));
            Assert.That(output.Page, Is.EqualTo(2));
            Assert.That(output.PageSize, Is.EqualTo(3));

            repo.VerifyAll();
            tenant.VerifyAll();
        }

        [Test]
        public async Task Retorna_lista_vacia_y_totales_en_cero_si_no_hay_coincidencias()
        {
            var repo = new Mock<IProductoRepository>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            tenant.Setup(t => t.EmpresaId).Returns(EmpresaId.From("20000000001"));

            repo.Setup(r => r.BuscarPorFiltroAsync(It.IsAny<FiltroProducto>()))
                .ReturnsAsync(Enumerable.Empty<ProductoSimple>());

            var sut = new ListarProductosUseCase(repo.Object, tenant.Object);

            var output = await sut.ExecuteAsync(new ListarProductosInputDto
            {
                Nombre = "NoExiste",
                Page = 1,
                PageSize = 10
            });

            Assert.That(output.Items, Is.Empty);
            Assert.That(output.TotalItems, Is.EqualTo(0));
            Assert.That(output.TotalPages, Is.EqualTo(0));
            Assert.That(output.EmpresaId, Is.EqualTo("20000000001"));

            repo.VerifyAll();
            tenant.VerifyAll();
        }

        [Test]
        public async Task Normaliza_page_y_pageSize_fuera_de_rango()
        {
            var repo = new Mock<IProductoRepository>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            var empresaId = EmpresaId.From("20123456780");
            tenant.Setup(t => t.EmpresaId).Returns(empresaId);

            var data = Seed(empresaId);
            repo.Setup(r => r.BuscarPorFiltroAsync(It.IsAny<FiltroProducto>()))
                .ReturnsAsync(data);

            var sut = new ListarProductosUseCase(repo.Object, tenant.Object);

            var output = await sut.ExecuteAsync(new ListarProductosInputDto
            {
                Page = -5,         // se normaliza a 1
                PageSize = 5000,   // se acota a 200
                OrdenarPor = "categoria",
                Direccion = "asc"
            });

            Assert.That(output.Page, Is.EqualTo(1));
            Assert.That(output.PageSize, Is.EqualTo(200));
            Assert.That(output.TotalItems, Is.EqualTo(data.Count));
            Assert.That(output.Items.Length, Is.EqualTo(Math.Min(200, data.Count)));
        }
    }
}
