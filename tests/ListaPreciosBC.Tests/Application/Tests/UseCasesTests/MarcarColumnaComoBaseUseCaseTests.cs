using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.Interfaces; // IUnitOfWork
using ListaPreciosBC.Application.UseCases;   // MarcarColumnaComoBaseUseCase
using ListaPreciosBC.Domain.Repositories;    // IListaPrecioRepository
using ListaPreciosBC.Domain.Aggregates;      // ListaPrecio
using ListaPreciosBC.Domain.ValueObjects;    // IdentificadorColumnaPrecio, NombreColumnaPrecio, ModoValorizacionColumna
using NUnit.Framework;
using SharedKernel.Exceptions;
using SharedKernel.Application.Interfaces;   // ITenantContext
using SharedKernel.ValueObjects;             // EmpresaId, TenantId

namespace ListaPreciosBC.Tests.Application.UseCases
{
    public class MarcarColumnaComoBaseUseCaseTests
    {
        // ---------------------- Fakes InMemory ----------------------

    private sealed class InMemoryListaPrecioRepository : IListaPrecioRepository
        {
            private readonly Dictionary<Guid, ListaPrecio> _store = new();
            private readonly Dictionary<Guid, int> _loadedVersion = new();

            public bool SimularConcurrencia { get; set; }
            public ListaPrecio? ListaActiva { get; set; }

            public Task<ListaPrecio?> ObtenerActivaAsync(EmpresaId empresaId, Guid? sucursalId = null, CancellationToken ct = default)
            {
                if (ListaActiva is not null)
                    _loadedVersion[ListaActiva.Id] = ListaActiva.Version;
                return Task.FromResult(ListaActiva);
            }

            // Helper extra para asserts
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

                // Simula cambio concurrente entre load y save
                if (SimularConcurrencia && _loadedVersion.TryGetValue(id, out var v))
                    _loadedVersion[id] = v + 1;

                if (_loadedVersion.TryGetValue(id, out var loaded) && loaded != expectedVersion)
                    throw new ConcurrencyException(
                        "Versión inesperada del agregado ListaPrecio.",
                        id.ToString(),
                        expectedVersion,
                        null,
                        null,
                        null
                    );

                _store[id] = aggregate;
                _loadedVersion[id] = aggregate.Version;
                ListaActiva = aggregate; // mantener como activa para el test
                return Task.CompletedTask;
            }

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

        // ---------------------- Builder con invariantes ----------------------
        // Creamos una lista con columna base (#1) y una extra (#2).
        private static ListaPrecio CrearListaConBaseYExtra(byte numeroExtra, ModoValorizacionColumna modoExtra)
        {
            // ADAPTA si tu aggregate tiene factory distinta
            var baseCfg = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.DesdeNumero(1),
                NombreColumnaPrecio.Crear("Base"),
                ModoValorizacionColumna.Fijo,
                esBase: true,
                visible: true,
                orden: 1
            );

            var extraCfg = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.DesdeNumero(numeroExtra),
                NombreColumnaPrecio.Crear($"Col{numeroExtra}"),
                modoExtra,                 // puede ser Fijo o PorVolumen (el dominio decide si es válido como base)
                esBase: false,
                visible: true,
                orden: numeroExtra
            );

            var plantilla = PlantillaColumnasPrecio.Crear(new[] { baseCfg, extraCfg });

            var lista = ListaPrecio.CrearNueva(
                Guid.NewGuid(),
                plantilla,
                "tester",
                DateTime.Now
            );

