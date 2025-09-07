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
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace CatalogoArticulosBC.Tests.Application.UseCases
{
    [TestFixture]
    public class CrearProductoSimpleUseCaseTests
    {
        private Mock<IProductoRepository> _repo = default!;
        private Mock<IUnitOfWork> _uow = default!;
        private Mock<ISkuGenerator> _skuGen = default!;
        private Mock<ITenantContext> _tenant = default!;
        private Mock<IEventBus> _bus = default!;

        private CrearProductoSimpleUseCase CreateSut(bool withBus = true, bool withSkuGen = true)
        {
            _repo = new Mock<IProductoRepository>(MockBehavior.Strict);
            _uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            _tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            if (withSkuGen) _skuGen = new Mock<ISkuGenerator>(MockBehavior.Strict);
            else _skuGen = new Mock<ISkuGenerator>(MockBehavior.Loose);

            if (withBus) _bus = new Mock<IEventBus>(MockBehavior.Strict);
            else _bus = new Mock<IEventBus>(MockBehavior.Loose);

            _tenant.Setup(t => t.EmpresaId).Returns(EmpresaId.From("20000000001"));
            // (TenantId puede no ser relevante aquí)
            return new CrearProductoSimpleUseCase(_repo.Object, _uow.Object, _tenant.Object,
                                                  withSkuGen ? _skuGen.Object : null,
                                                  withBus ? _bus.Object : null);
        }

        private static CrearProductoSimpleInputDto DtoBaseBienManualSku(Guid estId) => new()
        {
            AutogenerarSku = false,
            Sku = "PROD-001",
            Nombre = "Gaseosa 500ml",
            UnidadMedidaCodigo = "NIU",
            AfectacionImpuestoCodigo = "10",   // Gravado IGV
            TasaImpuestoPercent = 18m,         // 18%
            Categoria = "BEBIDAS",
            MonedaCodigoIso4217 = "PEN",
            Tipo = TipoProducto.Bien,
            TipoExistencia = TipoExistencia.ProductosTerminados,
            Establecimientos = new List<Guid> { estId },
            Descripcion = "Bebida carbonatada",
            Marca = "ACME",
            PrecioVentaMonto = 5.50m,
            PrecioIncluyeIGV = true,
            CodigoSUNAT = "12345678",
            CentroDeCostoCodigo = "VENTAS",
            CentroDeCostoNombre = "Ventas",
            PesoKg = 0.55m,
            CodigoBarras = "7501031311309",    // GTIN-13 válido proporcionado por el usuario
            CodigoFabrica = "FAB-01"
        };

        private static CrearProductoSimpleInputDto DtoBaseServicioAutoSku(Guid estId) => new()
        {
            AutogenerarSku = true,
            // Sku omitido
            Nombre = "Servicio de instalación",
            UnidadMedidaCodigo = "ZZ",         // Servicio
            AfectacionImpuestoCodigo = "20",   // Exonerado
            TasaImpuestoPercent = null,        // se asume 0
            Categoria = "SERVICIOS",
            MonedaCodigoIso4217 = "PEN",
            Tipo = TipoProducto.Servicio,
            // TipoExistencia omitido -> Servicios
            Establecimientos = new List<Guid> { estId },
            Descripcion = "Instalación básica",
            PrecioVentaMonto = null,           // opcional
            PrecioIncluyeIGV = true
        };

        [Test]
        public async Task Crear_Bien_SkuManual_Gravado18_OK_Persiste_PublicaEventos_Y_RetornaSalida()
        {
            // arrange
            var estId = Guid.NewGuid();
            var dto = DtoBaseBienManualSku(estId);

            var sut = CreateSut(withBus: true, withSkuGen: false);

            ProductoSimple? capturado = null;

            _repo.Setup(r => r.ExisteSkuAsync(It.IsAny<Sku>(), It.IsAny<EmpresaId>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);

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
                Assert.That(result.ProductoId, Is.EqualTo(capturado!.ProductoId));
                Assert.That(result.Sku, Is.EqualTo("PROD-001"));
                Assert.That(result.Habilitado, Is.True);
                Assert.That(result.Tipo, Is.EqualTo(TipoProducto.Bien));
                Assert.That(result.TipoExistencia, Is.EqualTo(TipoExistencia.ProductosTerminados));
                Assert.That(result.Nombre, Is.EqualTo("Gaseosa 500ml"));
                Assert.That(result.Categoria, Is.EqualTo("BEBIDAS"));
                Assert.That(result.Moneda, Is.EqualTo("PEN"));
                Assert.That(result.PrecioVentaMonto, Is.EqualTo(5.50m));
                Assert.That(result.AfectacionImpuestoCodigo, Is.EqualTo("10"));
                Assert.That(result.TasaImpuestoPercent, Is.EqualTo(18m));
                Assert.That(result.Establecimientos.Single(), Is.EqualTo(estId));
                Assert.That(capturado!.DomainEvents.Any(e => e.GetType().Name == "ProductoCreado"), Is.True);
            });

            _repo.Verify(r => r.ExisteSkuAsync(It.Is<Sku>(s => s.Valor == "PROD-001"),
                                               It.Is<EmpresaId>(e => e.Value == "20000000001"),
                                               It.IsAny<CancellationToken>()), Times.Once);

            _repo.Verify(r => r.AddAsync(It.IsAny<ProductoSimple>(), It.IsAny<CancellationToken>()), Times.Once);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
            _bus.Verify(b => b.PublishAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Crear_Servicio_AutoSku_Exonerado_OK_SinPrecio_Persiste()
        {
            // arrange
            var estId = Guid.NewGuid();
            var dto = DtoBaseServicioAutoSku(estId);

            var sut = CreateSut(withBus: true, withSkuGen: true);

            _skuGen.Setup(g => g.Generar()).Returns(Sku.Crear("SERV-001"));

            ProductoSimple? capturado = null;

            _repo.Setup(r => r.ExisteSkuAsync(It.IsAny<Sku>(), It.IsAny<EmpresaId>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);

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
                Assert.That(result.Sku, Is.EqualTo("SERV-001"));
                Assert.That(result.Tipo, Is.EqualTo(TipoProducto.Servicio));
                Assert.That(result.TipoExistencia, Is.EqualTo(TipoExistencia.Servicios));
                Assert.That(result.AfectacionImpuestoCodigo, Is.EqualTo("20")); // Exonerado
                Assert.That(result.TasaImpuestoPercent, Is.EqualTo(0m));
                Assert.That(result.PrecioVentaMonto, Is.Null);
                Assert.That(result.Establecimientos.Single(), Is.EqualTo(estId));
            });

            _skuGen.Verify(g => g.Generar(), Times.Once);
            _repo.Verify(r => r.AddAsync(It.IsAny<ProductoSimple>(), It.IsAny<CancellationToken>()), Times.Once);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
            _bus.Verify(b => b.PublishAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void Crear_Gravado_SinTasa_LanzaArgumentException()
        {
            var estId = Guid.NewGuid();
            var dto = new CrearProductoSimpleInputDto
            {
                AutogenerarSku = false,
                Sku = "ABC-001",
                Nombre = "Producto Gravado",
                UnidadMedidaCodigo = "NIU",
                AfectacionImpuestoCodigo = "10",   // gravado
                TasaImpuestoPercent = null,        // falta
                Categoria = "CAT",
                MonedaCodigoIso4217 = "PEN",
                Tipo = TipoProducto.Bien,
                Establecimientos = new List<Guid> { estId }
            };

            var sut = CreateSut(withBus: false, withSkuGen: false);

            _repo.Setup(r => r.ExisteSkuAsync(It.IsAny<Sku>(), It.IsAny<EmpresaId>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);

            Assert.That(async () => await sut.Handle(dto), Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void Crear_DuplicadoSku_LanzaBusinessRuleException()
        {
            var estId = Guid.NewGuid();
            var dto = DtoBaseBienManualSku(estId);

            var sut = CreateSut(withBus: false, withSkuGen: false);

            _repo.Setup(r => r.ExisteSkuAsync(It.IsAny<Sku>(), It.IsAny<EmpresaId>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

            Assert.That(async () => await sut.Handle(dto), Throws.TypeOf<BusinessRuleException>());
        }

        [Test]
        public void Crear_SinEstablecimientos_LanzaArgumentException()
        {
            var dto = DtoBaseBienManualSku(Guid.NewGuid());
            dto.Establecimientos.Clear();

            var sut = CreateSut(withBus: false, withSkuGen: false);

            _repo.Setup(r => r.ExisteSkuAsync(It.IsAny<Sku>(), It.IsAny<EmpresaId>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);

            Assert.That(async () => await sut.Handle(dto), Throws.TypeOf<ArgumentException>());
        }
    }
}
