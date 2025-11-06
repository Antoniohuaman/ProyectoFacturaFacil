#nullable enable
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.UseCases;
using ListaPreciosBC.Domain.Aggregates;
using ListaPreciosBC.Domain.Repositories;
using ListaPreciosBC.Domain.ValueObjects;
using NUnit.Framework;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects; // Sku, Moneda, EmpresaId, ProductoId
using SharedKernel.Application.Interfaces;   // ITenantContext
using Moq;

namespace ListaPreciosBC.Tests.UnitTests.Application
{
    [TestFixture]
    public class RegistrarPrecioBaseUseCaseTests
    {
        // ----------------- helpers -----------------
        private static IdentificadorColumnaPrecio P(byte n) => IdentificadorColumnaPrecio.DesdeNumero(n);
        private static NombreColumnaPrecio N(string s)      => NombreColumnaPrecio.Crear(s);

        private static ConfiguracionColumnaPrecio C(
            byte id, string nombre, bool esBase, bool visible, byte orden, ModoValorizacionColumna? modo = null)
        {
            var m = modo ?? ModoValorizacionColumna.Fijo;
            return ConfiguracionColumnaPrecio.Crear(P(id), N(nombre), m, esBase, visible, orden);
        }

        private static ListaPrecio ListaActivaPorDefecto(Guid idLista)
            => ListaPrecio.CrearConPlantillaPorDefecto(EmpresaId.From("EMP-01"), idLista); // P1 = Base(Fijo), visible, orden 1

        // ----------------- casos -----------------

