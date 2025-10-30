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
using SharedKernel.ValueObjects;             // Moneda, Sku
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
                {
                    _loadedVersion[key] = v + 1; // simula cambio entre load y save
                }

                    if (_loadedVersion.TryGetValue(key, out var loaded) && loaded != expectedVersion)
                        throw new ConcurrencyException(
                            "Versión inesperada del agregado.",
                            aggregate.Sku.Valor,
                            expectedVersion,
                            aggregate.Version,
                            null,
                            null
                        );

                _store[key] = aggregate;
                _loadedVersion[key] = aggregate.Version;
                return Task.CompletedTask;
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

            // Helper para tests
            public Task EliminarAsync(Sku sku, int? expectedVersion = null, CancellationToken ct = default)
                => EliminarAsync(EmpresaId.From("TEST-EMPRESA"), null, sku, expectedVersion, ct);

                public Task<IEnumerable<PrecioProducto>> ObtenerPorSkusAsync(IEnumerable<Sku> skus, CancellationToken ct = default)
                {
                    var result = skus.Select(s => _store.TryGetValue(s.Valor, out var agg) ? agg : null).Where(x => x != null).Cast<PrecioProducto>();
                    return Task.FromResult(result);
                }

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
            return ListaPrecio.CrearNueva(Guid.NewGuid(), plantilla);
        }

        private static PrecioProducto CrearPrecioProducto(string sku)
        {
            return PrecioProducto.CrearNuevo(Sku.Crear(sku));
        }

        private static bool ExistePrecioParaCantidad(PrecioProducto agg, byte columnaNumero, int cantidad)
        {
            var colId = IdentificadorColumnaPrecio.DesdeNumero(columnaNumero);
            var vigente = agg.ObtenerPrecioVigente(colId, DateTimeOffset.UtcNow.Date, cantidad);
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
            var sut = new UpsertMatrizVolumenUseCase(precioRepo, listaRepo, uow, tenant.Object);

            var req = new UpsertMatrizVolumenUseCase.Request(
                Sku: "SKU-001",
                ColumnaNumero: 2,
                Tramos: new List<UpsertMatrizVolumenUseCase.Tramo>
                {
                    new(DesdeCantidad: 1,  HastaCantidad: 9,  Monto: 10.50m, IncluyeImpuesto: true),
                    new(DesdeCantidad: 10, HastaCantidad: null, Monto: 9.90m, IncluyeImpuesto: true)
                },
                CantidadReferenciaParaEventoBase: 1,
                Usuario: "tester"
            );

            var res = await sut.Handle(req, CancellationToken.None);

            Assert.That(res.Sku, Is.EqualTo("SKU-001"));
            Assert.That(res.ColumnaNumero, Is.EqualTo(2));
            Assert.That(res.TramosActualizados, Is.EqualTo(2));
            Assert.That(res.Version, Is.GreaterThanOrEqualTo(0));

            // Efecto observable: el agregado debe resolver precio para cantidades en ambos tramos
            var post = await precioRepo.ObtenerPorSkuAsync(Sku.Crear("SKU-001"));
            Assert.That(post, Is.Not.Null);
            Assert.That(ExistePrecioParaCantidad(post!, 2, cantidad: 1), Is.True);
            Assert.That(ExistePrecioParaCantidad(post!, 2, cantidad: 10), Is.True);
        }

        [Test]
        public void UpsertMatrizVolumen_Falla_SiNoHayListaActiva()
        {
            var listaRepo = new InMemoryListaPrecioRepository { ListaActiva = null };
            var precioRepo = new InMemoryPrecioProductoRepository();
            var uow = new InMemoryUow();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var sut = new UpsertMatrizVolumenUseCase(precioRepo, listaRepo, uow, tenant.Object);

            var req = new UpsertMatrizVolumenUseCase.Request(
                Sku: "SKU-404",
                ColumnaNumero: 2,
                Tramos: new List<UpsertMatrizVolumenUseCase.Tramo>
                {
                    new(1, 10, 10m, true)
                }
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
            var sut = new UpsertMatrizVolumenUseCase(precioRepo, listaRepo, uow, tenant.Object);

            var req = new UpsertMatrizVolumenUseCase.Request(
                Sku: "SKU-001",
                ColumnaNumero: 9, // no existe
                Tramos: new List<UpsertMatrizVolumenUseCase.Tramo>
                {
                    new(1, 10, 10m, true)
                }
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
            var lista = ListaPrecio.CrearNueva(Guid.NewGuid(), plantilla);
            var listaRepo = new InMemoryListaPrecioRepository { ListaActiva = lista };
            IPrecioProductoRepository precioRepo = new InMemoryPrecioProductoRepository();
            var uow = new InMemoryUow();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var sut = new UpsertMatrizVolumenUseCase(precioRepo, listaRepo, uow, tenant.Object);

            var req = new UpsertMatrizVolumenUseCase.Request(
                Sku: "SKU-001",
                ColumnaNumero: 1, // fijo
                Tramos: new List<UpsertMatrizVolumenUseCase.Tramo>
                {
                    new(1, 10, 10m, true)
                }
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
            var sut = new UpsertMatrizVolumenUseCase(precioRepo, listaRepo, uow, tenant.Object);

            var req = new UpsertMatrizVolumenUseCase.Request(
                Sku: "SKU-001",
                ColumnaNumero: 2,
                Tramos: new List<UpsertMatrizVolumenUseCase.Tramo>
                {
                    new(1, 10, 10m, true)
                },
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
            var sut = new UpsertMatrizVolumenUseCase(precioRepo, listaRepo, uow, tenant.Object);

            // Tramos solapados: [1..10] y [8..∞)
            var req = new UpsertMatrizVolumenUseCase.Request(
                Sku: "SKU-001",
                ColumnaNumero: 2,
                Tramos: new List<UpsertMatrizVolumenUseCase.Tramo>
                {
                    new(1, 10, 10m, true),
                    new(8, null, 9m, true)
                }
            );

            Assert.That(async () => await sut.Handle(req, CancellationToken.None),
                        Throws.TypeOf<BusinessRuleException>());
        }

        
    }
}
