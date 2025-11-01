using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CatalogoArticulosBC.Application.UseCases.EliminarProductosSeleccionados;
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
    public class EliminarProductosSeleccionadosUseCaseTests
    {
        private static EmpresaId EMP(string v = "20111111111") => EmpresaId.From(v);
        private static Moneda PEN() => Moneda.PEN();
        private static NombreProducto NP(string v) => new(v);
        private static UnidadDeMedida UDM(string v = "NIU") => UnidadDeMedida.From(v);
        private static AfectacionImpuesto AIGV() => AfectacionImpuesto.Gravado_10;
        private static TasaImpuesto TIGV18() => TasaImpuesto.IGV18;
        
        private static List<EstablecimientoId> ESTS()
            => new() { EstablecimientoId.New() };

        private static ProductoSimple CrearProducto(string skuCode, string nombre = "Cola 500ml", EmpresaId? empresaId = null)
        {
            var sku = Sku.Crear(skuCode);
            var p = new ProductoSimple(
                empresaId: empresaId ?? EMP(),
                moneda: PEN(),
                sku: sku,
                nombre: NP(nombre),
                unidadMedida: UDM(),
                afectacionImpuesto: AIGV(),
                tasaImpuesto: TIGV18(),
                categoriaId: CategoriaId.New(),
                establecimientosAsignados: ESTS(),
                descripcion: "Bebida"
            );
            p.AsignarCategoria(p.CategoriaId!.Value, nombreSnapshot: "GASEOSAS");
            return p;
        }

        [Test]
        public async Task EliminarPorIds_Y_Skus_Elimina_Los_Existentes_Y_Devuelve_Conteos()
        {
            // Arrange
            var repo   = new Mock<IProductoRepository>(MockBehavior.Strict);
            var uow    = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var empresa = TenantTestHelpers.AnyEmpresaId();
            var tenant = TenantTestHelpers.MockTenant(empresa);

            // Preparar 3 productos con el mismo EmpresaId que el tenant
            var p1 = CrearProducto("SKU-001", "P1", empresa);
            var p2 = CrearProducto("SKU-002", "P2", empresa);
            var p3 = CrearProducto("SKU-003", "P3", empresa);

            // IDs solicitados incluyen p1 y un inexistente
            var ids = new List<Guid> { p1.ProductoId, Guid.NewGuid() };
            // SKUs solicitados incluyen p2, y uno inválido sintácticamente y otro inexistente válido
            var skus = new List<string> { "SKU-002", "   ", "SKU-XYZ" };

            // Resoluciones por ID
            repo.Setup(r => r.GetByIdAsync(p1.ProductoId, empresa)).ReturnsAsync(p1);
            repo.Setup(r => r.GetByIdAsync(It.Is<Guid>(g => g != p1.ProductoId), empresa)).ReturnsAsync((ProductoSimple?)null);

            // Resoluciones por SKU
            repo.Setup(r => r.GetBySkuAsync(It.Is<Sku>(s => s.Valor == "SKU-002"), empresa)).ReturnsAsync(p2);
            repo.Setup(r => r.GetBySkuAsync(It.Is<Sku>(s => s.Valor == "SKU-XYZ"), empresa)).ReturnsAsync((ProductoSimple?)null);


            // Batch delete: solo p1 y p2 existen, así que se eliminan 2
            repo.Setup(r => r.DeleteManyAsync(
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.ToList().Contains(p1.ProductoId) && ids.ToList().Contains(p2.ProductoId)),
                empresa,
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(2);

            uow.Setup(x => x.CommitAsync()).Returns(Task.CompletedTask);

            var sut = new EliminarProductosSeleccionadosUseCase(repo.Object, uow.Object, tenant.Object);

            // Act
            var output = await sut.ExecuteAsync(new EliminarProductosSeleccionadosInputDto
            {
                Confirmar = true,
                ProductoIds = ids,
                Skus = skus
            });

            // Assert
            Assert.That(output.Exitoso, Is.True);
            Assert.That(output.EmpresaId, Is.EqualTo(empresa.Value));
            Assert.That(output.CantidadSolicitada, Is.EqualTo(3)); // p1 + p2 + SKU-XYZ (vacíos/espacios se filtran)
            Assert.That(output.CantidadEliminada, Is.EqualTo(2)); // p1 y p2
            Assert.That(output.CantidadNoEncontrada, Is.EqualTo(1 + 1)); // 1 id inexistente + 1 sku inexistente
            // Ya no se puede garantizar el orden ni los IDs exactos eliminados, solo la cantidad
            Assert.That(output.IdsEliminados.Count, Is.EqualTo(2));
            Assert.That(output.SkusNoEncontrados, Does.Contain("SKU-XYZ"));

            repo.Verify(r => r.DeleteManyAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<EmpresaId>(), It.IsAny<CancellationToken>()), Times.Once);
            uow.VerifyAll();
            tenant.VerifyAll();
        }

        [Test]
        public void DebeFallar_Si_NoConfirma()
        {
            var repo   = new Mock<IProductoRepository>(MockBehavior.Strict);
            var uow    = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var tenant = TenantTestHelpers.MockTenant();

            var sut = new EliminarProductosSeleccionadosUseCase(repo.Object, uow.Object, tenant.Object);

            Assert.ThrowsAsync<BusinessRuleException>(async () =>
                await sut.ExecuteAsync(new EliminarProductosSeleccionadosInputDto
                {
                    Confirmar = false,
                    ProductoIds = new[] { Guid.NewGuid() }
                }));

            repo.Verify(r => r.DeleteAsync(It.IsAny<ProductoSimple>()), Times.Never);
            uow.Verify(x => x.CommitAsync(), Times.Never);
        }

        [Test]
        public void DebeFallar_Si_NoVienen_Ids_Ni_Skus()
        {
            var repo   = new Mock<IProductoRepository>(MockBehavior.Strict);
            var uow    = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            var sut = new EliminarProductosSeleccionadosUseCase(repo.Object, uow.Object, tenant.Object);

            Assert.ThrowsAsync<BusinessRuleException>(async () =>
                await sut.ExecuteAsync(new EliminarProductosSeleccionadosInputDto
                {
                    Confirmar = true
                    // Sin ids ni skus
                }));

            repo.Verify(r => r.DeleteAsync(It.IsAny<ProductoSimple>()), Times.Never);
            uow.Verify(x => x.CommitAsync(), Times.Never);
        }

        [Test]
        public async Task Si_Nada_Existe_Igual_Commit_Una_Vez_Y_Contadores_En_Cero()
        {
            var repo   = new Mock<IProductoRepository>(MockBehavior.Strict);
            var uow    = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var empresa = TenantTestHelpers.AnyEmpresaId();
            var tenant = TenantTestHelpers.MockTenant(empresa);

            var idInexistente = Guid.NewGuid();
            repo.Setup(r => r.GetByIdAsync(idInexistente, empresa)).ReturnsAsync((ProductoSimple?)null);


            // Batch delete: ninguno existe, así que se eliminan 0
            repo.Setup(r => r.DeleteManyAsync(
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.ToList().Contains(idInexistente)),
                It.IsAny<EmpresaId>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);
            uow.Setup(x => x.CommitAsync()).Returns(Task.CompletedTask);

            var sut = new EliminarProductosSeleccionadosUseCase(repo.Object, uow.Object, tenant.Object);

            var output = await sut.ExecuteAsync(new EliminarProductosSeleccionadosInputDto
            {
                Confirmar = true,
                ProductoIds = new[] { idInexistente }
            });

            Assert.That(output.Exitoso, Is.True);
            Assert.That(output.CantidadSolicitada, Is.EqualTo(1));
            Assert.That(output.CantidadEliminada, Is.EqualTo(0));
            Assert.That(output.CantidadNoEncontrada, Is.EqualTo(1));

            // No se verifica GetByIdAsync porque ya no se usa
            repo.Verify(r => r.DeleteManyAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<EmpresaId>(), It.IsAny<CancellationToken>()), Times.Once);
            uow.VerifyAll();
            tenant.VerifyAll();
        }
    }
}
