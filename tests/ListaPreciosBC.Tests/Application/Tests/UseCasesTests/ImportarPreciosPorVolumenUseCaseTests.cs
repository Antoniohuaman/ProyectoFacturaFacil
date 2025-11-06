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
using Moq;

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
            private string? _lastLookupProductoKey;

            public bool SimularConcurrencia { get; set; }

            private static string Key(ProductoId productoId) => productoId.Value.ToString();

            public Task EliminarAsync(EmpresaId empresaId, EstablecimientoId? establecimientoId, ProductoId productoId, int? expectedVersion = null, CancellationToken ct = default)
            {
                var key = Key(productoId);
                _store.Remove(key);
                _loadedVersion.Remove(key);
                return Task.CompletedTask;
            }

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
            public int CommitCount { get; private set; }
            public Task CommitAsync(CancellationToken ct = default)
            {
                CommitCount++;
                return Task.CompletedTask;
            }
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
            var lista = ListaPrecio.CrearNueva(EmpresaId.From("EMP-01"), Guid.NewGuid(), plantilla);
            return lista;
        }

        private static PrecioProducto CrearNuevoAgg(string sku)
            => PrecioProducto.CrearNuevo(EmpresaId.From("EMP-01"), ProductoId.New());

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
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
        var catalogo = new Mock<ICatalogoReadModel>();
        var prod1 = ProductoId.New();
        var prod2 = ProductoId.New();
        catalogo.Setup(c => c.TryGetProductoIdBySkuAsync(It.IsAny<EmpresaId>(), It.Is<string>(s => s == "SKU-001"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(prod1);
        catalogo.Setup(c => c.TryGetProductoIdBySkuAsync(It.IsAny<EmpresaId>(), It.Is<string>(s => s == "SKU-002"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(prod2);
            var sut        = new ImportarPreciosPorVolumenUseCase(listaRepo, precioRepo, uow, tenant.Object, catalogo.Object);

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
            var agg1 = await precioRepo.ObtenerPorProductoIdAsync(prod1);
            var agg2 = await precioRepo.ObtenerPorProductoIdAsync(prod2);
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
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
        var catalogo = new Mock<ICatalogoReadModel>();
        var prodSku1 = ProductoId.New();
        catalogo.Setup(c => c.TryGetProductoIdBySkuAsync(It.IsAny<EmpresaId>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(prodSku1);
            var sut        = new ImportarPreciosPorVolumenUseCase(listaRepo, precioRepo, uow, tenant.Object, catalogo.Object);

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

            var agg = await precioRepo.ObtenerPorProductoIdAsync(prodSku1);
            Assert.That(PrecioVigente(agg!, 2, 1), Is.EqualTo(12m));
        }

        [Test]
        public async Task Importar_Continua_SiColumnaEsFija_RegistraError()
        {
            var listaRepo  = new InMemoryListaPrecioRepository { ListaActiva = CrearListaConBaseFijoYMayoristaVolumen() };
            var precioRepo = new InMemoryPrecioProductoRepository();
            var uow        = new InMemoryUow();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var catalogo2 = new Mock<ICatalogoReadModel>();
            catalogo2.Setup(c => c.TryGetProductoIdBySkuAsync(It.IsAny<EmpresaId>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProductoId.New());
            var sut        = new ImportarPreciosPorVolumenUseCase(listaRepo, precioRepo, uow, tenant.Object, catalogo2.Object);

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
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var catalogo3 = new Mock<ICatalogoReadModel>();
            catalogo3.Setup(c => c.TryGetProductoIdBySkuAsync(It.IsAny<EmpresaId>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProductoId.New());
            var sut        = new ImportarPreciosPorVolumenUseCase(listaRepo, precioRepo, uow, tenant.Object, catalogo3.Object);

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
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var catalogoC = new Mock<ICatalogoReadModel>();
            // Preparar agregado y retornar su ProductoId para SKU-LOCK
            var agg = PrecioProducto.CrearNuevo(EmpresaId.From("EMP-01"), ProductoId.New());
            catalogoC.Setup(c => c.TryGetProductoIdBySkuAsync(It.IsAny<EmpresaId>(), It.Is<string>(s => s == "SKU-LOCK"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg.ProductoId);
            var sut        = new ImportarPreciosPorVolumenUseCase(listaRepo, precioRepo, uow, tenant.Object, catalogoC.Object);

            // Seed con SKU para provocar conflicto de versión
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
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var catalogo4 = new Mock<ICatalogoReadModel>();
            catalogo4.Setup(c => c.TryGetProductoIdBySkuAsync(It.IsAny<EmpresaId>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProductoId.New());
            var sut        = new ImportarPreciosPorVolumenUseCase(listaRepo, precioRepo, uow, tenant.Object, catalogo4.Object);

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

        
    }
}
