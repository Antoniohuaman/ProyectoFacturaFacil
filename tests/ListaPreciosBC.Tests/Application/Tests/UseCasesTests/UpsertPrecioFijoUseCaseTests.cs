using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.UseCases;
using ListaPreciosBC.Domain.Aggregates;
using ListaPreciosBC.Application.Interfaces;
using ListaPreciosBC.Domain.Repositories;
using ListaPreciosBC.Domain.ValueObjects;
using NUnit.Framework;
using Moq;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;
using ListaPreciosBC.Domain.Events;
using SharedKernel.Application.Interfaces;   // ITenantContext

namespace ListaPreciosBC.Tests.Application.UseCases
{
    public class UpsertPrecioFijoUseCaseTests
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

            public bool SimularConcurrencia { get; set; } = false;

            private static string Key(ProductoId productoId) => productoId.Value.ToString();

            public Task<PrecioProducto?> ObtenerPorProductoIdAsync(EmpresaId empresaId, EstablecimientoId? establecimientoId, ProductoId productoId, CancellationToken ct = default)
            {
                var key = Key(productoId);
                _store.TryGetValue(key, out var agg);
                if (agg is not null) _loadedVersion[key] = agg.Version;
                _lastLookupProductoKey = key;
                return Task.FromResult<PrecioProducto?>(agg);
            }

            public Task GuardarAsync(PrecioProducto aggregate, EmpresaId empresaId, EstablecimientoId? establecimientoId, int expectedVersion, CancellationToken ct = default)
            {
                var key = _lastLookupProductoKey ?? Key(aggregate.ProductoId);

                if (SimularConcurrencia && _loadedVersion.TryGetValue(key, out var v))
                {
                    _loadedVersion[key] = v + 1;
                }

                if (_loadedVersion.TryGetValue(key, out var loaded) && loaded != expectedVersion)
                    throw new ConcurrencyException(
                        "Versión inesperada del agregado.",
                        key,
                        expectedVersion,
                        loaded,
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
                if (_store.TryGetValue(key, out var agg) && expectedVersion.HasValue && agg.Version != expectedVersion.Value)
                    throw new ConcurrencyException("Versión inesperada del agregado al eliminar.", key, expectedVersion.Value, agg.Version, null, null);
                _store.Remove(key);
                _loadedVersion.Remove(key);
                return Task.CompletedTask;
            }

            // Helpers de prueba
            public void Seed(PrecioProducto agg)
            {
                var key = Key(agg.ProductoId);
                _store[key] = agg;
                _loadedVersion[key] = agg.Version;
            }

            public Task<PrecioProducto?> ObtenerPorProductoIdAsync(ProductoId productoId, CancellationToken ct = default)
                => Task.FromResult(_store.TryGetValue(Key(productoId), out var agg) ? agg : null);
        }

        private sealed class InMemoryUow : IUnitOfWork
        {
            public int CommitCount { get; private set; }
            public Task CommitAsync(CancellationToken ct = default)
            {
                CommitCount++;
                return Task.CompletedTask;
            }
        }

        

        // ---------------------- Builders de dominio (usando tu API pública) ----------------------

