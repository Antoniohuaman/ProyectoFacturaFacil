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
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace ListaPreciosBC.Tests.Application.UseCases
{
    public class UpsertPrecioFijoUseCaseTests
    {
        // ---------------------- Fakes InMemory ----------------------

        private sealed class InMemoryListaPrecioRepository : IListaPrecioRepository
        {
            public ListaPrecio? ListaActiva { get; set; }

            public Task<ListaPrecio?> ObtenerActivaAsync(Guid empresaId, Guid? sucursalId, CancellationToken ct = default)
                => Task.FromResult(ListaActiva);

            public Task<ListaPrecio?> ObtenerActivaAsync(CancellationToken ct = default)
                => Task.FromResult(ListaActiva);

            public Task<ListaPrecio?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default)
                => Task.FromResult(ListaActiva is not null && ListaActiva.Id == id ? ListaActiva : null);

            public Task GuardarAsync(ListaPrecio aggregate, int expectedVersion, CancellationToken ct = default)
                => Task.CompletedTask;
        }

        private sealed class InMemoryPrecioProductoRepository : IPrecioProductoRepository
        {
            // Partition key: (empresa, sucursal, sku)
            private readonly Dictionary<string, PrecioProducto> _store = new();
            private readonly Dictionary<string, int> _loadedVersion = new();

            public bool SimularConcurrencia { get; set; } = false;

            public Task<PrecioProducto?> ObtenerPorSkuAsync(Guid empresaId, Guid? sucursalId, Sku sku, CancellationToken ct = default)
            {
                var key = sku.Valor;
                if (_store.TryGetValue(key, out var agg))
                {
                    _loadedVersion[key] = agg.Version; // para expectedVersion
                    return Task.FromResult<PrecioProducto?>(agg);
                }
                return Task.FromResult<PrecioProducto?>(null);
            }

            public Task<PrecioProducto?> ObtenerPorSkuAsync(Sku sku, CancellationToken ct = default)
            {
                // Dummy para cumplir interfaz
                return Task.FromResult<PrecioProducto?>(null);
            }

            public Task GuardarAsync(PrecioProducto aggregate, int expectedVersion, CancellationToken ct = default)
            {
                var key = aggregate.Sku.Valor;

                // Simula update concurrente: alguien incrementó la versión “entre” load y save
                if (SimularConcurrencia && _loadedVersion.TryGetValue(key, out var v))
                {
                    _loadedVersion[key] = v + 1;
                }

                if (_loadedVersion.TryGetValue(key, out var loaded) && loaded != expectedVersion)
                    throw new ConcurrencyException(
                        "Versión inesperada del agregado.",
                        aggregate.Sku.Valor,
                        expectedVersion,
                        loaded,
                        null,
                        null
                    );

                _store[key] = aggregate;
                _loadedVersion[key] = aggregate.Version;
                return Task.CompletedTask;
            }

            public Task EliminarAsync(Sku sku, int? expectedVersion = null, CancellationToken ct = default)
            {
                // Dummy para cumplir interfaz
                return Task.CompletedTask;
            }

            // Helpers de prueba
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

        // ---------------------- Builders de dominio (usando tu API pública) ----------------------

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
            return ListaPrecio.CrearNueva(Guid.NewGuid(), plantilla);
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
            var volCfg = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.DesdeNumero(numero),
                NombreColumnaPrecio.Crear($"Vol{numero}"),
                ModoValorizacionColumna.PorVolumen,
                false,
                visible,
                numero
            );
            var plantilla = PlantillaColumnasPrecio.Crear(new[] { baseCfg, volCfg });
            return ListaPrecio.CrearNueva(Guid.NewGuid(), plantilla);
        }

        private static PrecioProducto CrearPrecioProducto(Guid empresaId, Guid? sucursalId, string sku)
        {
            // Usar la fábrica real
            return PrecioProducto.CrearNuevo(Sku.Crear(sku));
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
            var sut = new UpsertPrecioFijoUseCase(precioRepo, listaRepo, uow);

            var req = new UpsertPrecioFijoUseCase.Request(
                "SKU-001",
                1,
                10.50m,
                true,
                DateTimeOffset.UtcNow.Date,
                null,
                "tester"
            );

            // Act
            var res = await sut.Handle(req, CancellationToken.None);

            // Assert básicos del response
            Assert.That(res.Sku, Is.EqualTo("SKU-001"));
            Assert.That(res.ColumnaNumero, Is.EqualTo(1));
            Assert.That(res.Monto, Is.EqualTo(10.50m));
            Assert.That(res.Moneda, Is.EqualTo("PEN"));
            Assert.That(res.Version, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void UpsertPrecioFijo_Falla_SiNoHayListaActiva()
        {
            // Arrange
            var empresa = Guid.NewGuid();
            var listaRepo = new InMemoryListaPrecioRepository { ListaActiva = null };
            var precioRepo = new InMemoryPrecioProductoRepository();
            var uow = new InMemoryUow();
            var sut = new UpsertPrecioFijoUseCase(precioRepo, listaRepo, uow);

            var req = new UpsertPrecioFijoUseCase.Request(
                "SKU-404",
                1,
                1m,
                true,
                DateTimeOffset.UtcNow.Date,
                null
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
            var sut = new UpsertPrecioFijoUseCase(precioRepo, listaRepo, uow);

            var req = new UpsertPrecioFijoUseCase.Request(
                "SKU-001",
                9,
                10m,
                true,
                DateTimeOffset.UtcNow.Date,
                null
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
            var sut = new UpsertPrecioFijoUseCase(precioRepo, listaRepo, uow);

            var req = new UpsertPrecioFijoUseCase.Request(
                "SKU-001",
                2,
                10m,
                true,
                DateTimeOffset.UtcNow.Date,
                null
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
            var sut = new UpsertPrecioFijoUseCase(precioRepo, listaRepo, uow);

            var req = new UpsertPrecioFijoUseCase.Request(
                "SKU-001",
                1,
                10.50m,
                true,
                DateTimeOffset.UtcNow.Date,
                null,
                "tester"
            );

            // Act + Assert
            Assert.ThrowsAsync<ConcurrencyException>(() => sut.Handle(req, CancellationToken.None));
        }
    }
}
