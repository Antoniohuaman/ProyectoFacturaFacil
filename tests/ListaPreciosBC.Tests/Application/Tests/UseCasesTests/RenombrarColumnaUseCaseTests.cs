using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.Interfaces; // IUnitOfWork
using ListaPreciosBC.Application.UseCases;   // RenombrarColumnaUseCase
using ListaPreciosBC.Domain.Repositories;    // IListaPrecioRepository
using ListaPreciosBC.Domain.Aggregates;      // ListaPrecio
using ListaPreciosBC.Domain.ValueObjects;    // IdentificadorColumnaPrecio, NombreColumnaPrecio, ModoValorizacionColumna
using NUnit.Framework;
using SharedKernel.Exceptions;
using SharedKernel.Application.Interfaces;   // ITenantContext
using SharedKernel.ValueObjects;             // EmpresaId, TenantId

namespace ListaPreciosBC.Tests.Application.UseCases
{
    public class RenombrarColumnaUseCaseTests
    {
        // ---------------------- Fakes InMemory ----------------------

        private sealed class InMemoryListaPrecioRepository : IListaPrecioRepository
        {
            private readonly Dictionary<Guid, ListaPrecio> _store = new();
            private readonly Dictionary<Guid, int> _loadedVersion = new();

            public bool SimularConcurrencia { get; set; }

            // Para el test, consideramos "activa" la única guardada
            public ListaPrecio? ListaActiva { get; set; }

            public Task<ListaPrecio?> ObtenerActivaAsync(EmpresaId empresaId, Guid? sucursalId = null, CancellationToken ct = default)
            {
                if (ListaActiva is not null)
                {
                    _loadedVersion[ListaActiva.Id] = ListaActiva.Version;
                }
                return Task.FromResult(ListaActiva);
            }

            // Helper para asserts de test
            public Task<ListaPrecio?> ObtenerActivaAsync() => Task.FromResult(ListaActiva);

            public Task<ListaPrecio?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default)
            {
                _store.TryGetValue(id, out var lp);
                if (lp is not null) _loadedVersion[id] = lp.Version;
                return Task.FromResult(lp);
            }

            public Task GuardarAsync(ListaPrecio aggregate, EmpresaId empresaId, Guid? sucursalId, int expectedVersion, CancellationToken ct = default)
            {
                var id = aggregate.Id;

                // Simula un cambio concurrente entre load y save
                if (SimularConcurrencia && _loadedVersion.TryGetValue(id, out var v))
                {
                    _loadedVersion[id] = v + 1;
                }

                if (_loadedVersion.TryGetValue(id, out var loaded) && loaded != expectedVersion)
                    throw new ConcurrencyException(
                        "ListaPrecio", // aggregate
                        id.ToString(),  // aggregateId
                        expectedVersion,
                        loaded,
                        "Versión inesperada del agregado ListaPrecio."
                    );

                _store[id] = aggregate;
                _loadedVersion[id] = aggregate.Version;
                // Para este test, mantenemos también como activa
                ListaActiva = aggregate;
                return Task.CompletedTask;
            }

            // Helper para seed
            public void Seed(ListaPrecio lista)
            {
                _store[lista.Id] = lista;
                _loadedVersion[lista.Id] = lista.Version;
                ListaActiva = lista;
            }
        }

        private sealed class InMemoryUow : IUnitOfWork
        {
            public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
        }

        // ---------------------- Builder con invariantes (Base + otra columna) ----------------------

