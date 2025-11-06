using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.Interfaces; // IUnitOfWork
using ListaPreciosBC.Application.UseCases;   // ImportarPreciosBasicoUseCase
using ListaPreciosBC.Domain.Repositories;    // IListaPrecioRepository, IPrecioProductoRepository
using ListaPreciosBC.Domain.Aggregates;      // ListaPrecio, PrecioProducto
using ListaPreciosBC.Domain.ValueObjects;    // IdentificadorColumnaPrecio, NombreColumnaPrecio, ModoValorizacionColumna, ValorPrecio, PeriodoVigencia, MatrizVolumen, TramoVolumen
using NUnit.Framework;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;             // Moneda, Sku
using SharedKernel.Application.Interfaces;   // ITenantContext
using Moq;

namespace ListaPreciosBC.Tests.Application.UseCases
{
    public class ImportarPreciosBasicoUseCaseTests
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
                    // Simular cambio concurrente entre load y save
                    _loadedVersion[key] = v + 1;
                }

                if (_loadedVersion.TryGetValue(key, out var loaded) && loaded != expectedVersion)
                    throw new ConcurrencyException(
                        "Versión inesperada del agregado PrecioProducto.",
                        aggregateId: key,
                        expectedVersion: expectedVersion,
                        currentVersion: aggregate.Version
                    );

                _store[key] = aggregate;
                _loadedVersion[key] = aggregate.Version;
                return Task.CompletedTask;
            }

            public Task EliminarAsync(EmpresaId empresaId, EstablecimientoId? establecimientoId, ProductoId productoId, int? expectedVersion = null, CancellationToken ct = default)
            {
                var key = Key(productoId);
                _store.Remove(key);
                _loadedVersion.Remove(key);
                return Task.CompletedTask;
            }

            public void Seed(PrecioProducto agg)
            {
                var key = Key(agg.ProductoId);
                _store[key] = agg;
                _loadedVersion[key] = agg.Version;
            }

            // Helpers para tests
            public Task<PrecioProducto?> ObtenerPorProductoIdAsync(ProductoId productoId, CancellationToken ct = default)
            {
                _store.TryGetValue(Key(productoId), out var agg);
                return Task.FromResult(agg);
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
                ModoValorizacionColumna.PorVolumen, // volumen (no permitido en importación básica)
                esBase: false,
                visible: true,
                orden: 3
            );
            var plantilla = PlantillaColumnasPrecio.Crear(new[] { baseCfg, minoristaCfg, mayoristaCfg });
            return ListaPrecio.CrearNueva(EmpresaId.From("EMP-01"), Guid.NewGuid(), plantilla);
        }

        private static PrecioProducto CrearPrecioProducto(string sku)
            => PrecioProducto.CrearNuevo(EmpresaId.From("EMP-01"), ProductoId.New());

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
        public async Task Importar_Exito_MultiplesFilas_MultiplesSkus()
        {
            var listaRepo  = new InMemoryListaPrecioRepository { ListaActiva = CrearListaConBaseYMinoristaFijosYMayoristaVolumen() };
            var precioRepo = new InMemoryPrecioProductoRepository();
            var uow        = new InMemoryUow();
            var tenant     = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var catalogo = new Moq.Mock<ListaPreciosBC.Application.Interfaces.ICatalogoReadModel>();
            var prod1 = ProductoId.New();
            var prod2 = ProductoId.New();
            catalogo.Setup(c => c.TryGetProductoIdBySkuAsync(It.IsAny<EmpresaId>(), It.Is<string>(s => s == "SKU-001"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(prod1);
            catalogo.Setup(c => c.TryGetProductoIdBySkuAsync(It.IsAny<EmpresaId>(), It.Is<string>(s => s == "SKU-002"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(prod2);
            var sut        = new ImportarPreciosBasicoUseCase(listaRepo, precioRepo, uow, tenant.Object, catalogo.Object);

            var req = new ImportarPreciosBasicoUseCase.Request(
                Filas: new List<ImportarPreciosBasicoUseCase.Fila>
                {
                    new("SKU-001", 1, 10.50m, DateTime.UtcNow.Date.AddDays(-10), null, true), // Base Fijo
                    new("SKU-002", 2, 12.00m, DateTime.UtcNow.Date.AddDays(-5),  null, true), // Minorista Fijo
                },
                Usuario: "tester"
            );

            var res = await sut.Handle(req, CancellationToken.None);

            Assert.That(res.Procesadas, Is.EqualTo(2));
            Assert.That(res.Exitosas, Is.EqualTo(2));
            Assert.That(res.Fallidas, Is.EqualTo(0));
            Assert.That(res.AgregadosAfectados, Is.EqualTo(2));

            // Verificar efecto: ambos SKUs tienen precio vigente en sus columnas
            var agg1 = await precioRepo.ObtenerPorProductoIdAsync(prod1);
            var agg2 = await precioRepo.ObtenerPorProductoIdAsync(prod2);
            Assert.That(agg1, Is.Not.Null);
            Assert.That(agg2, Is.Not.Null);

            Assert.That(ExistePrecio(agg1!, 1, 1), Is.True);
            Assert.That(ExistePrecio(agg2!, 2, 1), Is.True);
        }

        [Test]
        public async Task Importar_Continua_SiColumnaNoExiste_RegistraError()
        {
            var listaRepo  = new InMemoryListaPrecioRepository { ListaActiva = CrearListaConBaseYMinoristaFijosYMayoristaVolumen() };
            var precioRepo = new InMemoryPrecioProductoRepository();
            var uow        = new InMemoryUow();
            var tenant     = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var catalogo2 = new Moq.Mock<ListaPreciosBC.Application.Interfaces.ICatalogoReadModel>();
            var prodSku1 = ProductoId.New();
            catalogo2.Setup(c => c.TryGetProductoIdBySkuAsync(It.IsAny<EmpresaId>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(prodSku1);
            var sut        = new ImportarPreciosBasicoUseCase(listaRepo, precioRepo, uow, tenant.Object, catalogo2.Object);

            var req = new ImportarPreciosBasicoUseCase.Request(
                Filas: new List<ImportarPreciosBasicoUseCase.Fila>
                {
                    new("SKU-001", 9, 10m, DateTime.UtcNow.Date, null, true), // no existe
                    new("SKU-001", 1, 11m, DateTime.UtcNow.Date, null, true), // válida
                },
                Usuario: "tester",
                DetenerAntePrimerError: false
            );

            var res = await sut.Handle(req, CancellationToken.None);

            Assert.That(res.Procesadas, Is.EqualTo(2));
            Assert.That(res.Exitosas, Is.EqualTo(1));
            Assert.That(res.Fallidas, Is.EqualTo(1));
            Assert.That(res.Errores.Length, Is.EqualTo(1));
            Assert.That(res.Errores[0].ColumnaNumero, Is.EqualTo((byte)9));

            var agg = await precioRepo.ObtenerPorProductoIdAsync(prodSku1);
            Assert.That(ExistePrecio(agg!, 1, 1), Is.True); // se registró la válida
        }

        [Test]
        public async Task Importar_Continua_SiColumnaEsVolumen_RegistraError()
        {
            var listaRepo  = new InMemoryListaPrecioRepository { ListaActiva = CrearListaConBaseYMinoristaFijosYMayoristaVolumen() };
            var precioRepo = new InMemoryPrecioProductoRepository();
            var uow        = new InMemoryUow();
            var tenant     = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var catalogo3 = new Moq.Mock<ListaPreciosBC.Application.Interfaces.ICatalogoReadModel>();
            var prodSku = ProductoId.New();
            catalogo3.Setup(c => c.TryGetProductoIdBySkuAsync(It.IsAny<EmpresaId>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(prodSku);
            var sut        = new ImportarPreciosBasicoUseCase(listaRepo, precioRepo, uow, tenant.Object, catalogo3.Object);

            var req = new ImportarPreciosBasicoUseCase.Request(
                Filas: new List<ImportarPreciosBasicoUseCase.Fila>
                {
                    new("SKU-001", 1, 10m, DateTime.UtcNow.Date, null, true), // válido (Fijo)
                    new("SKU-001", 3,  9m, DateTime.UtcNow.Date, null, true), // inválido (Volumen)
                },
                Usuario: "tester",
                DetenerAntePrimerError: false
            );

            var res = await sut.Handle(req, CancellationToken.None);

            Assert.That(res.Exitosas, Is.EqualTo(1));
            Assert.That(res.Fallidas, Is.EqualTo(1));
            Assert.That(res.Errores.Single().ColumnaNumero, Is.EqualTo((byte)3));

            var agg = await precioRepo.ObtenerPorProductoIdAsync(prodSku);
            Assert.That(ExistePrecio(agg!, 1, 1), Is.True);
        }

        [Test]
        public void Importar_AbortarAntePrimerError_LanzaExcepcion()
        {
            var listaRepo  = new InMemoryListaPrecioRepository { ListaActiva = CrearListaConBaseYMinoristaFijosYMayoristaVolumen() };
            var precioRepo = new InMemoryPrecioProductoRepository();
            var uow        = new InMemoryUow();
            var tenant     = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var catalogoX = new Moq.Mock<ListaPreciosBC.Application.Interfaces.ICatalogoReadModel>();
            catalogoX.Setup(c => c.TryGetProductoIdBySkuAsync(It.IsAny<EmpresaId>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProductoId.New());
            var sut        = new ImportarPreciosBasicoUseCase(listaRepo, precioRepo, uow, tenant.Object, catalogoX.Object);

            var req = new ImportarPreciosBasicoUseCase.Request(
                Filas: new List<ImportarPreciosBasicoUseCase.Fila>
                {
                    new("SKU-001", 9, 10m, DateTime.UtcNow.Date, null, true), // no existe => debe abortar
                    new("SKU-001", 1, 11m, DateTime.UtcNow.Date, null, true),
                },
                DetenerAntePrimerError: true
            );

            Assert.That(async () => await sut.Handle(req, CancellationToken.None),
                        Throws.TypeOf<NotFoundException>());
        }

        [Test]
        public void Importar_Abortar_ConcurrenciaEnGuardar_LanzaConcurrencyException()
        {
            var listaRepo  = new InMemoryListaPrecioRepository { ListaActiva = CrearListaConBaseYMinoristaFijosYMayoristaVolumen() };
            var precioRepo = new InMemoryPrecioProductoRepository { SimularConcurrencia = true };
            var uow        = new InMemoryUow();
            var tenant     = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var catalogoY = new Moq.Mock<ListaPreciosBC.Application.Interfaces.ICatalogoReadModel>();
            // retornar el ProductoId del seed para SKU-LOCK
            var agg = PrecioProducto.CrearNuevo(EmpresaId.From("EMP-01"), ProductoId.New());
            catalogoY.Setup(c => c.TryGetProductoIdBySkuAsync(It.IsAny<EmpresaId>(), It.Is<string>(s => s == "SKU-LOCK"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg.ProductoId);
            var sut        = new ImportarPreciosBasicoUseCase(listaRepo, precioRepo, uow, tenant.Object, catalogoY.Object);

            // Seed con un SKU para provocar conflicto de versión en GuardarAsync
            agg.UpsertPrecioFijo(
                IdentificadorColumnaPrecio.DesdeNumero(1),
                ValorPrecio.DesdeMonto(9.99m, Moneda.PEN(), true),
                PeriodoVigencia.Crear(DateTime.UtcNow.AddDays(-10), null),
                "seed",
                DateTimeOffset.UtcNow.AddDays(-10)
            );
            precioRepo.Seed(agg);

            var req = new ImportarPreciosBasicoUseCase.Request(
                Filas: new List<ImportarPreciosBasicoUseCase.Fila>
                {
                    new("SKU-LOCK", 1, 10.50m, DateTime.UtcNow.Date, null, true) // modifica el mismo SKU
                },
                DetenerAntePrimerError: true
            );

            // El conflicto se dispara en GuardarAsync; la implementación del UC relanza
            Assert.That(async () => await sut.Handle(req, CancellationToken.None),
                        Throws.TypeOf<ConcurrencyException>());
        }

        [Test]
        public void Importar_Falla_SiNoHayListaActiva()
        {
            var listaRepo  = new InMemoryListaPrecioRepository { ListaActiva = null };
            var precioRepo = new InMemoryPrecioProductoRepository();
            var uow        = new InMemoryUow();
            var tenant     = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var catalogo4 = new Moq.Mock<ListaPreciosBC.Application.Interfaces.ICatalogoReadModel>();
            catalogo4.Setup(c => c.TryGetProductoIdBySkuAsync(It.IsAny<EmpresaId>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProductoId.New());
            var sut        = new ImportarPreciosBasicoUseCase(listaRepo, precioRepo, uow, tenant.Object, catalogo4.Object);

            var req = new ImportarPreciosBasicoUseCase.Request(
                Filas: new List<ImportarPreciosBasicoUseCase.Fila>
                {
                    new("SKU-001", 1, 10m, DateTime.UtcNow.Date, null, true)
                }
            );

            Assert.That(async () => await sut.Handle(req, CancellationToken.None),
                        Throws.TypeOf<NotFoundException>());
        }

        
    }
}
