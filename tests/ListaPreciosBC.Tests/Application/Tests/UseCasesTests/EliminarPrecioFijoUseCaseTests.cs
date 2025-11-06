using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.Interfaces; // IUnitOfWork
using ListaPreciosBC.Application.UseCases;   // EliminarPrecioFijoUseCase
using ListaPreciosBC.Domain.Repositories;    // IListaPrecioRepository, IPrecioProductoRepository
using ListaPreciosBC.Domain.Aggregates;      // ListaPrecio, PrecioProducto
using ListaPreciosBC.Domain.ValueObjects;    // Sku, IdentificadorColumnaPrecio, PeriodoVigencia, ValorPrecio, ModoValorizacionColumna
using NUnit.Framework;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;             // Moneda (para seed), ProductoId, EmpresaId
using SharedKernel.Application.Interfaces;   // ITenantContext
using Moq;

namespace ListaPreciosBC.Tests.Application.UseCases
{
    public class EliminarPrecioFijoUseCaseTests
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
            private string? _lastLookupSku;
            public bool SimularConcurrencia { get; set; }

            public Task<PrecioProducto?> ObtenerPorSkuAsync(EmpresaId empresaId, Guid? sucursalId, Sku sku, CancellationToken ct = default)
            {
                _lastLookupSku = sku.Valor;
                if (_store.TryGetValue(_lastLookupSku, out var agg))
                {
                    _loadedVersion[_lastLookupSku] = agg.Version;
                    return Task.FromResult<PrecioProducto?>(agg);
                }
                return Task.FromResult<PrecioProducto?>(null);
            }
            // Helper para asserts en tests
            public Task<PrecioProducto?> ObtenerPorSkuAsync(Sku sku, CancellationToken ct = default)
            {
                _store.TryGetValue(sku.Valor, out var agg);
                return Task.FromResult<PrecioProducto?>(agg);
            }
                public Task EliminarAsync(EmpresaId empresaId, Guid? sucursalId, Sku sku, int? expectedVersion = null, CancellationToken ct = default)
                {
                    var key = sku.Valor;
                    if (_store.TryGetValue(key, out var agg))
                    {
                        if (expectedVersion.HasValue && agg.Version != expectedVersion.Value)
                            throw new ConcurrencyException(
                                "Versión inesperada del agregado al eliminar.",
                                sku.Valor,
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

            public Task GuardarAsync(PrecioProducto aggregate, EmpresaId empresaId, Guid? sucursalId, int expectedVersion, CancellationToken ct = default)
            {
                if (string.IsNullOrEmpty(_lastLookupSku))
                    throw new InvalidOperationException("Debe consultarse por SKU antes de guardar para conocer la llave de persistencia.");
                var key = _lastLookupSku;

                if (SimularConcurrencia && _loadedVersion.TryGetValue(key, out var v))
                {
                    _loadedVersion[key] = v + 1; // simula cambio externo entre load y save
                }

                if (_loadedVersion.TryGetValue(key, out var loaded) && loaded != expectedVersion)
                    throw new ConcurrencyException(
                        "Versión inesperada del agregado.",
                        key,                // identificador (SKU)
                        expectedVersion,     // expectedVersion
                        aggregate.Version,   // actualVersion
                        null,                // propertyName
                        null                 // additionalData
                    );

                _store[key] = aggregate;
                _loadedVersion[key] = aggregate.Version;
                return Task.CompletedTask;
            }

            public void Seed(string sku, PrecioProducto agg)
            {
                _store[sku] = agg;
                _loadedVersion[sku] = agg.Version;
            }

            // Implementación faltante para la interfaz
            public Task<IEnumerable<PrecioProducto>> ObtenerPorSkusAsync(IEnumerable<Sku> skus, CancellationToken ct = default)
            {
                var result = skus.Select(s => _store.TryGetValue(s.Valor, out var agg) ? agg : null).Where(x => x != null).Cast<PrecioProducto>();
                return Task.FromResult(result);
            }
        }

        private sealed class InMemoryUow : IUnitOfWork
        {
            public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
        }

        // ---------------------- Builders (ajusta si tu API difiere) ----------------------

        private static ListaPrecio CrearListaActivaConColumnaFija(byte numero, bool esBase = true, bool visible = true)
        {
            var cfg = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.DesdeNumero(numero),
                NombreColumnaPrecio.Crear(esBase ? "Base" : $"Col{numero}"),
                ModoValorizacionColumna.Fijo,
                esBase,
                visible,
                numero
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
                true,
                true,
                1
            );
            var volumenCfg = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.DesdeNumero(numero),
                NombreColumnaPrecio.Crear($"Vol{numero}"),
                ModoValorizacionColumna.PorVolumen,
                false,
                visible,
                numero
            );
            var plantilla = PlantillaColumnasPrecio.Crear(new[] { baseCfg, volumenCfg });
            return ListaPrecio.CrearNueva(EmpresaId.From("EMP-01"), Guid.NewGuid(), plantilla);
        }