            return lista;
        }

        // ---------------------- Tests ----------------------

        [Test]
        public async Task MarcarComoBase_Exito_CambiaBase_Y_DesmarcaLaAnterior()
        {
            var repo = new InMemoryListaPrecioRepository();
            var uow = new InMemoryUow();
            var sut = new MarcarColumnaComoBaseUseCase(repo, uow, new TenantContextFake());

            var lista = CrearListaConBaseYExtra(numeroExtra: 2, modoExtra: ModoValorizacionColumna.PorVolumen);
            repo.Seed(lista);

            // Precondición: columna 1 es base
            var colId1 = IdentificadorColumnaPrecio.DesdeNumero(1);
            var before = await repo.ObtenerActivaAsync();
            Assert.That(before!.Plantilla.Columnas.Single(c => c.Id.Equals(colId1)).EsBase, Is.True);

            // Acción: marcar columna 2 como base
            var req = new MarcarColumnaComoBaseUseCase.Request(
                ColumnaNumero: 2,
                Usuario: "tester"
            );

            var res = await sut.Handle(req, CancellationToken.None);

            Assert.That(res.ColumnaNumeroBase, Is.EqualTo(2));
            Assert.That(res.Version, Is.GreaterThanOrEqualTo(0));

            // Verificación: ahora 2 es base y 1 dejó de serlo; además unicidad
            var persisted = await repo.ObtenerActivaAsync();
            var colId2 = IdentificadorColumnaPrecio.DesdeNumero(2);

            var c1Base = persisted!.Plantilla.Columnas.Single(c => c.Id.Equals(colId1)).EsBase;
            var c2Base = persisted!.Plantilla.Columnas.Single(c => c.Id.Equals(colId2)).EsBase;

            Assert.That(c2Base, Is.True);
            Assert.That(c1Base, Is.False);
            Assert.That(persisted.Plantilla.Columnas.Count(c => c.EsBase), Is.EqualTo(1));
        }

        [Test]
        public void MarcarComoBase_Falla_SiNoHayListaActiva()
        {
            var repo = new InMemoryListaPrecioRepository { ListaActiva = null };
            var uow = new InMemoryUow();
            var sut = new MarcarColumnaComoBaseUseCase(repo, uow, new TenantContextFake());

            var req = new MarcarColumnaComoBaseUseCase.Request(
                ColumnaNumero: 2
            );

            Assert.That(async () => await sut.Handle(req, CancellationToken.None),
                        Throws.TypeOf<NotFoundException>());
        }

        [Test]
        public void MarcarComoBase_Falla_SiColumnaNoExiste()
        {
            var repo = new InMemoryListaPrecioRepository();
            var uow = new InMemoryUow();
            var sut = new MarcarColumnaComoBaseUseCase(repo, uow, new TenantContextFake());

            var lista = CrearListaConBaseYExtra(numeroExtra: 2, modoExtra: ModoValorizacionColumna.Fijo);
            repo.Seed(lista);

            var req = new MarcarColumnaComoBaseUseCase.Request(
                ColumnaNumero: 9 // no existe
            );

            Assert.That(async () => await sut.Handle(req, CancellationToken.None),
                        Throws.TypeOf<NotFoundException>());
        }

        [Test]
        public async Task MarcarComoBase_Idempotente_SiYaEsBase_NoRompeInvariantes()
        {
            var repo = new InMemoryListaPrecioRepository();
            var uow = new InMemoryUow();
            var sut = new MarcarColumnaComoBaseUseCase(repo, uow, new TenantContextFake());

            var lista = CrearListaConBaseYExtra(numeroExtra: 2, modoExtra: ModoValorizacionColumna.Fijo);
            repo.Seed(lista);

            // Volver a marcar como base la #1 (ya es base)
            var req = new MarcarColumnaComoBaseUseCase.Request(
                ColumnaNumero: 1
            );

            var res = await sut.Handle(req, CancellationToken.None);

            Assert.That(res.ColumnaNumeroBase, Is.EqualTo(1));

            var colId1 = IdentificadorColumnaPrecio.DesdeNumero(1);
            var colId2 = IdentificadorColumnaPrecio.DesdeNumero(2);
            var persisted = await repo.ObtenerActivaAsync();

            var c1Base = persisted!.Plantilla.Columnas.Single(c => c.Id.Equals(colId1)).EsBase;
            var c2Base = persisted!.Plantilla.Columnas.Single(c => c.Id.Equals(colId2)).EsBase;

            Assert.That(c1Base, Is.True);
            Assert.That(c2Base, Is.False);
            Assert.That(persisted.Plantilla.Columnas.Count(c => c.EsBase), Is.EqualTo(1));
        }

        [Test]
        public void MarcarComoBase_FallaPorConcurrencia_SiVersionCambiaEntreLoadYSave()
        {
            var repo = new InMemoryListaPrecioRepository { SimularConcurrencia = true };
            var uow = new InMemoryUow();
            var sut = new MarcarColumnaComoBaseUseCase(repo, uow, new TenantContextFake());

            var lista = CrearListaConBaseYExtra(numeroExtra: 2, modoExtra: ModoValorizacionColumna.Fijo);
            repo.Seed(lista);

            var req = new MarcarColumnaComoBaseUseCase.Request(
                ColumnaNumero: 2,
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
