using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.Interfaces; // IUnitOfWork
using ListaPreciosBC.Application.UseCases;   // UpsertMatrizVolumenUseCase
using ListaPreciosBC.Domain.Repositories;    // IListaPrecioRepository, IPrecioProductoRepository
using ListaPreciosBC.Domain.Aggregates;      // ListaPrecio, PrecioProducto
using ListaPreciosBC.Domain.ValueObjects;    // IdentificadorColumnaPrecio, NombreColumnaPrecio, ModoValorizacionColumna, PeriodoVigencia, ValorPrecio
using NUnit.Framework;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;             // Moneda, Sku, EmpresaId, ProductoId
using SharedKernel.Application.Interfaces;   // ITenantContext
using Moq;

namespace ListaPreciosBC.Tests.Application.UseCases
{
    public class UpsertMatrizVolumenUseCaseTests
    {
        // ---------------------- Fakes InMemory ----------------------

        private sealed class InMemoryListaPrecioRepository : IListaPrecioRepository
        {
            public ListaPrecio? ListaActiva { get; set; }

            public Task<ListaPrecio?> ObtenerActivaAsync(EmpresaId empresaId, Guid? sucursalId = null, CancellationToken ct = default)
                => Task.FromResult(ListaActiva);

            public Task<ListaPrecio?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default)
                => Task.FromResult(ListaActiva is not null && ListaActiva.Id == id ? ListaActiva : null);

            public Task GuardarAsync(ListaPrecio aggregate, EmpresaId empresaId, Guid? sucursalId, int expectedVersion, CancellationToken ct = default)
                => Task.CompletedTask;
        }

        private sealed class InMemoryPrecioProductoRepository : IPrecioProductoRepository
        {
            private readonly Dictionary<string, PrecioProducto> _store = new();
            private readonly Dictionary<string, int> _loadedVersion = new();
            private string? _lastLookupProductoKey;
            public bool SimularConcurrencia { get; set; }

            private static string Key(ProductoId productoId) => productoId.Value.ToString();

            public Task<PrecioProducto?> ObtenerPorProductoIdAsync(EmpresaId empresaId, EstablecimientoId? establecimientoId, ProductoId productoId, CancellationToken ct = default)
            {
                var key = Key(productoId);
                if (_store.TryGetValue(key, out var agg))
                {
                    _loadedVersion[key] = agg.Version;
                    _lastLookupProductoKey = key;
                    return Task.FromResult<PrecioProducto?>(agg);
                }
                _lastLookupProductoKey = key;
                return Task.FromResult<PrecioProducto?>(null);
            }

            public Task GuardarAsync(PrecioProducto aggregate, EmpresaId empresaId, EstablecimientoId? establecimientoId, int expectedVersion, CancellationToken ct = default)
            {
                var key = _lastLookupProductoKey ?? Key(aggregate.ProductoId);

                if (SimularConcurrencia && _loadedVersion.TryGetValue(key, out var v))
                {
                    _loadedVersion[key] = v + 1; // simula cambio entre load y save
                }

                if (_loadedVersion.TryGetValue(key, out var loaded) && loaded != expectedVersion)
                    throw new ConcurrencyException(
                        "Versión inesperada del agregado.",
                        key,
                        expectedVersion,
                        aggregate.Version,
                        null,
                        null
                    );

                _store[key] = aggregate;
                _loadedVersion[key] = aggregate.Version;
                return Task.CompletedTask;
            }

            public Task EliminarAsync(EmpresaId empresaId, EstablecimientoId? establecimientoId, ProductoId productoId, int? expectedVersion = null, CancellationToken ct = default)
            {
                var key = Key(productoId);
                if (_store.TryGetValue(key, out var agg))
                {
                    if (expectedVersion.HasValue && agg.Version != expectedVersion.Value)
                        throw new ConcurrencyException(
                            "Versión inesperada del agregado al eliminar.",
                            key,
                            expectedVersion.Value,
                            agg.Version,
                            null,
                            null
                        );
                    _store.Remove(key);
                    _loadedVersion.Remove(key);
                }
                return Task.CompletedTask;
            }

