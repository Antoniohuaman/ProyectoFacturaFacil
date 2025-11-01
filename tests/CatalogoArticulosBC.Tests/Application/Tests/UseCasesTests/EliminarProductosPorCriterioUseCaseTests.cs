using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CatalogoArticulosBC.Application.UseCases.EliminarProductosPorCriterio;
using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.Filters;
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
    public class EliminarProductosPorCriterioUseCaseTests
    {
        private static Moneda PEN() => Moneda.PEN();
        private static AfectacionImpuesto AIGV() => AfectacionImpuesto.Gravado_10;
        private static TasaImpuesto TIGV18() => TasaImpuesto.IGV18;
    private static UnidadDeMedida UDM() => UnidadDeMedida.From("NIU");
        private static List<EstablecimientoId> ESTS() => new() { EstablecimientoId.New() };
        private static NombreProducto NP(string v) => new(v);

        private static ProductoSimple CrearProducto(
            string skuCode,
            string nombre,
            CategoriaId categoriaId,
            bool habilitado,
            decimal? precio = null,
            string? categoriaNombreSnapshot = null)
        {
            var precioVO = precio.HasValue ? new PrecioVenta(precio.Value, PEN(), AIGV(), incluyeIGV: true) : null;
            var empresaId = EmpresaId.From("20123456789");
            var p = new ProductoSimple(
                empresaId: empresaId,
                moneda: PEN(),
                sku: Sku.Crear(skuCode),
                nombre: NP(nombre),
                unidadMedida: UDM(),
                afectacionImpuesto: AIGV(),
                tasaImpuesto: TIGV18(),
                categoriaId: categoriaId,
                establecimientosAsignados: ESTS(),
                descripcion: "d",
                marca: null,
                precioVenta: precioVO,
                    codigoSunat: null,
                centroDeCosto: null,
                peso: null,
                codigoBarras: null,
                codigoFabrica: null,
                tipo: TipoProducto.Bien,
                tipoExistencia: TipoExistencia.Mercaderias
            );
            p.AsignarCategoria(categoriaId, nombreSnapshot: categoriaNombreSnapshot);
            if (!habilitado)
                p.Deshabilitar("test");
            return p;
        }

        [Test]
        public async Task Elimina_Todos_Los_Que_Coinciden_Con_El_Criterio()
        {
            // Arrange
            var repo = new Mock<IProductoRepository>(MockBehavior.Strict);
            var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            tenant.Setup(t => t.EmpresaId).Returns(EmpresaId.From("20123456789"));

            // 3 productos: 2 coinciden con filtro (nombre contiene "COLA", cat "GASEOSAS" y precio entre 3 y 6)
            var catGaseosas = CategoriaId.New();
            var catBebidas = CategoriaId.New();
            var p1 = CrearProducto("SKU-001", "COLA 500", catGaseosas, true, 3.50m, "GASEOSAS");
            var p2 = CrearProducto("SKU-002", "COLA ZERO", catGaseosas, true, 5.00m, "GASEOSAS");
            var p3 = CrearProducto("SKU-003", "AGUA 700", catBebidas, true, 2.00m, "BEBIDAS");

            // Setup búsqueda
            repo
                .Setup(r => r.BuscarPorFiltroAsync(It.Is<FiltroProducto>(f =>
                    f.Nombre == "COLA"
                    && f.CategoriaId != null && f.CategoriaId == catGaseosas
                    && f.Habilitado == null
                    && f.PrecioMin == 3m
                    && f.PrecioMax == 6m)))
                .ReturnsAsync(new[] { p1, p2 });

            // Eliminaciones
            repo.Setup(r => r.DeleteAsync(p1)).Returns(Task.CompletedTask);
            repo.Setup(r => r.DeleteAsync(p2)).Returns(Task.CompletedTask);

            uow.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);

            var sut = new EliminarProductosPorCriterioUseCase(repo.Object, uow.Object, tenant.Object);

            // Act
            var result = await sut.ExecuteAsync(new EliminarProductosPorCriterioInputDto
            {
                Confirmar = true,
                NombreContiene = "COLA",
                CategoriaId = catGaseosas.ToString(),
                PrecioMin = 3m,
                PrecioMax = 6m
            });

            // Assert
            Assert.That(result.Exitoso, Is.True);
            Assert.That(result.EmpresaId, Is.EqualTo("20123456789"));
            Assert.That(result.CantidadCoincidente, Is.EqualTo(2));
            Assert.That(result.CantidadEliminada, Is.EqualTo(2));
            Assert.That(result.IdsEliminados, Is.EquivalentTo(new[] { p1.ProductoId, p2.ProductoId }));
            Assert.That(result.Criterio.NombreContiene, Is.EqualTo("COLA"));
            Assert.That(result.Criterio.CategoriaId, Is.EqualTo(catGaseosas.ToString()));
            Assert.That(result.Criterio.PrecioMin, Is.EqualTo(3m));
            Assert.That(result.Criterio.PrecioMax, Is.EqualTo(6m));

            repo.VerifyAll();
            uow.VerifyAll();
            tenant.VerifyAll();
        }

        [Test]
        public void Falla_Si_No_Confirma()
        {
            var repo = new Mock<IProductoRepository>(MockBehavior.Strict);
            var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            var sut = new EliminarProductosPorCriterioUseCase(repo.Object, uow.Object, tenant.Object);

            Assert.ThrowsAsync<BusinessRuleException>(async () =>
                await sut.ExecuteAsync(new EliminarProductosPorCriterioInputDto
                {
                    Confirmar = false,
                    NombreContiene = "X"
                }));
        }

        [Test]
        public void Falla_Si_No_Hay_Criterios()
        {
            var repo = new Mock<IProductoRepository>(MockBehavior.Strict);
            var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            var sut = new EliminarProductosPorCriterioUseCase(repo.Object, uow.Object, tenant.Object);

            Assert.ThrowsAsync<BusinessRuleException>(async () =>
                await sut.ExecuteAsync(new EliminarProductosPorCriterioInputDto
                {
                    Confirmar = true
                    // Sin criterios
                }));
        }

        [Test]
        public void Falla_Si_Rango_De_Precios_Invalido()
        {
            var repo = new Mock<IProductoRepository>(MockBehavior.Strict);
            var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            var sut = new EliminarProductosPorCriterioUseCase(repo.Object, uow.Object, tenant.Object);

            Assert.ThrowsAsync<BusinessRuleException>(async () =>
                await sut.ExecuteAsync(new EliminarProductosPorCriterioInputDto
                {
                    Confirmar = true,
                    PrecioMin = 10m,
                    PrecioMax = 5m
                }));
        }

        [Test]
        public async Task Sin_Coincidencias_No_Lanza_Error_Pero_Commit_Y_Devuelve_Contadores_En_Cero()
        {
            var repo = new Mock<IProductoRepository>(MockBehavior.Strict);
            var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            tenant.Setup(t => t.EmpresaId).Returns(EmpresaId.From("20123456789"));

            repo
                .Setup(r => r.BuscarPorFiltroAsync(It.Is<FiltroProducto>(f =>
                    f.Nombre == "NO-EXISTE" &&
                    f.CategoriaId == null &&
                    f.Habilitado == true &&
                    f.PrecioMin == null &&
                    f.PrecioMax == null)))
                .ReturnsAsync(Array.Empty<ProductoSimple>());

            uow.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);

            var sut = new EliminarProductosPorCriterioUseCase(repo.Object, uow.Object, tenant.Object);

            var result = await sut.ExecuteAsync(new EliminarProductosPorCriterioInputDto
            {
                Confirmar = true,
                NombreContiene = "NO-EXISTE",
                Habilitado = true
            });

            Assert.That(result.Exitoso, Is.True);
            Assert.That(result.CantidadCoincidente, Is.EqualTo(0));
            Assert.That(result.CantidadEliminada, Is.EqualTo(0));
            Assert.That(result.IdsEliminados.Any(), Is.False);

            repo.VerifyAll();
            uow.VerifyAll();
            tenant.VerifyAll();
        }
    }
}
