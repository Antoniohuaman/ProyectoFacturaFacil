using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CatalogoArticulosBC.Application.UseCases.ConsultarProducto;
using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.Entities;
using CatalogoArticulosBC.Domain.Repositories;
using CatalogoArticulosBC.Domain.ValueObjects;
using Moq;
using NUnit.Framework;
using SharedKernel.Application.Interfaces;
using SharedKernel.ValueObjects;

namespace CatalogoArticulosBC.Tests.Application.UseCases
{
    [TestFixture]
    public class ConsultarProductoUseCase_ExtraFieldsTests
    {
        private Mock<IProductoRepository> _repo = default!;
        private Mock<ITenantContext> _tenant = default!;

        private ConsultarProductoUseCase CreateSut()
        {
            _repo = new Mock<IProductoRepository>(MockBehavior.Strict);
            _tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            _tenant.Setup(t => t.EmpresaId).Returns(EmpresaId.From("20000000001"));
            return new ConsultarProductoUseCase(_repo.Object, _tenant.Object);
        }

        private static ProductoSimple CrearProductoConExtras(EmpresaId empresaId)
        {
            var moneda = Moneda.Create("PEN");
            var sku = Sku.Crear("SKU-100");
            var nombre = new NombreProducto("Prod con extras");
            var unidad = UnidadDeMedida.From("NIU");
            var afectacion = AfectacionImpuesto.From("10");
            var tasa = TasaImpuesto.FromPercent(18);
            var categoria = new Categoria("CAT");
            var establecimientos = new List<EstablecimientoId> { EstablecimientoId.From(Guid.NewGuid()) };

            var p = new ProductoSimple(
                empresaId,
                moneda,
                sku,
                nombre,
                unidad,
                afectacion,
                tasa,
                categoria,
                establecimientos,
                descripcion: null,
                marca: null,
                precioVenta: new PrecioVenta(12.50m, moneda, afectacion, true),
                codigoSunat: null,
                centroDeCosto: null,
                peso: null,
                codigoBarras: null,
                codigoFabrica: null,
                tipo: TipoProducto.Bien,
                tipoExistencia: TipoExistencia.ProductosTerminados,
                asignarATodosLosEstablecimientos: false,
                imagenPrincipalId: null,
                precioCompraDecimal: 8.75m,
                porcentajeGananciaDecimal: 20m,
                alias: "Alias Comercial"
            );
            return p;
        }

        [Test]
        public async Task Proyeccion_incluye_campos_extras_cuando_estan_presentes()
        {
            // arrange
            var sut = CreateSut();
            var empresaId = EmpresaId.From("20000000001");
            var producto = CrearProductoConExtras(empresaId);

            _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), empresaId))
                 .ReturnsAsync(producto);
            _repo.Setup(r => r.GetMultimediaByProductoIdAsync(It.IsAny<Guid>()))
                 .ReturnsAsync(new List<MultimediaProducto>());

            // act
            var dto = await sut.ExecuteAsync(new ConsultarProductoInputDto { ProductoId = producto.ProductoId, IncluirMultimedia = false }, CancellationToken.None);

            // assert
            Assert.Multiple(() =>
            {
                Assert.That(dto.PrecioCompra, Is.EqualTo(8.75m));
                Assert.That(dto.PorcentajeGanancia, Is.EqualTo(20m));
                Assert.That(dto.Alias, Is.EqualTo("Alias Comercial"));
            });
        }

        [Test]
        public async Task Proyeccion_devuelve_null_para_campos_extras_si_no_existen()
        {
            // arrange
            var sut = CreateSut();
            var empresaId = EmpresaId.From("20000000001");

            var moneda = Moneda.Create("PEN");
            var p = new ProductoSimple(
                empresaId,
                moneda,
                Sku.Crear("SKU-200"),
                new NombreProducto("Prod sin extras"),
                UnidadDeMedida.From("NIU"),
                AfectacionImpuesto.From("20"),
                TasaImpuesto.Cero,
                new Categoria("CAT"),
                new List<EstablecimientoId> { EstablecimientoId.From(Guid.NewGuid()) }
            );

            _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), empresaId))
                 .ReturnsAsync(p);
            _repo.Setup(r => r.GetMultimediaByProductoIdAsync(It.IsAny<Guid>()))
                 .ReturnsAsync(new List<MultimediaProducto>());

            // act
            var dto = await sut.ExecuteAsync(new ConsultarProductoInputDto { ProductoId = p.ProductoId, IncluirMultimedia = false }, CancellationToken.None);

            // assert
            Assert.Multiple(() =>
            {
                Assert.That(dto.PrecioCompra, Is.Null);
                Assert.That(dto.PorcentajeGanancia, Is.Null);
                Assert.That(dto.Alias, Is.Null);
            });
        }
    }
}
