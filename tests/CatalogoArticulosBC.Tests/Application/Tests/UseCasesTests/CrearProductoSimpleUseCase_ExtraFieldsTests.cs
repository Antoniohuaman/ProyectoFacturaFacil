using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CatalogoArticulosBC.Application.UseCases.CrearProductoSimple;
using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.Repositories;
using CatalogoArticulosBC.Domain.Services;
using CatalogoArticulosBC.Domain.ValueObjects;
using Moq;
using NUnit.Framework;
using SharedKernel.Application.Interfaces;
using SharedKernel.Events;
using SharedKernel.ValueObjects;

namespace CatalogoArticulosBC.Tests.Application.UseCases
{
    [TestFixture]
    public class CrearProductoSimpleUseCase_ExtraFieldsTests
    {
        private Mock<IProductoRepository> _repo = default!;
        private Mock<IUnitOfWork> _uow = default!;
        private Mock<ISkuGenerator> _skuGen = default!;
        private Mock<ITenantContext> _tenant = default!;
        private Mock<IEventBus> _bus = default!;

        private CrearProductoSimpleUseCase CreateSut()
        {
            _repo = new Mock<IProductoRepository>(MockBehavior.Strict);
            _uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            _tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            _skuGen = new Mock<ISkuGenerator>(MockBehavior.Strict);
            _bus = new Mock<IEventBus>(MockBehavior.Strict);

            _tenant.Setup(t => t.EmpresaId).Returns(EmpresaId.From("20000000001"));
            return new CrearProductoSimpleUseCase(_repo.Object, _uow.Object, _tenant.Object, _skuGen.Object, _bus.Object);
        }

        [Test]
        public async Task Al_crear_producto_con_campos_extra_se_persisten_en_el_agregado_y_salen_en_dto()
        {
            // arrange
            var estId = Guid.NewGuid();
            var dto = new CrearProductoSimpleInputDto
            {
                AutogenerarSku = false,
                Sku = "PROD-EXTRA-01",
                Nombre = "Prod Extra",
                UnidadMedidaCodigo = "NIU",
                AfectacionImpuestoCodigo = "10",
                TasaImpuestoPercent = 18m,
                Categoria = "CAT",
                MonedaCodigoIso4217 = "PEN",
                Tipo = TipoProducto.Bien,
                Establecimientos = new List<Guid> { estId },
                PrecioVentaMonto = 12.50m,
                PrecioIncluyeIGV = true,
                // Nuevos
                PrecioCompra = 7.40m,
                PorcentajeGanancia = 15.50m,
                Alias = " Mi  Alias  Limpio "
            };

            var sut = CreateSut();

            _skuGen.Setup(g => g.Generar())
                   .Returns(Sku.Crear("IGNORED")); // no se usa porque AutogenerarSku=false

            _repo.Setup(r => r.ExisteSkuAsync(It.IsAny<Sku>(), It.IsAny<EmpresaId>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);

            ProductoSimple? capturado = null;
            _repo.Setup(r => r.AddAsync(It.IsAny<ProductoSimple>(), It.IsAny<CancellationToken>()))
                 .Callback<ProductoSimple, CancellationToken>((p, _) => capturado = p)
                 .Returns(Task.CompletedTask);

            _uow.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);
            _bus.Setup(b => b.PublishAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // act
            var result = await sut.Handle(dto, CancellationToken.None);

            // assert
            Assert.That(capturado, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(capturado!.PrecioCompra!.Monto, Is.EqualTo(7.40m));
                Assert.That(capturado.PorcentajeGanancia!.Valor, Is.EqualTo(15.50m));
                Assert.That(capturado.Alias!.Valor, Is.EqualTo("Mi Alias Limpio")); // colapsado/trimmed

                Assert.That(result.PrecioCompra, Is.EqualTo(7.40m));
                Assert.That(result.PorcentajeGanancia, Is.EqualTo(15.50m));
                Assert.That(result.Alias, Is.EqualTo("Mi Alias Limpio"));
            });
        }
    }
}