        private static PrecioProducto CrearPrecioProducto(string sku)
        {
            var empresaId = EmpresaId.From("EMP-01");
            var productoId = ProductoId.New();
            return PrecioProducto.CrearNuevo(empresaId, productoId);
        }

        private static bool ExistePrecioFijo(PrecioProducto agg, byte columnaNumero)
        {
            var colId = IdentificadorColumnaPrecio.DesdeNumero(columnaNumero);
            var precio = agg.ObtenerPrecioVigente(colId, DateTimeOffset.UtcNow.Date, cantidad: 1);
            return precio is not null;
        }

        // ---------------------- Tests ----------------------

        [Test]
        public async Task EliminarPrecioFijo_Exito_EliminaPrecioYPersiste()
        {
            var listaRepo = new InMemoryListaPrecioRepository
            {
                ListaActiva = CrearListaActivaConColumnaFija(numero: 1, esBase: true)
            };

            var precioRepo = new InMemoryPrecioProductoRepository();
            var sku = "SKU-001";

            // Seed: SKU con precio fijo vigente en columna 1
            var existente = CrearPrecioProducto(sku);
            existente.UpsertPrecioFijo(
                IdentificadorColumnaPrecio.DesdeNumero(1),
                ValorPrecio.DesdeMonto(10.50m, Moneda.PEN(), true),
                PeriodoVigencia.Crear(DateTimeOffset.UtcNow.AddDays(-30), null),
                "seed",
                DateTimeOffset.UtcNow.AddDays(-30)
            );
            precioRepo.Seed(sku, existente);

            var uow = new InMemoryUow();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var sut = new EliminarPrecioFijoUseCase(precioRepo, listaRepo, uow, tenant.Object);

            var req = new EliminarPrecioFijoUseCase.Request(
                Sku: sku,
                ColumnaNumero: 1,
                LanzarSiNoExiste: true,
                Usuario: "tester"
            );

            var res = await sut.Handle(req, CancellationToken.None);

            Assert.That(res.Sku, Is.EqualTo(sku));
            Assert.That(res.ColumnaNumero, Is.EqualTo(1));
            Assert.That(res.Version, Is.GreaterThanOrEqualTo(0));

            // Verificación de efecto: el precio vigente ya no debe existir
            var post = await precioRepo.ObtenerPorSkuAsync(Sku.Crear(sku));
            Assert.That(post, Is.Not.Null);
            Assert.That(ExistePrecioFijo(post!, 1), Is.False);
        }

        [Test]
        public void EliminarPrecioFijo_Falla_SiNoHayListaActiva()
        {
            var listaRepo = new InMemoryListaPrecioRepository { ListaActiva = null };
            var precioRepo = new InMemoryPrecioProductoRepository();
            var uow = new InMemoryUow();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var sut = new EliminarPrecioFijoUseCase(precioRepo, listaRepo, uow, tenant.Object);

            var req = new EliminarPrecioFijoUseCase.Request(
                Sku: "SKU-404",
                ColumnaNumero: 1,
                LanzarSiNoExiste: true
            );

            Assert.That(async () => await sut.Handle(req, CancellationToken.None), Throws.TypeOf<NotFoundException>());
        }

