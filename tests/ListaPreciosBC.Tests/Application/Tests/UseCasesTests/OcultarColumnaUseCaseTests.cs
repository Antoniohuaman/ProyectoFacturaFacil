using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.Interfaces; // IUnitOfWork
using ListaPreciosBC.Application.UseCases;   // OcultarColumnaUseCase
using ListaPreciosBC.Domain.Repositories;    // IListaPrecioRepository
using ListaPreciosBC.Domain.Aggregates;      // ListaPrecio
using ListaPreciosBC.Domain.ValueObjects;    // IdentificadorColumnaPrecio, NombreColumnaPrecio, ModoValorizacionColumna
using NUnit.Framework;
using SharedKernel.Exceptions;
using SharedKernel.Application.Interfaces;   // ITenantContext
using SharedKernel.ValueObjects;             // EmpresaId, TenantId
using Moq;

namespace ListaPreciosBC.Tests.Application.UseCases
{
    public class OcultarColumnaUseCaseTests
    {
        // ---------------------- Fake InMemory ----------------------

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
                ListaActiva = aggregate; // mantener como activa en el test
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

        private static ListaPrecio CrearListaConBaseYExtraVisible()
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
                IdentificadorColumnaPrecio.DesdeNumero(2),
                NombreColumnaPrecio.Crear("Mayorista"),
                ModoValorizacionColumna.PorVolumen,
                esBase: false,
                visible: true,   // ← comienza visible
                orden: 2
            );

            var plantilla = PlantillaColumnasPrecio.Crear(new[] { baseCfg, extraCfg });

            var lista = ListaPrecio.CrearNueva(
                EmpresaId.From("EMP-01"),
                Guid.NewGuid(),
                plantilla,
                "tester",
                DateTimeOffset.Now
            );

            return lista;
        }

        private static ListaPrecio CrearListaConBaseYExtraOculta()
        {
            var baseCfg = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.DesdeNumero(1),
                NombreColumnaPrecio.Crear("Base"),
                ModoValorizacionColumna.Fijo,
                esBase: true,
                visible: true,
                orden: 1
            );

            var extraCfg = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.DesdeNumero(2),
                NombreColumnaPrecio.Crear("Mayorista"),
                ModoValorizacionColumna.PorVolumen,
                esBase: false,
                visible: false,  // ← ya oculta
                orden: 2
            );

            var plantilla = PlantillaColumnasPrecio.Crear(new[] { baseCfg, extraCfg });

            var lista = ListaPrecio.CrearNueva(
                EmpresaId.From("EMP-01"),
                Guid.NewGuid(),
                plantilla,
                "tester",
                DateTimeOffset.Now
            );

            return lista;
        }

        // ---------------------- Tests ----------------------

        [Test]
        public async Task OcultarColumna_Exito_CambiaVisibleAFalso_Persistiendo()
        {
            var repo = new InMemoryListaPrecioRepository();
            var uow = new InMemoryUow();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var sut = new OcultarColumnaUseCase(repo, uow, tenant.Object);

            var lista = CrearListaConBaseYExtraVisible();
            repo.Seed(lista);

            var req = new OcultarColumnaUseCase.Request(
                ColumnaNumero: 2,
                Usuario: "tester"
            );

            var res = await sut.Handle(req, CancellationToken.None);

            Assert.That(res.ColumnaNumero, Is.EqualTo(2));
            Assert.That(res.Visible, Is.False);
            Assert.That(res.Version, Is.GreaterThanOrEqualTo(0));

            // Verificación: la columna 2 quedó oculta, la base sigue visible
            var persisted = await repo.ObtenerActivaAsync();
            var col2 = persisted!.Plantilla.Columnas.Single(c => c.Id.Equals(IdentificadorColumnaPrecio.DesdeNumero(2)));
            var col1 = persisted!.Plantilla.Columnas.Single(c => c.Id.Equals(IdentificadorColumnaPrecio.DesdeNumero(1)));
            Assert.That(col2.Visible, Is.False);
            Assert.That(col1.Visible, Is.True);
        }

        [Test]
        public void OcultarColumna_Falla_SiNoHayListaActiva()
        {
            var repo = new InMemoryListaPrecioRepository { ListaActiva = null };
            var uow = new InMemoryUow();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var sut = new OcultarColumnaUseCase(repo, uow, tenant.Object);

            var req = new OcultarColumnaUseCase.Request(ColumnaNumero: 2);

            Assert.That(async () => await sut.Handle(req, CancellationToken.None),
                        Throws.TypeOf<NotFoundException>());
        }

        [Test]
        public void OcultarColumna_Falla_SiColumnaNoExiste()
        {
            var repo = new InMemoryListaPrecioRepository();
            var uow = new InMemoryUow();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var sut = new OcultarColumnaUseCase(repo, uow, tenant.Object);

            var lista = CrearListaConBaseYExtraVisible();
            repo.Seed(lista);

            var req = new OcultarColumnaUseCase.Request(ColumnaNumero: 9); // no existe

            Assert.That(async () => await sut.Handle(req, CancellationToken.None),
                        Throws.TypeOf<NotFoundException>());
        }

        [Test]
        public async Task OcultarColumna_Idempotente_SiYaEstabaOculta_NoRompeInvariantes()
        {
            var repo = new InMemoryListaPrecioRepository();
            var uow = new InMemoryUow();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var sut = new OcultarColumnaUseCase(repo, uow, tenant.Object);

            var lista = CrearListaConBaseYExtraOculta();
            repo.Seed(lista);

            var req = new OcultarColumnaUseCase.Request(ColumnaNumero: 2);

            var res = await sut.Handle(req, CancellationToken.None);

            Assert.That(res.ColumnaNumero, Is.EqualTo(2));
            Assert.That(res.Visible, Is.False); // sigue oculta
            Assert.That(res.Version, Is.GreaterThanOrEqualTo(0));

            var persisted = await repo.ObtenerActivaAsync();
            Assert.That(persisted!.Plantilla.Columnas.Count(c => c.EsBase), Is.EqualTo(1)); // unicidad base intacta
        }

        [Test]
        public void OcultarColumna_FallaPorConcurrencia_SiVersionCambiaEntreLoadYSave()
        {
            var repo = new InMemoryListaPrecioRepository { SimularConcurrencia = true };
            var uow = new InMemoryUow();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var sut = new OcultarColumnaUseCase(repo, uow, tenant.Object);

            var lista = CrearListaConBaseYExtraVisible();
            repo.Seed(lista);

            var req = new OcultarColumnaUseCase.Request(
                ColumnaNumero: 2,
                Usuario: "tester"
            );

            Assert.That(async () => await sut.Handle(req, CancellationToken.None),
                        Throws.TypeOf<ConcurrencyException>());
        }

        
    }
}
