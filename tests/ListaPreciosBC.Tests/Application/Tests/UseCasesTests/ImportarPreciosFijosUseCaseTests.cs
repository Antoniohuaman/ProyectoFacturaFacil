using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.Interfaces; // IUnitOfWork
using ListaPreciosBC.Application.UseCases;   // ImportarPreciosFijosUseCase
using ListaPreciosBC.Domain.Repositories;    // IListaPrecioRepository, IPrecioProductoRepository
using ListaPreciosBC.Domain.Aggregates;      // ListaPrecio, PrecioProducto
using ListaPreciosBC.Domain.ValueObjects;    // IdentificadorColumnaPrecio, NombreColumnaPrecio, ModoValorizacionColumna, ValorPrecio, PeriodoVigencia, Sku
using NUnit.Framework;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;             // Moneda
using SharedKernel.Application.Interfaces;   // ITenantContext

namespace ListaPreciosBC.Tests.Application.UseCases
{
    public class ImportarPreciosFijosUseCaseTests
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

            public Task<PrecioProducto?> ObtenerPorSkuAsync(EmpresaId empresaId, Guid? sucursalId, Sku sku, CancellationToken ct = default)
            {
                if (_store.TryGetValue(sku.Valor, out var agg))
                {
                    _loadedVersion[sku.Valor] = agg.Version;
                    return Task.FromResult<PrecioProducto?>(agg);
                }
                return Task.FromResult<PrecioProducto?>(null);
            }

            // Helper para tests
            public Task<PrecioProducto?> ObtenerPorSkuAsync(Sku sku, CancellationToken ct = default)
                => ObtenerPorSkuAsync(EmpresaId.From("TEST-EMPRESA"), null, sku, ct);

            public Task GuardarAsync(PrecioProducto aggregate, EmpresaId empresaId, Guid? sucursalId, int expectedVersion, CancellationToken ct = default)
            {
                var key = aggregate.Sku.Valor;

                if (SimularConcurrencia && _loadedVersion.TryGetValue(key, out var v))
                    _loadedVersion[key] = v + 1; // simula cambio concurrente

                if (_loadedVersion.TryGetValue(key, out var loaded) && loaded != expectedVersion)
                    throw new ConcurrencyException(
                        "PrecioProducto", // aggregate name
                        aggregate.Sku.Valor, // aggregateId
                        expectedVersion, // expectedVersion
                        loaded, // currentVersion
                        "Versión inesperada del agregado PrecioProducto.");

                _store[key] = aggregate;
                _loadedVersion[key] = aggregate.Version;
                return Task.CompletedTask;
            }

            public Task EliminarAsync(EmpresaId empresaId, Guid? sucursalId, Sku sku, int? expectedVersion = null, CancellationToken ct = default)
            {
                _store.Remove(sku.Valor);
                _loadedVersion.Remove(sku.Valor);
                return Task.CompletedTask;
            }

            // Helper para tests
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