        [Test]
        public void EliminarPrecioFijo_Falla_SiColumnaNoExiste()
        {
            var listaRepo = new InMemoryListaPrecioRepository
            {
                ListaActiva = CrearListaActivaConColumnaFija(numero: 1)
            };

            var precioRepo = new InMemoryPrecioProductoRepository();
            var uow = new InMemoryUow();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var sut = new EliminarPrecioFijoUseCase(precioRepo, listaRepo, uow, tenant.Object);

            var req = new EliminarPrecioFijoUseCase.Request(
                Sku: "SKU-001",
                ColumnaNumero: 9,
                LanzarSiNoExiste: true
            );

            Assert.That(async () => await sut.Handle(req, CancellationToken.None), Throws.TypeOf<NotFoundException>());
        }

        [Test]
        public void EliminarPrecioFijo_Falla_SiColumnaEsPorVolumen()
        {
            var listaRepo = new InMemoryListaPrecioRepository
            {
                ListaActiva = CrearListaActivaConColumnaVolumen(numero: 2)
            };

            var precioRepo = new InMemoryPrecioProductoRepository();
            var uow = new InMemoryUow();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var sut = new EliminarPrecioFijoUseCase(precioRepo, listaRepo, uow, tenant.Object);

            var req = new EliminarPrecioFijoUseCase.Request(
                Sku: "SKU-001",
                ColumnaNumero: 2,
                LanzarSiNoExiste: true
            );

            Assert.That(async () => await sut.Handle(req, CancellationToken.None), Throws.TypeOf<BusinessRuleException>());
        }

        [Test]
        public async Task EliminarPrecioFijo_Idempotente_SiNoExisteYFlagEsFalse()
        {
            var listaRepo = new InMemoryListaPrecioRepository
            {
                ListaActiva = CrearListaActivaConColumnaFija(numero: 1)
            };

            var precioRepo = new InMemoryPrecioProductoRepository(); // no seed

            var uow = new InMemoryUow();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var sut = new EliminarPrecioFijoUseCase(precioRepo, listaRepo, uow, tenant.Object);

            var req = new EliminarPrecioFijoUseCase.Request(
                Sku: "SKU-NEW",
                ColumnaNumero: 1,
                LanzarSiNoExiste: false // idempotente
            );

            var res = await sut.Handle(req, CancellationToken.None);
            Assert.That(res.Sku, Is.EqualTo("SKU-NEW"));
            Assert.That(res.ColumnaNumero, Is.EqualTo(1));
            Assert.That(res.Version, Is.EqualTo(0)); // no se creó/mutó agregado
        }

        [Test]
        public void EliminarPrecioFijo_FallaPorConcurrencia_SiVersionCambiaEntreLoadYSave()
        {
            var listaRepo = new InMemoryListaPrecioRepository
            {
                ListaActiva = CrearListaActivaConColumnaFija(numero: 1)
            };

            var precioRepo = new InMemoryPrecioProductoRepository { SimularConcurrencia = true };

            // Seed: SKU con precio fijo vigente
            var sku = "SKU-001";
            var existente = CrearPrecioProducto(sku);
            existente.UpsertPrecioFijo(
                IdentificadorColumnaPrecio.DesdeNumero(1),
                ValorPrecio.DesdeMonto(9.99m, Moneda.PEN(), true),
                PeriodoVigencia.Crear(DateTimeOffset.UtcNow.AddDays(-10), null),
                "seed",
                DateTimeOffset.UtcNow.AddDays(-10)
            );
            precioRepo.Seed(sku, existente);

            var uow = new InMemoryUow();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var sut = new EliminarPrecioFijoUseCase(precioRepo, listaRepo, uow, tenant.Object);

            var req = new EliminarPrecioFijoUseCase.Request(
                Sku: sku,
                ColumnaNumero: 1,
                LanzarSiNoExiste: true,
                Usuario: "tester"
            );

            Assert.That(async () => await sut.Handle(req, CancellationToken.None), Throws.TypeOf<ConcurrencyException>());
        }
        
    }
}