            public void Seed(PrecioProducto agg)
            {
                var key = Key(agg.ProductoId);
                _store[key] = agg;
                _loadedVersion[key] = agg.Version;
            }

            public Task<PrecioProducto?> ObtenerPorProductoIdAsync(ProductoId productoId, CancellationToken ct = default)
            {
                _store.TryGetValue(Key(productoId), out var agg);
                return Task.FromResult(agg);
            }
        }

        private sealed class InMemoryUow : IUnitOfWork
        {
            public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;
        }

        // ---------------------- Builders (alineados a invariantes) ----------------------

        private static ListaPrecio CrearListaActivaConBaseYColumnaVolumen(byte numeroVolumen, bool visible = true)
        {
            var baseCfg = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.DesdeNumero(1),
                NombreColumnaPrecio.Crear("Base"),
                ModoValorizacionColumna.Fijo,
                esBase: true,
                visible: true,
                orden: 1
            );
            var volCfg = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.DesdeNumero(numeroVolumen),
                NombreColumnaPrecio.Crear("Volumen"),
                ModoValorizacionColumna.PorVolumen,
                esBase: false,
                visible: visible,
                orden: numeroVolumen
            );
            var plantilla = PlantillaColumnasPrecio.Crear(new[] { baseCfg, volCfg });
            return ListaPrecio.CrearNueva(EmpresaId.From("EMP-01"), Guid.NewGuid(), plantilla);
        }

        private static PrecioProducto CrearPrecioProducto(string sku)
        {
            return PrecioProducto.CrearNuevo(EmpresaId.From("EMP-01"), ProductoId.New());
        }

        private static bool ExistePrecioParaCantidad(PrecioProducto agg, byte columnaNumero, int cantidad, UnidadDeMedida? unidad = null)
        {
            var colId = IdentificadorColumnaPrecio.DesdeNumero(columnaNumero);
            var unidadEvaluacion = unidad ?? UnidadDeMedida.NIU;
            var vigente = agg.ObtenerPrecioVigente(colId, unidadEvaluacion, DateTimeOffset.UtcNow.Date, cantidad);
            return vigente is not null;
        }

        // ---------------------- Tests ----------------------

        [Test]
        public async Task UpsertMatrizVolumen_Exito_CreaONActualiza_MatrizYPersiste()
        {
            var listaRepo = new InMemoryListaPrecioRepository
            {
                ListaActiva = CrearListaActivaConBaseYColumnaVolumen(numeroVolumen: 2)
            };

            var precioRepo = new InMemoryPrecioProductoRepository();
            var uow = new InMemoryUow();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var catalogo = new Mock<ListaPreciosBC.Application.Interfaces.ICatalogoReadModel>();
            var productoId = ProductoId.New();
            catalogo
                .Setup(c => c.TryGetProductoIdBySkuAsync(It.IsAny<EmpresaId>(), It.Is<string>(s => s == "SKU-001"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(productoId);
            var sut = new UpsertMatrizVolumenUseCase(precioRepo, listaRepo, uow, tenant.Object, catalogo.Object);

            var req = new UpsertMatrizVolumenUseCase.Request(
                Sku: "SKU-001",
                ColumnaNumero: 2,
                Tramos: new List<UpsertMatrizVolumenUseCase.Tramo>
                {
                    new(DesdeCantidad: 1,  HastaCantidad: 9,  Monto: 10.50m, IncluyeImpuesto: true),
                    new(DesdeCantidad: 10, HastaCantidad: null, Monto: 9.90m, IncluyeImpuesto: true)
                },
                CantidadReferenciaParaEventoBase: 1,
                UnidadMedidaCodigo: UnidadDeMedida.KGM.Codigo,
                Usuario: "tester"
            );

            var res = await sut.Handle(req, CancellationToken.None);

            Assert.That(res.Sku, Is.EqualTo("SKU-001"));
            Assert.That(res.ColumnaNumero, Is.EqualTo(2));
            Assert.That(res.TramosActualizados, Is.EqualTo(2));
            Assert.That(res.UnidadMedidaCodigo, Is.EqualTo(UnidadDeMedida.KGM.Codigo));
            Assert.That(res.Version, Is.GreaterThanOrEqualTo(0));

            // Efecto observable: el agregado debe resolver precio para cantidades en ambos tramos
            var post = await precioRepo.ObtenerPorProductoIdAsync(productoId);
            Assert.That(post, Is.Not.Null);
            Assert.That(ExistePrecioParaCantidad(post!, 2, cantidad: 1, unidad: UnidadDeMedida.KGM), Is.True);
            Assert.That(ExistePrecioParaCantidad(post!, 2, cantidad: 10, unidad: UnidadDeMedida.KGM), Is.True);

            var registroKg = post!.PreciosPorUnidad.SingleOrDefault(p =>
                p.UnidadDeMedida.Codigo == UnidadDeMedida.KGM.Codigo &&
                p.ColumnaId.Equals(IdentificadorColumnaPrecio.DesdeNumero(2)));
            Assert.That(registroKg, Is.Not.Null);
            Assert.That(registroKg!.MatrizVolumen, Is.Not.Null);

            // Assert de evento con tenant
            var ev = post!.DomainEvents.OfType<ListaPreciosBC.Domain.Events.MatrizVolumenActualizada>().LastOrDefault();
            Assert.That(ev, Is.Not.Null);
            Assert.That(ev!.EmpresaId, Is.EqualTo(EmpresaId.From("EMP-01")));
        }

        [Test]
        public void UpsertMatrizVolumen_Falla_SiNoHayListaActiva()
        {
            var listaRepo = new InMemoryListaPrecioRepository { ListaActiva = null };
            var precioRepo = new InMemoryPrecioProductoRepository();
            var uow = new InMemoryUow();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var catalogo = new Mock<ListaPreciosBC.Application.Interfaces.ICatalogoReadModel>();
            catalogo
                .Setup(c => c.TryGetProductoIdBySkuAsync(It.IsAny<EmpresaId>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProductoId.New());
            var sut = new UpsertMatrizVolumenUseCase(precioRepo, listaRepo, uow, tenant.Object, catalogo.Object);

            var req = new UpsertMatrizVolumenUseCase.Request(
                Sku: "SKU-404",
                ColumnaNumero: 2,
                Tramos: new List<UpsertMatrizVolumenUseCase.Tramo>
                {
                    new(1, 10, 10m, true)
                },
                UnidadMedidaCodigo: UnidadDeMedida.NIU.Codigo
            );

            Assert.That(async () => await sut.Handle(req, CancellationToken.None),
                        Throws.TypeOf<NotFoundException>());
        }

        [Test]
        public void UpsertMatrizVolumen_Falla_SiColumnaNoExiste()
        {
            var listaRepo = new InMemoryListaPrecioRepository
            {
                ListaActiva = CrearListaActivaConBaseYColumnaVolumen(numeroVolumen: 2)
            };

            var precioRepo = new InMemoryPrecioProductoRepository();
            var uow = new InMemoryUow();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var catalogo2 = new Mock<ListaPreciosBC.Application.Interfaces.ICatalogoReadModel>();
            catalogo2
                .Setup(c => c.TryGetProductoIdBySkuAsync(It.IsAny<EmpresaId>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProductoId.New());
            var sut = new UpsertMatrizVolumenUseCase(precioRepo, listaRepo, uow, tenant.Object, catalogo2.Object);

            var req = new UpsertMatrizVolumenUseCase.Request(
                Sku: "SKU-001",
                ColumnaNumero: 9, // no existe
                Tramos: new List<UpsertMatrizVolumenUseCase.Tramo>
                {
                    new(1, 10, 10m, true)
                },
                UnidadMedidaCodigo: UnidadDeMedida.NIU.Codigo
            );

            Assert.That(async () => await sut.Handle(req, CancellationToken.None),
                        Throws.TypeOf<NotFoundException>());
        }

        [Test]
        public void UpsertMatrizVolumen_Falla_SiColumnaEsFija()
        {
            // Lista solo con Base (Fijo) y sin columna de volumen = columna 1
            var baseCfg = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.DesdeNumero(1),
                NombreColumnaPrecio.Crear("Base"),
                ModoValorizacionColumna.Fijo,
                esBase: true,
                visible: true,
                orden: 1
            );
            var plantilla = PlantillaColumnasPrecio.Crear(new[] { baseCfg });
            var lista = ListaPrecio.CrearNueva(EmpresaId.From("EMP-01"), Guid.NewGuid(), plantilla);
            var listaRepo = new InMemoryListaPrecioRepository { ListaActiva = lista };
            IPrecioProductoRepository precioRepo = new InMemoryPrecioProductoRepository();
            var uow = new InMemoryUow();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var catalogo3 = new Mock<ListaPreciosBC.Application.Interfaces.ICatalogoReadModel>();
            catalogo3
                .Setup(c => c.TryGetProductoIdBySkuAsync(It.IsAny<EmpresaId>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProductoId.New());
            var sut = new UpsertMatrizVolumenUseCase(precioRepo, listaRepo, uow, tenant.Object, catalogo3.Object);

            var req = new UpsertMatrizVolumenUseCase.Request(
                Sku: "SKU-001",
                ColumnaNumero: 1, // fijo
                Tramos: new List<UpsertMatrizVolumenUseCase.Tramo>
                {
                    new(1, 10, 10m, true)
                },
                UnidadMedidaCodigo: UnidadDeMedida.NIU.Codigo
            );

            Assert.That(async () => await sut.Handle(req, CancellationToken.None),
                        Throws.TypeOf<BusinessRuleException>());
        }

        [Test]
        public void UpsertMatrizVolumen_FallaPorConcurrencia_SiVersionCambiaEntreLoadYSave()
        {
            var listaRepo = new InMemoryListaPrecioRepository
            {
                ListaActiva = CrearListaActivaConBaseYColumnaVolumen(numeroVolumen: 2)
            };

            var precioRepo = new InMemoryPrecioProductoRepository { SimularConcurrencia = true };

            // Seed: SKU existente con algo en historial (para subir versión)
            var existente = CrearPrecioProducto("SKU-001");
            existente.UpsertPrecioFijo(
                IdentificadorColumnaPrecio.DesdeNumero(1),
                ValorPrecio.DesdeMonto(9.99m, Moneda.PEN(), true),
                PeriodoVigencia.Crear(DateTimeOffset.UtcNow.AddDays(-10), null),
                "seed",
                DateTimeOffset.UtcNow.AddDays(-10)
            );
            precioRepo.Seed(existente);

            var uow = new InMemoryUow();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var catalogo4 = new Mock<ListaPreciosBC.Application.Interfaces.ICatalogoReadModel>();
            catalogo4
                .Setup(c => c.TryGetProductoIdBySkuAsync(It.IsAny<EmpresaId>(), It.Is<string>(s => s == "SKU-001"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(existente.ProductoId);
            var sut = new UpsertMatrizVolumenUseCase(precioRepo, listaRepo, uow, tenant.Object, catalogo4.Object);

            var req = new UpsertMatrizVolumenUseCase.Request(
                Sku: "SKU-001",
                ColumnaNumero: 2,
                Tramos: new List<UpsertMatrizVolumenUseCase.Tramo>
                {
                    new(1, 10, 10m, true)
                },
                UnidadMedidaCodigo: UnidadDeMedida.NIU.Codigo,
                Usuario: "tester"
            );

            Assert.That(async () => await sut.Handle(req, CancellationToken.None),
                        Throws.TypeOf<ConcurrencyException>());
        }

        [Test]
        public void UpsertMatrizVolumen_Falla_SiTramosSeSolapan()
        {
            var listaRepo = new InMemoryListaPrecioRepository
            {
                ListaActiva = CrearListaActivaConBaseYColumnaVolumen(numeroVolumen: 2)
            };

            var precioRepo = new InMemoryPrecioProductoRepository();
            var uow = new InMemoryUow();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var catalogo5 = new Mock<ListaPreciosBC.Application.Interfaces.ICatalogoReadModel>();
            catalogo5
                .Setup(c => c.TryGetProductoIdBySkuAsync(It.IsAny<EmpresaId>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProductoId.New());
            var sut = new UpsertMatrizVolumenUseCase(precioRepo, listaRepo, uow, tenant.Object, catalogo5.Object);

            // Tramos solapados: [1..10] y [8..∞)
            var req = new UpsertMatrizVolumenUseCase.Request(
                Sku: "SKU-001",
                ColumnaNumero: 2,
                Tramos: new List<UpsertMatrizVolumenUseCase.Tramo>
                {
                    new(1, 10, 10m, true),
                    new(8, null, 9m, true)
                },
                UnidadMedidaCodigo: UnidadDeMedida.NIU.Codigo
            );

            Assert.That(async () => await sut.Handle(req, CancellationToken.None),
                        Throws.TypeOf<BusinessRuleException>());
        }

        [Test]
        public async Task UpsertMatrizVolumen_EmiteEventosConTenantYEstablecimiento_CuandoSeIndicaSucursal()
        {
            // Arrange
            var sucursalId = Guid.NewGuid();
            var listaRepo = new InMemoryListaPrecioRepository
            {
                ListaActiva = CrearListaActivaConBaseYColumnaVolumen(numeroVolumen: 2)
            };

            var precioRepo = new InMemoryPrecioProductoRepository();
            var uowMock = new Moq.Mock<IUnitOfWork>();
            uowMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var tenant = new Moq.Mock<ITenantContext>();
            var empresaTenant = EmpresaId.From("EMP-TNT");
            tenant.SetupGet(t => t.EmpresaId).Returns(empresaTenant);

            var catalogo = new Moq.Mock<ListaPreciosBC.Application.Interfaces.ICatalogoReadModel>();
            var productoIdEst = ProductoId.New();
            catalogo
                .Setup(c => c.TryGetProductoIdBySkuAsync(It.IsAny<EmpresaId>(), It.Is<string>(s => s == "SKU-EST-002"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(productoIdEst);

            var sut = new UpsertMatrizVolumenUseCase(precioRepo, listaRepo, uowMock.Object, tenant.Object, catalogo.Object);

            var req = new UpsertMatrizVolumenUseCase.Request(
                Sku: "SKU-EST-002",
                ColumnaNumero: 2,
                Tramos: new List<UpsertMatrizVolumenUseCase.Tramo>
                {
                    new(1, 10, 10m, true)
                },
                CantidadReferenciaParaEventoBase: 1,
                UnidadMedidaCodigo: UnidadDeMedida.NIU.Codigo,
                Usuario: "tester",
                Cuando: DateTimeOffset.UtcNow,
                SucursalId: sucursalId
            );

            // Act
            var res = await sut.Handle(req, CancellationToken.None);

            // Assert commit 1 vez
            uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);

            // Assert eventos con tenant y establecimiento
            var post = await precioRepo.ObtenerPorProductoIdAsync(productoIdEst);
            Assert.That(post, Is.Not.Null);
            var ev = post!.DomainEvents.OfType<ListaPreciosBC.Domain.Events.MatrizVolumenActualizada>().LastOrDefault();
            Assert.That(ev, Is.Not.Null);
            Assert.That(ev!.EmpresaId, Is.EqualTo(empresaTenant));
            Assert.That(ev!.EstablecimientoId, Is.EqualTo(sucursalId));
        }
    }
}