        [Test]
        public async Task Crea_agregado_y_registra_precio_base_nuevo()
        {
            // Arrange
            var listaActiva = ListaActivaPorDefecto(Guid.NewGuid());
            var listaRepo   = new ListaPrecioRepoInMemory();
            listaRepo.SemillaActiva(listaActiva);

            var precioRepo  = new PrecioProductoRepoInMemory();
            var uow         = new UnitOfWorkInMemory();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var catalogo = new Moq.Mock<ListaPreciosBC.Application.Interfaces.ICatalogoReadModel>();
            catalogo
                .Setup(c => c.TryGetProductoIdBySkuAsync(It.IsAny<EmpresaId>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProductoId.New());
            var uc          = new RegistrarPrecioBaseUseCase(listaRepo, precioRepo, uow, tenant.Object, catalogo.Object);

            var req = new RegistrarPrecioBaseUseCase.Request(
                EmpresaId: Guid.NewGuid(),            // no usado por el UC (la lista se resuelve sin ids en tu firma)
                SucursalId: null,
                Sku: "CAP-001",
                Monto: 10.50m,
                Moneda: Moneda.PEN(),
                IncluyeImpuesto: true,
                Desde: DateTime.Today,
                Hasta: null,
                Usuario: "tester",
                Cuando: DateTimeOffset.UtcNow,
                CantidadReferenciaParaEventoBase: 1
            );

            // Act
            var res = await uc.ExecuteAsync(req, CancellationToken.None);

            // Assert respuesta
            Assert.That(res.Sku.Valor, Is.EqualTo("CAP-001"));
            Assert.That(res.ColumnaBaseNumero, Is.EqualTo((byte)1)); // P1 por plantilla por defecto
            Assert.That(res.Valor.Importe.Monto, Is.EqualTo(10.50m));
            Assert.That(res.Valor.Importe.Moneda, Is.EqualTo(Moneda.PEN()));
            Assert.That(res.Vigencia.Desde.Date, Is.EqualTo(DateTime.Today));
            Assert.That(res.Vigencia.Hasta, Is.Null);
            Assert.That(res.Version, Is.EqualTo(1)); // nuevo agregado: primera mutación

            // Persistencia y evento con tenant
            var stored = await precioRepo.ObtenerPorSkuAsync(Sku.Crear("CAP-001"), CancellationToken.None);
            Assert.That(stored, Is.Not.Null);
            Assert.That(stored!.Version, Is.EqualTo(res.Version));
            var ev = stored!.DomainEvents.OfType<ListaPreciosBC.Domain.Events.PrecioFijoActualizado>().LastOrDefault();
            Assert.That(ev, Is.Not.Null);
            Assert.That(ev!.EmpresaId, Is.EqualTo(EmpresaId.From("EMP-01")));

            // UoW
            Assert.That(uow.SaveChangesCount, Is.EqualTo(1));

            // Concurrencia (expectedVersion registrado en repo)
            Assert.That(precioRepo.LastExpectedVersion, Is.EqualTo(0));
        }

        [Test]
        public async Task Actualiza_precio_base_para_sku_existente_manteniendo_concurrencia()
        {
            // Arrange
            var listaActiva = ListaActivaPorDefecto(Guid.NewGuid());
            var listaRepo   = new ListaPrecioRepoInMemory();
            listaRepo.SemillaActiva(listaActiva);

            var sku = Sku.Crear("MOCH-123");
            var agregado = PrecioProducto.CrearNuevo(EmpresaId.From("EMP-01"), ProductoId.New()); // Version = 0
            var precioRepo = new PrecioProductoRepoInMemory();
            precioRepo.Semilla(sku.Valor, agregado);

            var uow = new UnitOfWorkInMemory();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var catalogo2 = new Moq.Mock<ListaPreciosBC.Application.Interfaces.ICatalogoReadModel>();
            catalogo2
                .Setup(c => c.TryGetProductoIdBySkuAsync(It.IsAny<EmpresaId>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProductoId.New());
            var uc  = new RegistrarPrecioBaseUseCase(listaRepo, precioRepo, uow, tenant.Object, catalogo2.Object);

            var req = new RegistrarPrecioBaseUseCase.Request(
                EmpresaId: Guid.NewGuid(),
                SucursalId: Guid.NewGuid(),
                Sku: sku.Valor,
                Monto: 99.99m,
                Moneda: Moneda.PEN(),
                IncluyeImpuesto: false,
                Desde: DateTime.Today,
                Hasta: DateTime.Today.AddDays(7),
                Usuario: "tester",
                Cuando: DateTimeOffset.UtcNow,
                CantidadReferenciaParaEventoBase: 1
            );

            var versionAntes = agregado.Version;

            // Act
            var res = await uc.ExecuteAsync(req, CancellationToken.None);

            // Assert
            Assert.That(res.Version, Is.EqualTo(versionAntes + 1));
            Assert.That(uow.SaveChangesCount, Is.EqualTo(1));
            Assert.That(precioRepo.LastExpectedVersion, Is.EqualTo(versionAntes));
        }

        [Test]
        public void Lanza_NotFound_si_no_existe_lista_activa()
        {
            // Arrange
            var listaRepo  = new ListaPrecioRepoInMemory(); // sin semilla activa
            var precioRepo = new PrecioProductoRepoInMemory();
            var uow        = new UnitOfWorkInMemory();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var catalogo3 = new Moq.Mock<ListaPreciosBC.Application.Interfaces.ICatalogoReadModel>();
            catalogo3
                .Setup(c => c.TryGetProductoIdBySkuAsync(It.IsAny<EmpresaId>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProductoId.New());
            var uc         = new RegistrarPrecioBaseUseCase(listaRepo, precioRepo, uow, tenant.Object, catalogo3.Object);

            var req = new RegistrarPrecioBaseUseCase.Request(
                EmpresaId: Guid.NewGuid(),
                SucursalId: null,
                Sku: "X",
                Monto: 1m,
                Moneda: Moneda.PEN(),
                IncluyeImpuesto: true,
                Desde: DateTime.Today,
                Hasta: null,
                Usuario: null,
                Cuando: null
            );

            // Act + Assert
            Assert.That(async () => await uc.ExecuteAsync(req, CancellationToken.None),
                        Throws.TypeOf<NotFoundException>());
        }

        [Test]
        public void Lanza_Concurrency_si_otro_proceso_actualiza_antes()
        {
            // Arrange
            var listaActiva = ListaActivaPorDefecto(Guid.NewGuid());
            var listaRepo   = new ListaPrecioRepoInMemory();
            listaRepo.SemillaActiva(listaActiva);

            var sku = Sku.Crear("R-77");
            var agregado = PrecioProducto.CrearNuevo(EmpresaId.From("EMP-01"), ProductoId.New());
            var precioRepo = new PrecioProductoRepoInMemory { ForceConcurrencyConflictNextSave = true };
            precioRepo.Semilla(sku.Valor, agregado);

            var uow = new UnitOfWorkInMemory();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var catalogo4 = new Moq.Mock<ListaPreciosBC.Application.Interfaces.ICatalogoReadModel>();
            catalogo4
                .Setup(c => c.TryGetProductoIdBySkuAsync(It.IsAny<EmpresaId>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProductoId.New());
            var uc  = new RegistrarPrecioBaseUseCase(listaRepo, precioRepo, uow, tenant.Object, catalogo4.Object);

            var req = new RegistrarPrecioBaseUseCase.Request(
                EmpresaId: Guid.NewGuid(),
                SucursalId: Guid.NewGuid(),
                Sku: sku.Valor,
                Monto: 15m,
                Moneda: Moneda.PEN(),
                IncluyeImpuesto: true,
                Desde: DateTime.Today,
                Hasta: null,
                Usuario: "tester",
                Cuando: DateTimeOffset.UtcNow
            );

            // Act + Assert
            Assert.That(async () => await uc.ExecuteAsync(req, CancellationToken.None),
                        Throws.TypeOf<ConcurrencyException>());
            Assert.That(uow.SaveChangesCount, Is.EqualTo(0));
        }

        // ----------------- dobles InMemory (sólo tests) -----------------

        private sealed class ListaPrecioRepoInMemory : IListaPrecioRepository
        {
            private ListaPrecio? _activa;

            public void SemillaActiva(ListaPrecio lista) => _activa = lista;

            public Task<ListaPrecio?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default)
                => Task.FromResult<ListaPrecio?>(_activa is not null && _activa.Id == id ? _activa : null);

            public Task<ListaPrecio?> ObtenerActivaAsync(EmpresaId empresaId, Guid? sucursalId = null, CancellationToken ct = default)
                => Task.FromResult(_activa);

            public Task GuardarAsync(ListaPrecio aggregate, EmpresaId empresaId, Guid? sucursalId, int expectedVersion, CancellationToken ct = default)
                => Task.CompletedTask;
        }

    private sealed class PrecioProductoRepoInMemory : IPrecioProductoRepository
        {
            private readonly System.Collections.Generic.Dictionary<string, PrecioProducto> _store = new();
            private readonly System.Collections.Generic.Dictionary<string, int> _versions = new();
            private readonly System.Collections.Generic.Dictionary<ProductoId, string> _skuByProducto = new();
            private string? _lastLookupSku;

            public bool ForceConcurrencyConflictNextSave { get; set; }
            public int? LastExpectedVersion { get; private set; }

            public void Semilla(string sku, PrecioProducto agregado)
            {
                _store[sku]    = agregado;
                _versions[sku] = agregado.Version;
                _skuByProducto[agregado.ProductoId] = sku;
            }

            public Task<PrecioProducto?> ObtenerPorSkuAsync(EmpresaId empresaId, Guid? sucursalId, Sku sku, CancellationToken ct = default)
            {
                _lastLookupSku = sku.Valor;
                _store.TryGetValue(_lastLookupSku, out var agg);
                return Task.FromResult(agg);
            }

            // Helper overload para asserts en tests
            public Task<PrecioProducto?> ObtenerPorSkuAsync(Sku sku, CancellationToken ct = default)
            {
                _store.TryGetValue(sku.Valor, out var agg);
                return Task.FromResult(agg);
            }

            public Task GuardarAsync(PrecioProducto aggregate, EmpresaId empresaId, Guid? sucursalId, int expectedVersion, CancellationToken ct = default)
            {
                LastExpectedVersion = expectedVersion;
                var key = (_skuByProducto.TryGetValue(aggregate.ProductoId, out var mapped) ? mapped : _lastLookupSku) ?? "UNKNOWN";

                var exists = _store.ContainsKey(key);

                // Simular “otro escritor” antes de guardar
                if (exists && ForceConcurrencyConflictNextSave)
                {
                    _versions[key] = _versions[key] + 1;
                }

                if (!exists)
                {
                    // crear: esperamos expectedVersion == 0
                    if (expectedVersion != 0)
                        throw new ConcurrencyException(nameof(PrecioProducto), key, expectedVersion, currentVersion: null);

                    _store[key]    = aggregate;
                    _versions[key] = aggregate.Version;
                    _skuByProducto[aggregate.ProductoId] = key;
                    return Task.CompletedTask;
                }

                var current = _versions[key];
                if (expectedVersion != current)
                    throw new ConcurrencyException(nameof(PrecioProducto), key, expectedVersion, current);

                _store[key]    = aggregate;
                _versions[key] = aggregate.Version;
                _skuByProducto[aggregate.ProductoId] = key;
                return Task.CompletedTask;
            }

            public Task EliminarAsync(EmpresaId empresaId, Guid? sucursalId, Sku sku, int? expectedVersion = null, CancellationToken ct = default)
            {
                var key = sku.Valor;
                if (_store.ContainsKey(key))
                {
                    if (expectedVersion.HasValue && _versions[key] != expectedVersion.Value)
                        throw new ConcurrencyException(nameof(PrecioProducto), key, expectedVersion.Value, _versions[key]);
                    _store.Remove(key);
                    _versions.Remove(key);
                }
                // idempotente: si no existe, no hace nada
                return Task.CompletedTask;
            }
        }

        

        private sealed class UnitOfWorkInMemory : ListaPreciosBC.Application.Interfaces.IUnitOfWork
        {
            public int SaveChangesCount { get; private set; }

            public Task SaveChangesAsync(CancellationToken ct = default)
            {
                SaveChangesCount++;
                return Task.CompletedTask;
            }
        }
    }
}