        private static ListaPrecio CrearListaConBaseYColumna(byte numeroColumnaExtra, bool visibleExtra = true)
        {
            // Crear la plantilla con las dos columnas
            var baseCfg = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.DesdeNumero(1),
                NombreColumnaPrecio.Crear("Base"),
                ModoValorizacionColumna.Fijo,
                esBase: true,
                visible: true,
                orden: 1
            );
            var extraCfg = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.DesdeNumero(numeroColumnaExtra),
                NombreColumnaPrecio.Crear($"Col{numeroColumnaExtra}"),
                ModoValorizacionColumna.Fijo, // puede ser fijo o volumen; el rename no depende del modo
                esBase: false,
                visible: visibleExtra,
                orden: numeroColumnaExtra
            );
            var plantilla = PlantillaColumnasPrecio.Crear(new[] { baseCfg, extraCfg });
            return ListaPrecio.CrearNueva(Guid.NewGuid(), plantilla);
        }

        // ---------------------- Tests ----------------------

        [Test]
        public async Task RenombrarColumna_Exito_CambiaNombreYPersiste()
        {
            var repo = new InMemoryListaPrecioRepository();
            var uow = new InMemoryUow();
            var sut = new RenombrarColumnaUseCase(repo, uow, new TenantContextFake());

            var lista = CrearListaConBaseYColumna(2);
            repo.Seed(lista);

            var req = new RenombrarColumnaUseCase.Request(
                ColumnaNumero: 2,
                NuevoNombre: "Mayorista",
                Usuario: "tester"
            );

            var res = await sut.Handle(req, CancellationToken.None);

            Assert.That(res.ColumnaNumero, Is.EqualTo(2));
            Assert.That(res.Nombre, Is.EqualTo("Mayorista"));
            Assert.That(res.Version, Is.GreaterThanOrEqualTo(0));

            // Verificación de efecto: la columna #2 ahora se llama "Mayorista"
            var colId = IdentificadorColumnaPrecio.DesdeNumero(2);
            var persisted = await repo.ObtenerActivaAsync();
            var nombre = persisted!.Plantilla.Columnas.Single(c => c.Id.Equals(colId)).Nombre.Valor;
            Assert.That(nombre, Is.EqualTo("Mayorista"));
        }

        [Test]
        public void RenombrarColumna_Falla_SiNoHayListaActiva()
        {
            var repo = new InMemoryListaPrecioRepository { ListaActiva = null };
            var uow = new InMemoryUow();
            var sut = new RenombrarColumnaUseCase(repo, uow, new TenantContextFake());

            var req = new RenombrarColumnaUseCase.Request(
                ColumnaNumero: 2,
                NuevoNombre: "Mayorista"
            );

            Assert.That(async () => await sut.Handle(req, CancellationToken.None),
                        Throws.TypeOf<NotFoundException>());
        }

        [Test]
        public void RenombrarColumna_Falla_SiColumnaNoExiste()
        {
            var repo = new InMemoryListaPrecioRepository();
            var uow = new InMemoryUow();
            var sut = new RenombrarColumnaUseCase(repo, uow, new TenantContextFake());

            var lista = CrearListaConBaseYColumna(2);
            repo.Seed(lista);

            var req = new RenombrarColumnaUseCase.Request(
                ColumnaNumero: 9, // no existe
                NuevoNombre: "VIP"
            );

            Assert.That(async () => await sut.Handle(req, CancellationToken.None),
                        Throws.TypeOf<NotFoundException>());
        }

        [Test]
        public void RenombrarColumna_FallaPorConcurrencia_SiVersionCambiaEntreLoadYSave()
        {
            var repo = new InMemoryListaPrecioRepository { SimularConcurrencia = true };
            var uow = new InMemoryUow();
            var sut = new RenombrarColumnaUseCase(repo, uow, new TenantContextFake());

            var lista = CrearListaConBaseYColumna(2);
            repo.Seed(lista);

            var req = new RenombrarColumnaUseCase.Request(
                ColumnaNumero: 2,
                NuevoNombre: "Mayorista",
                Usuario: "tester"
            );

            Assert.That(async () => await sut.Handle(req, CancellationToken.None),
                        Throws.TypeOf<ConcurrencyException>());
        }

        private sealed class TenantContextFake : ITenantContext
        {
            public TenantId TenantId { get; } = TenantId.New();
            public EmpresaId EmpresaId { get; } = EmpresaId.From("TEST-EMPRESA");
        }
    }
}