        private static ListaPrecio CrearListaConBaseYMinoristaFijosYMayoristaVolumen()
        {
            var baseCfg = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.DesdeNumero(1),
                NombreColumnaPrecio.Crear("Base"),
                ModoValorizacionColumna.Fijo,
                esBase: true,
                visible: true,
                orden: 1
            );
            var minoristaCfg = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.DesdeNumero(2),
                NombreColumnaPrecio.Crear("Minorista"),
                ModoValorizacionColumna.Fijo,
                esBase: false,
                visible: true,
                orden: 2
            );
            var mayoristaCfg = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.DesdeNumero(3),
                NombreColumnaPrecio.Crear("Mayorista"),
                ModoValorizacionColumna.PorVolumen, // volumen (no permitido aquí)
                esBase: false,
                visible: true,
                orden: 3
            );
            var plantilla = PlantillaColumnasPrecio.Crear(new[] { baseCfg, minoristaCfg, mayoristaCfg });
            return ListaPrecio.CrearNueva(Guid.NewGuid(), plantilla);
        }

        private static PrecioProducto CrearPrecioProducto(string sku)
            => PrecioProducto.CrearNuevo(Sku.Crear(sku));

        private static bool ExistePrecio(PrecioProducto agg, byte columna, int cantidad)
        {
            var vigente = agg.ObtenerPrecioVigente(
                IdentificadorColumnaPrecio.DesdeNumero(columna),
                DateTimeOffset.UtcNow,
                cantidad);
            return vigente is not null;
        }

        // ---------------------- Tests ----------------------

        [Test]
        public async Task Importar_Exito_MultiplesSkus_MultiplesColumnasPorFila()
        {
            var listaRepo  = new InMemoryListaPrecioRepository { ListaActiva = CrearListaConBaseYMinoristaFijosYMayoristaVolumen() };
            var precioRepo = new InMemoryPrecioProductoRepository();
            var uow        = new InMemoryUow();
            var sut        = new ImportarPreciosFijosUseCase(listaRepo, precioRepo, uow, new TenantContextFake());

            var req = new ImportarPreciosFijosUseCase.Request(
                Filas: new List<ImportarPreciosFijosUseCase.Fila>
                {
                    new("SKU-001", new List<ImportarPreciosFijosUseCase.ItemPrecio>
                    {
                        new(1, 10.50m, DateTime.UtcNow.Date.AddDays(-10), null, true),
                        new(2, 11.00m, DateTime.UtcNow.Date.AddDays(-5),  null, true),
                    }),
                    new("SKU-002", new List<ImportarPreciosFijosUseCase.ItemPrecio>
                    {
                        new(1,  9.99m, DateTime.UtcNow.Date.AddDays(-1),  null, false),
                        new(2, 12.25m, DateTime.UtcNow.Date,             null, true),
                    })
                },
                Usuario: "tester"
            );

            var res = await sut.Handle(req, CancellationToken.None);

            Assert.That(res.FilasProcesadas, Is.EqualTo(2));
            Assert.That(res.ItemsProcesados, Is.EqualTo(4));
            Assert.That(res.ItemsExitosos, Is.EqualTo(4));
            Assert.That(res.ItemsFallidos, Is.EqualTo(0));
            Assert.That(res.Errores.Length, Is.EqualTo(0));
            Assert.That(res.AgregadosAfectados, Is.EqualTo(2));

            var agg1 = await precioRepo.ObtenerPorSkuAsync(Sku.Crear("SKU-001"));
            var agg2 = await precioRepo.ObtenerPorSkuAsync(Sku.Crear("SKU-002"));
            Assert.That(agg1, Is.Not.Null);
            Assert.That(agg2, Is.Not.Null);

            Assert.That(ExistePrecio(agg1!, 1, 1), Is.True);
            Assert.That(ExistePrecio(agg1!, 2, 1), Is.True);
            Assert.That(ExistePrecio(agg2!, 1, 1), Is.True);
            Assert.That(ExistePrecio(agg2!, 2, 1), Is.True);
        }

        [Test]
        public async Task Importar_Continua_SiItemEsDeColumnaVolumen_RegistraErrorYProcesaResto()
        {
            var listaRepo  = new InMemoryListaPrecioRepository { ListaActiva = CrearListaConBaseYMinoristaFijosYMayoristaVolumen() };
            var precioRepo = new InMemoryPrecioProductoRepository();
            var uow        = new InMemoryUow();
            var sut        = new ImportarPreciosFijosUseCase(listaRepo, precioRepo, uow, new TenantContextFake());

            var req = new ImportarPreciosFijosUseCase.Request(
                Filas: new List<ImportarPreciosFijosUseCase.Fila>
                {
                    new("SKU-001", new List<ImportarPreciosFijosUseCase.ItemPrecio>
                    {
                        new(1, 10m,  DateTime.UtcNow.Date, null, true), // válido
                        new(3,  9m,  DateTime.UtcNow.Date, null, true), // inválido (volumen)
                        new(2, 11m,  DateTime.UtcNow.Date, null, true), // válido
                    }),
                },
                Usuario: "tester",
                DetenerAntePrimerError: false
            );

            var res = await sut.Handle(req, CancellationToken.None);

            Assert.That(res.ItemsProcesados, Is.EqualTo(3));
            Assert.That(res.ItemsExitosos, Is.EqualTo(2));
            Assert.That(res.ItemsFallidos, Is.EqualTo(1));
            Assert.That(res.Errores.Single().ColumnaNumero, Is.EqualTo((byte)3));

            var agg = await precioRepo.ObtenerPorSkuAsync(Sku.Crear("SKU-001"));
            Assert.That(ExistePrecio(agg!, 1, 1), Is.True);
            Assert.That(ExistePrecio(agg!, 2, 1), Is.True);
        }

        [Test]
        public async Task Importar_Continua_SiColumnaNoExiste_RegistraError()
        {
            var listaRepo  = new InMemoryListaPrecioRepository { ListaActiva = CrearListaConBaseYMinoristaFijosYMayoristaVolumen() };
            var precioRepo = new InMemoryPrecioProductoRepository();
            var uow        = new InMemoryUow();
            var sut        = new ImportarPreciosFijosUseCase(listaRepo, precioRepo, uow, new TenantContextFake());

            var req = new ImportarPreciosFijosUseCase.Request(
                Filas: new List<ImportarPreciosFijosUseCase.Fila>
                {
                    new("SKU-001", new List<ImportarPreciosFijosUseCase.ItemPrecio>
                    {
                        new(9, 10m, DateTime.UtcNow.Date, null, true), // no existe
                        new(1, 11m, DateTime.UtcNow.Date, null, true), // válido
                    }),
                },
                Usuario: "tester",
                DetenerAntePrimerError: false
            );

            var res = await sut.Handle(req, CancellationToken.None);

            Assert.That(res.ItemsExitosos, Is.EqualTo(1));
            Assert.That(res.ItemsFallidos, Is.EqualTo(1));
            Assert.That(res.Errores.Length, Is.EqualTo(1));
            Assert.That(res.Errores[0].ColumnaNumero, Is.EqualTo((byte)9));

            var agg = await precioRepo.ObtenerPorSkuAsync(Sku.Crear("SKU-001"));
            Assert.That(ExistePrecio(agg!, 1, 1), Is.True);
        }

        [Test]
        public void Importar_AbortarAntePrimerError_LanzaBusinessRuleException()
        {
            var listaRepo  = new InMemoryListaPrecioRepository { ListaActiva = CrearListaConBaseYMinoristaFijosYMayoristaVolumen() };
            var precioRepo = new InMemoryPrecioProductoRepository();
            var uow        = new InMemoryUow();
            var sut        = new ImportarPreciosFijosUseCase(listaRepo, precioRepo, uow, new TenantContextFake());

            var req = new ImportarPreciosFijosUseCase.Request(
                Filas: new List<ImportarPreciosFijosUseCase.Fila>
                {
                    new("SKU-001", new List<ImportarPreciosFijosUseCase.ItemPrecio>
                    {
                        new(3, 9m, DateTime.UtcNow.Date, null, true) // inválido (volumen) => aborta
                    }),
                    new("SKU-001", new List<ImportarPreciosFijosUseCase.ItemPrecio>
                    {
                        new(1, 11m, DateTime.UtcNow.Date, null, true),
                    }),
                },
                DetenerAntePrimerError: true
            );

            Assert.That(async () => await sut.Handle(req, CancellationToken.None),
                        Throws.TypeOf<BusinessRuleException>());
        }

        [Test]
        public void Importar_Abortar_ConcurrenciaAlGuardar_LanzaConcurrencyException()
        {
            var listaRepo  = new InMemoryListaPrecioRepository { ListaActiva = CrearListaConBaseYMinoristaFijosYMayoristaVolumen() };
            var precioRepo = new InMemoryPrecioProductoRepository { SimularConcurrencia = true };
            var uow        = new InMemoryUow();
            var sut        = new ImportarPreciosFijosUseCase(listaRepo, precioRepo, uow, new TenantContextFake());

            // Seed para forzar conflicto
            var agg = PrecioProducto.CrearNuevo(Sku.Crear("SKU-LOCK"));
            agg.UpsertPrecioFijo(
                IdentificadorColumnaPrecio.DesdeNumero(1),
                ValorPrecio.DesdeMonto(9.99m, Moneda.PEN(), true),
                PeriodoVigencia.Crear(DateTime.UtcNow.AddDays(-10), null),
                "seed",
                DateTimeOffset.UtcNow.AddDays(-10)
            );
            precioRepo.Seed(agg);

            var req = new ImportarPreciosFijosUseCase.Request(
                Filas: new List<ImportarPreciosFijosUseCase.Fila>
                {
                    new("SKU-LOCK", new List<ImportarPreciosFijosUseCase.ItemPrecio>
                    {
                        new(1, 10.50m, DateTime.UtcNow.Date, null, true)
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
            var sut        = new ImportarPreciosFijosUseCase(listaRepo, precioRepo, uow, new TenantContextFake());

            var req = new ImportarPreciosFijosUseCase.Request(
                Filas: new List<ImportarPreciosFijosUseCase.Fila>
                {
                    new("SKU-001", new List<ImportarPreciosFijosUseCase.ItemPrecio>
                    {
                        new(1, 10m, DateTime.UtcNow.Date, null, true)
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