        private static ListaPrecio CrearListaActivaConColumnaFija(byte numero, bool esBase = true, bool visible = true)
        {
            var cfg = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.DesdeNumero(numero),
                NombreColumnaPrecio.Crear(esBase ? "Base" : $"Col{numero}"),
                ModoValorizacionColumna.Fijo,
                esBase: esBase,
                visible: visible,
                orden: numero
            );
            var plantilla = PlantillaColumnasPrecio.Crear(new[] { cfg });
            return ListaPrecio.CrearNueva(EmpresaId.From("EMP-01"), Guid.NewGuid(), plantilla);
        }

        private static ListaPrecio CrearListaActivaConColumnaVolumen(byte numero, bool esBase = false, bool visible = true)
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
                IdentificadorColumnaPrecio.DesdeNumero(numero),
                NombreColumnaPrecio.Crear($"Vol{numero}"),
                ModoValorizacionColumna.PorVolumen,
                esBase: false,
                visible: visible,
                orden: numero
            );
            var plantilla = PlantillaColumnasPrecio.Crear(new[] { baseCfg, volCfg });
            return ListaPrecio.CrearNueva(EmpresaId.From("EMP-01"), Guid.NewGuid(), plantilla);
        }

        private static PrecioProducto CrearPrecioProducto(Guid empresaId, Guid? sucursalId, string sku)
        {
            // Usar la fábrica real
            return PrecioProducto.CrearNuevo(EmpresaId.From("EMP-01"), ProductoId.New());
        }

        // ---------------------- Tests ----------------------

        [Test]
        public async Task UpsertPrecioFijo_Exito_CreaONActualiza_PersistiendoConExpectedVersion()
        {
            // Arrange
            var empresa = Guid.NewGuid();
            // sucursal no se usa en este test

            var listaRepo = new InMemoryListaPrecioRepository
            {
                ListaActiva = CrearListaActivaConColumnaFija(numero: 1, esBase: true)
            };

            var precioRepo = new InMemoryPrecioProductoRepository();
            var uow = new InMemoryUow();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
        var catalogo = new Moq.Mock<ICatalogoReadModel>();
        var productoId = ProductoId.New();
        catalogo.Setup(c => c.TryGetProductoIdBySkuAsync(It.IsAny<EmpresaId>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(productoId);
        var sut = new UpsertPrecioFijoUseCase(precioRepo, listaRepo, uow, tenant.Object, catalogo.Object);

            var req = new UpsertPrecioFijoUseCase.Request(
                Sku: "SKU-001",
                ColumnaNumero: 1,
                Monto: 10.50m,
                IncluyeImpuesto: true,
                VigenciaDesde: DateTimeOffset.UtcNow.Date,
                VigenciaHasta: null,
                UnidadMedidaCodigo: UnidadDeMedida.KGM.Codigo,
                Usuario: "tester"
            );

            // Act
            var res = await sut.Handle(req, CancellationToken.None);

            // Assert básicos del response
            Assert.That(res.Sku, Is.EqualTo("SKU-001"));
            Assert.That(res.ColumnaNumero, Is.EqualTo(1));
            Assert.That(res.Monto, Is.EqualTo(10.50m));
            Assert.That(res.Moneda, Is.EqualTo("PEN"));
            Assert.That(res.Version, Is.GreaterThanOrEqualTo(0));
            Assert.That(res.UnidadMedidaCodigo, Is.EqualTo(UnidadDeMedida.KGM.Codigo));

            // Assert de evento de dominio
            var agg = await precioRepo.ObtenerPorProductoIdAsync(productoId);
            Assert.That(agg, Is.Not.Null);
            Assert.That(agg!.DomainEvents.OfType<PrecioFijoActualizado>().Any(), Is.True);

            var registroKg = agg!.PreciosPorUnidad.SingleOrDefault(p =>
                p.UnidadDeMedida.Codigo == UnidadDeMedida.KGM.Codigo &&
                p.ColumnaId.Equals(IdentificadorColumnaPrecio.DesdeNumero(1)));
            Assert.That(registroKg, Is.Not.Null);
            Assert.That(registroKg!.TienePrecioFijo, Is.True);

            // Assert del tenant en el evento
            var evt = agg!.DomainEvents.OfType<PrecioFijoActualizado>().Last();
            Assert.That(evt.EmpresaId, Is.EqualTo(EmpresaId.From("EMP-01")));
        }

        [Test]
        public void UpsertPrecioFijo_Falla_SiNoHayListaActiva()
        {
            // Arrange
            var empresa = Guid.NewGuid();
            var listaRepo = new InMemoryListaPrecioRepository { ListaActiva = null };
            var precioRepo = new InMemoryPrecioProductoRepository();
            var uow = new InMemoryUow();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
        var catalogo = new Moq.Mock<ICatalogoReadModel>();
        catalogo.Setup(c => c.TryGetProductoIdBySkuAsync(It.IsAny<EmpresaId>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProductoId.New());
        var sut = new UpsertPrecioFijoUseCase(precioRepo, listaRepo, uow, tenant.Object, catalogo.Object);

            var req = new UpsertPrecioFijoUseCase.Request(
                Sku: "SKU-404",
                ColumnaNumero: 1,
                Monto: 1m,
                IncluyeImpuesto: true,
                VigenciaDesde: DateTimeOffset.UtcNow.Date,
                VigenciaHasta: null,
                UnidadMedidaCodigo: UnidadDeMedida.NIU.Codigo
            );

            // Act + Assert
            Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(req, CancellationToken.None));
        }

        [Test]
        public void UpsertPrecioFijo_Falla_SiColumnaNoExiste()
        {
            // Arrange: lista sin la columna 9
            var empresa = Guid.NewGuid();
            var listaRepo = new InMemoryListaPrecioRepository
            {
                ListaActiva = CrearListaActivaConColumnaFija(numero: 1)
            };
            var precioRepo = new InMemoryPrecioProductoRepository();
            var uow = new InMemoryUow();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
        var catalogo = new Moq.Mock<ICatalogoReadModel>();
        catalogo.Setup(c => c.TryGetProductoIdBySkuAsync(It.IsAny<EmpresaId>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProductoId.New());
        var sut = new UpsertPrecioFijoUseCase(precioRepo, listaRepo, uow, tenant.Object, catalogo.Object);

            var req = new UpsertPrecioFijoUseCase.Request(
                Sku: "SKU-001",
                ColumnaNumero: 9,
                Monto: 10m,
                IncluyeImpuesto: true,
                VigenciaDesde: DateTimeOffset.UtcNow.Date,
                VigenciaHasta: null,
                UnidadMedidaCodigo: UnidadDeMedida.NIU.Codigo
            );

            // Act + Assert
            Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(req, CancellationToken.None));
        }

        [Test]
        public void UpsertPrecioFijo_Falla_SiColumnaEsPorVolumen()
        {
            // Arrange: lista con columna modo Volumen
            var empresa = Guid.NewGuid();
            var listaRepo = new InMemoryListaPrecioRepository
            {
                ListaActiva = CrearListaActivaConColumnaVolumen(numero: 2)
            };
            var precioRepo = new InMemoryPrecioProductoRepository();
            var uow = new InMemoryUow();
        var tenant = new Mock<ITenantContext>();
        tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
        var catalogo = new Moq.Mock<ICatalogoReadModel>();
        catalogo.Setup(c => c.TryGetProductoIdBySkuAsync(It.IsAny<EmpresaId>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProductoId.New());
        var sut = new UpsertPrecioFijoUseCase(precioRepo, listaRepo, uow, tenant.Object, catalogo.Object);

            var req = new UpsertPrecioFijoUseCase.Request(
                Sku: "SKU-001",
                ColumnaNumero: 2,
                Monto: 10m,
                IncluyeImpuesto: true,
                VigenciaDesde: DateTimeOffset.UtcNow.Date,
                VigenciaHasta: null,
                UnidadMedidaCodigo: UnidadDeMedida.NIU.Codigo
            );

            // Act + Assert
            Assert.ThrowsAsync<BusinessRuleException>(() => sut.Handle(req, CancellationToken.None));
        }

        [Test]
    public void UpsertPrecioFijo_FallaPorConcurrencia_SiVersionCambiaEntreLoadYSave()
        {
            // Arrange
            var empresa = Guid.NewGuid();
            var listaRepo = new InMemoryListaPrecioRepository
            {
                ListaActiva = CrearListaActivaConColumnaFija(numero: 1)
            };
            var precioRepo = new InMemoryPrecioProductoRepository { SimularConcurrencia = true };

            // Seed: ya existe el agregado con algún precio antes
            var existente = CrearPrecioProducto(empresa, null, "SKU-001");
            // Lo mutamos una vez para que tenga versión > 0
            existente.UpsertPrecioFijo(
                IdentificadorColumnaPrecio.DesdeNumero(1),
                ValorPrecio.DesdeMonto(9.99m, Moneda.PEN(), true),
                PeriodoVigencia.Crear(DateTimeOffset.UtcNow.AddDays(-10), null),
                "seed", DateTimeOffset.UtcNow.AddDays(-10)
            );
            precioRepo.Seed(existente);

            var uow = new InMemoryUow();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
        var catalogo = new Moq.Mock<ICatalogoReadModel>();
        // Usamos el mismo ProductoId del agregado existente para simular concurrencia
        catalogo.Setup(c => c.TryGetProductoIdBySkuAsync(It.IsAny<EmpresaId>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existente.ProductoId);
        var sut = new UpsertPrecioFijoUseCase(precioRepo, listaRepo, uow, tenant.Object, catalogo.Object);

            var req = new UpsertPrecioFijoUseCase.Request(
                Sku: "SKU-001",
                ColumnaNumero: 1,
                Monto: 10.50m,
                IncluyeImpuesto: true,
                VigenciaDesde: DateTimeOffset.UtcNow.Date,
                VigenciaHasta: null,
                UnidadMedidaCodigo: UnidadDeMedida.NIU.Codigo,
                Usuario: "tester"
            );

            // Act + Assert
            Assert.ThrowsAsync<ConcurrencyException>(() => sut.Handle(req, CancellationToken.None));
        }

        [Test]
        public async Task UpsertPrecioFijo_EmiteEventosConTenantYEstablecimiento_CuandoSeIndicaSucursal()
        {
            // Arrange
            var sucursalId = Guid.NewGuid();
            var listaRepo = new InMemoryListaPrecioRepository
            {
                ListaActiva = CrearListaActivaConColumnaFija(numero: 1, esBase: true)
            };

            var precioRepo = new InMemoryPrecioProductoRepository();
            var uowMock = new Moq.Mock<IUnitOfWork>();
            uowMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            var tenant = new Moq.Mock<ITenantContext>();
            var empresaTenant = EmpresaId.From("EMP-TNT");
            tenant.SetupGet(t => t.EmpresaId).Returns(empresaTenant);

            var catalogo = new Moq.Mock<ICatalogoReadModel>();
            var productoIdEst = ProductoId.New();
            catalogo
                .Setup(c => c.TryGetProductoIdBySkuAsync(It.IsAny<EmpresaId>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(productoIdEst);

            var sut = new UpsertPrecioFijoUseCase(precioRepo, listaRepo, uowMock.Object, tenant.Object, catalogo.Object);

            var req = new UpsertPrecioFijoUseCase.Request(
                Sku: "SKU-EST-001",
                ColumnaNumero: 1,
                Monto: 25.00m,
                IncluyeImpuesto: true,
                VigenciaDesde: DateTimeOffset.UtcNow.Date,
                VigenciaHasta: null,
                UnidadMedidaCodigo: UnidadDeMedida.NIU.Codigo,
                Usuario: "tester",
                Cuando: DateTimeOffset.UtcNow,
                SucursalId: sucursalId
            );

            // Act
            var res = await sut.Handle(req, CancellationToken.None);

            // Assert: commit 1 vez
            uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);

            // Assert eventos con tenant y establecimiento
            var agg = await precioRepo.ObtenerPorProductoIdAsync(productoIdEst);
            Assert.That(agg, Is.Not.Null);
            var evtFijo = agg!.DomainEvents.OfType<PrecioFijoActualizado>().LastOrDefault();
            Assert.That(evtFijo, Is.Not.Null);
            Assert.That(evtFijo!.EmpresaId, Is.EqualTo(empresaTenant));
            Assert.That(evtFijo!.EstablecimientoId, Is.EqualTo(sucursalId));
        }
    }
}
