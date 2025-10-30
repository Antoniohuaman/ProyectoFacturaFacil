using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.Interfaces; // IUnitOfWork
using ListaPreciosBC.Application.UseCases;   // ImportarPreciosPorVolumenUseCase
using ListaPreciosBC.Domain.Repositories;    // IListaPrecioRepository, IPrecioProductoRepository
using ListaPreciosBC.Domain.Aggregates;      // ListaPrecio, PrecioProducto
using ListaPreciosBC.Domain.ValueObjects;    // IdentificadorColumnaPrecio, NombreColumnaPrecio, ModoValorizacionColumna, TramoVolumen, MatrizVolumen, ValorPrecio
using NUnit.Framework;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;             // Moneda, Sku
using SharedKernel.Application.Interfaces;   // ITenantContext

namespace ListaPreciosBC.Tests.Application.UseCases
{
    public class ImportarPreciosPorVolumenUseCaseTests
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

            public void Seed(ListaPrecio lista) => ListaActiva = lista;
        }

        private sealed class InMemoryPrecioProductoRepository : IPrecioProductoRepository
        {
            private readonly Dictionary<string, PrecioProducto> _store = new();
            private readonly Dictionary<string, int> _loadedVersion = new();

            public bool SimularConcurrencia { get; set; }

            public Task EliminarAsync(EmpresaId empresaId, Guid? sucursalId, Sku sku, int? expectedVersion = null, CancellationToken ct = default)
            {
                _store.Remove(sku.Valor);
                _loadedVersion.Remove(sku.Valor);
                return Task.CompletedTask;
            }

            public Task<PrecioProducto?> ObtenerPorSkuAsync(EmpresaId empresaId, Guid? sucursalId, Sku sku, CancellationToken ct = default)
            {
                if (_store.TryGetValue(sku.Valor, out var agg))
                {
                    _loadedVersion[sku.Valor] = agg.Version;
                    return Task.FromResult<PrecioProducto?>(agg);
                }
                return Task.FromResult<PrecioProducto?>(null);
            }

            // Helpers para tests
            public Task<PrecioProducto?> ObtenerPorSkuAsync(Sku sku, CancellationToken ct = default)
                => ObtenerPorSkuAsync(EmpresaId.From("TEST-EMPRESA"), null, sku, ct);

            public Task GuardarAsync(PrecioProducto aggregate, EmpresaId empresaId, Guid? sucursalId, int expectedVersion, CancellationToken ct = default)
            {
                var key = aggregate.Sku.Valor;

                if (SimularConcurrencia && _loadedVersion.TryGetValue(key, out var v))
                    _loadedVersion[key] = v + 1; // simula cambio concurrente

                if (_loadedVersion.TryGetValue(key, out var loaded) && loaded != expectedVersion)
                    throw new ConcurrencyException(
                        "PrecioProducto", // aggregate
                        key,               // aggregateId
                        expectedVersion,   // expectedVersion
                        loaded,            // currentVersion
                        "Versión inesperada del agregado PrecioProducto.");

                _store[key] = aggregate;
                _loadedVersion[key] = aggregate.Version;
                return Task.CompletedTask;
            }

            // Helper delete legacy signature
            public Task EliminarAsync(Sku sku, int? expectedVersion = null, CancellationToken ct = default)
                => EliminarAsync(EmpresaId.From("TEST-EMPRESA"), null, sku, expectedVersion, ct);

            public void Seed(PrecioProducto agg)
            {
                var key = agg.Sku.Valor;
                _store[key] = agg;
                _loadedVersion[key] = agg.Version;
            }
        }

        private sealed class InMemoryUow : IUnitOfWork
        {
            public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
        }

        // ---------------------- Builders con invariantes ----------------------

        private static ListaPrecio CrearListaConBaseFijoYMayoristaVolumen()
        {
            var baseCfg = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.DesdeNumero(1),
                NombreColumnaPrecio.Crear("Base"),
                ModoValorizacionColumna.Fijo,
                esBase: true,
                visible: true,
                orden: 1
            );
            var mayoristaCfg = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.DesdeNumero(2),
                NombreColumnaPrecio.Crear("Mayorista"),
                ModoValorizacionColumna.PorVolumen,
                esBase: false,
                visible: true,
                orden: 2
            );
            var minoristaCfg = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.DesdeNumero(3),
                NombreColumnaPrecio.Crear("Minorista"),
                ModoValorizacionColumna.Fijo,
                esBase: false,
                visible: true,
                orden: 3
            );
            var plantilla = PlantillaColumnasPrecio.Crear(new[] { baseCfg, mayoristaCfg, minoristaCfg });
            var lista = ListaPrecio.CrearNueva(Guid.NewGuid(), plantilla);
            return lista;
        }

        private static PrecioProducto CrearNuevoAgg(string sku)
            => PrecioProducto.CrearNuevo(Sku.Crear(sku));

        private static decimal? PrecioVigente(PrecioProducto agg, byte col, int cantidad)
        {
            var r = agg.ObtenerPrecioVigente(IdentificadorColumnaPrecio.DesdeNumero(col), DateTimeOffset.UtcNow, cantidad);
            return r?.Valor.Monto;
        }

        // ---------------------- Tests ----------------------

        [Test]
        public async Task Importar_Exito_MultiplesSkus_MatrizDeVolumen()
        {
            var listaRepo  = new InMemoryListaPrecioRepository { ListaActiva = CrearListaConBaseFijoYMayoristaVolumen() };
            var precioRepo = new InMemoryPrecioProductoRepository();
            var uow        = new InMemoryUow();
            var sut        = new ImportarPreciosPorVolumenUseCase(listaRepo, precioRepo, uow, new TenantContextFake());

            var req = new ImportarPreciosPorVolumenUseCase.Request(
                Filas: new List<ImportarPreciosPorVolumenUseCase.Fila>
                {
                    new("SKU-001", new List<ImportarPreciosPorVolumenUseCase.ItemColumna>
                    {
                        new(2, new List<ImportarPreciosPorVolumenUseCase.Rango>
                        {
                            new(1,  9, 12m, true),
                            new(10, null, 10m, true),
                        })
                    }),
                    new("SKU-002", new List<ImportarPreciosPorVolumenUseCase.ItemColumna>
                    {
                        new(2, new List<ImportarPreciosPorVolumenUseCase.Rango>
                        {
                            new(1,  4, 8m, true),
                            new(5,  null, 7.5m, true),
                        })
                    }),
                },
                Usuario: "tester"
            );

            var res = await sut.Handle(req, CancellationToken.None);

            Assert.That(res.FilasProcesadas, Is.EqualTo(2));
            Assert.That(res.ItemsProcesados, Is.EqualTo(2));
            Assert.That(res.ItemsExitosos, Is.EqualTo(2));
            Assert.That(res.ItemsFallidos, Is.EqualTo(0));
            Assert.That(res.AgregadosAfectados, Is.EqualTo(2));

            // Verificar precios
            var agg1 = await precioRepo.ObtenerPorSkuAsync(Sku.Crear("SKU-001"));
            var agg2 = await precioRepo.ObtenerPorSkuAsync(Sku.Crear("SKU-002"));
            Assert.That(PrecioVigente(agg1!, 2, 1),  Is.EqualTo(12m));
            Assert.That(PrecioVigente(agg1!, 2, 10), Is.EqualTo(10m));
            Assert.That(PrecioVigente(agg2!, 2, 3),  Is.EqualTo(8m));
            Assert.That(PrecioVigente(agg2!, 2, 5),  Is.EqualTo(7.5m));
        }

        [Test]
        public async Task Importar_Continua_SiColumnaNoExiste_RegistraErrorYProcesaResto()
        {
            var listaRepo  = new InMemoryListaPrecioRepository { ListaActiva = CrearListaConBaseFijoYMayoristaVolumen() };
            var precioRepo = new InMemoryPrecioProductoRepository();
            var uow        = new InMemoryUow();
            var sut        = new ImportarPreciosPorVolumenUseCase(listaRepo, precioRepo, uow, new TenantContextFake());

            var req = new ImportarPreciosPorVolumenUseCase.Request(
                Filas: new List<ImportarPreciosPorVolumenUseCase.Fila>
                {
                    new("SKU-001", new List<ImportarPreciosPorVolumenUseCase.ItemColumna>
                    {
                        new(9, new List<ImportarPreciosPorVolumenUseCase.Rango> { new(1, null, 10m, true) }), // no existe
                        new(2, new List<ImportarPreciosPorVolumenUseCase.Rango> { new(1, 9, 12m, true), new(10, null, 10m, true) }), // válido
                    })
                },
                Usuario: "tester",
                DetenerAntePrimerError: false
            );

            var res = await sut.Handle(req, CancellationToken.None);

            Assert.That(res.ItemsProcesados, Is.EqualTo(2));
            Assert.That(res.ItemsExitosos, Is.EqualTo(1));
            Assert.That(res.ItemsFallidos, Is.EqualTo(1));
            Assert.That(res.Errores.Single().ColumnaNumero, Is.EqualTo((byte)9));

            var agg = await precioRepo.ObtenerPorSkuAsync(Sku.Crear("SKU-001"));
            Assert.That(PrecioVigente(agg!, 2, 1), Is.EqualTo(12m));
        }

        [Test]
        public async Task Importar_Continua_SiColumnaEsFija_RegistraError()
        {
            var listaRepo  = new InMemoryListaPrecioRepository { ListaActiva = CrearListaConBaseFijoYMayoristaVolumen() };
            var precioRepo = new InMemoryPrecioProductoRepository();
            var uow        = new InMemoryUow();
            var sut        = new ImportarPreciosPorVolumenUseCase(listaRepo, precioRepo, uow, new TenantContextFake());

            var req = new ImportarPreciosPorVolumenUseCase.Request(
                Filas: new List<ImportarPreciosPorVolumenUseCase.Fila>
                {
                    new("SKU-001", new List<ImportarPreciosPorVolumenUseCase.ItemColumna>
                    {
                        new(3, new List<ImportarPreciosPorVolumenUseCase.Rango> { new(1, null, 9m, true) }) // #3 es Fijo
                    })
                },
                DetenerAntePrimerError: false
            );

            var res = await sut.Handle(req, CancellationToken.None);

            Assert.That(res.ItemsExitosos, Is.EqualTo(0));
            Assert.That(res.ItemsFallidos, Is.EqualTo(1));
            Assert.That(res.Errores.Single().ColumnaNumero, Is.EqualTo((byte)3));
        }

        [Test]
        public async Task Importar_Continua_RangoInvalido_RegistraError()
        {
            var listaRepo  = new InMemoryListaPrecioRepository { ListaActiva = CrearListaConBaseFijoYMayoristaVolumen() };
            var precioRepo = new InMemoryPrecioProductoRepository();
            var uow        = new InMemoryUow();
            var sut        = new ImportarPreciosPorVolumenUseCase(listaRepo, precioRepo, uow, new TenantContextFake());

            var req = new ImportarPreciosPorVolumenUseCase.Request(
                Filas: new List<ImportarPreciosPorVolumenUseCase.Fila>
                {
                    new("SKU-001", new List<ImportarPreciosPorVolumenUseCase.ItemColumna>
                    {
                        new(2, new List<ImportarPreciosPorVolumenUseCase.Rango> { new(5, 3, 10m, true) }) // hasta < desde
                    })
                },
                DetenerAntePrimerError: false
            );

            var res = await sut.Handle(req, CancellationToken.None);

            Assert.That(res.ItemsExitosos, Is.EqualTo(0));
            Assert.That(res.ItemsFallidos, Is.EqualTo(1));
            Assert.That(res.Errores.Length, Is.EqualTo(1));
        }

        [Test]
        public void Importar_Abortar_ConcurrenciaAlGuardar_LanzaConcurrencyException()
        {
            var listaRepo  = new InMemoryListaPrecioRepository { ListaActiva = CrearListaConBaseFijoYMayoristaVolumen() };
            var precioRepo = new InMemoryPrecioProductoRepository { SimularConcurrencia = true };
            var uow        = new InMemoryUow();
            var sut        = new ImportarPreciosPorVolumenUseCase(listaRepo, precioRepo, uow, new TenantContextFake());

            // Seed con SKU para provocar conflicto de versión
            var agg = PrecioProducto.CrearNuevo(Sku.Crear("SKU-LOCK"));
            precioRepo.Seed(agg);

            var req = new ImportarPreciosPorVolumenUseCase.Request(
                Filas: new List<ImportarPreciosPorVolumenUseCase.Fila>
                {
                    new("SKU-LOCK", new List<ImportarPreciosPorVolumenUseCase.ItemColumna>
                    {
                        new(2, new List<ImportarPreciosPorVolumenUseCase.Rango> { new(1, 9, 11m, true), new(10, null, 9m, true) })
                    })
                },
                DetenerAntePrimerError: true
            );

            Assert.That(async () => await sut.Handle(req, CancellationToken.None),
                        Throws.TypeOf<ConcurrencyException>());
        }

        [Test]
        public void Importar_Falla_SiNoHayListaActiva()
        {
            var listaRepo  = new InMemoryListaPrecioRepository { ListaActiva = null };
            var precioRepo = new InMemoryPrecioProductoRepository();
            var uow        = new InMemoryUow();
            var sut        = new ImportarPreciosPorVolumenUseCase(listaRepo, precioRepo, uow, new TenantContextFake());

            var req = new ImportarPreciosPorVolumenUseCase.Request(
                Filas: new List<ImportarPreciosPorVolumenUseCase.Fila>
                {
                    new("SKU-001", new List<ImportarPreciosPorVolumenUseCase.ItemColumna>
                    {
                        new(2, new List<ImportarPreciosPorVolumenUseCase.Rango> { new(1, null, 10m, true) })
                    })
                }
            );

            Assert.That(async () => await sut.Handle(req, CancellationToken.None),
                        Throws.TypeOf<NotFoundException>());
        }

        private sealed class TenantContextFake : ITenantContext
        {
            public TenantId TenantId { get; } = TenantId.New();
            public EmpresaId EmpresaId { get; } = EmpresaId.From("TEST-EMPRESA");
        }
    }
}
