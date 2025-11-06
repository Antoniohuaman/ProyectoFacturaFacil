using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.Interfaces; // IUnitOfWork
using ListaPreciosBC.Application.UseCases;   // CambiarModoColumnaUseCase
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
    public class CambiarModoColumnaUseCaseTests
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

            // Helper extra para los asserts del test (no forma parte de la interfaz)
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
                ListaActiva = aggregate; // para este test, mantenemos como activa
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
            public int CommitCount { get; private set; }
            public Task CommitAsync(CancellationToken ct = default)
            {
                CommitCount++;
                return Task.CompletedTask;
            }
        }

        // ---------------------- Builders con invariantes ----------------------

        private static ListaPrecio CrearListaConBaseYExtra(byte numeroExtra, ModoValorizacionColumna modoExtra)
        {
            // Usar el factory adecuado de ListaPrecio
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
                modoExtra,
                esBase: false,
                visible: true,
                orden: numeroExtra
            );

            var plantilla = PlantillaColumnasPrecio.Crear(new[] { baseCfg, extraCfg });

            var lista = ListaPrecio.CrearNueva(
                EmpresaId.From("EMP-01"),
                Guid.NewGuid(),
                plantilla,
                "tester",
                DateTime.Now
            );

            return lista;
        }

        // ---------------------- Tests ----------------------

        [Test]
        public async Task CambiarModo_Exito_DeFijoAPorVolumen_PersistiendoYVerificando()
        {
            var repo = new InMemoryListaPrecioRepository();
            var uow = new InMemoryUow();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var sut = new CambiarModoColumnaUseCase(repo, uow, tenant.Object);

            var lista = CrearListaConBaseYExtra(numeroExtra: 2, modoExtra: ModoValorizacionColumna.Fijo);
            repo.Seed(lista);

            var req = new CambiarModoColumnaUseCase.Request(
                ColumnaNumero: 2,
                NuevoModo: ModoValorizacionColumna.PorVolumen,
                Usuario: "tester"
            );

            var res = await sut.Handle(req, CancellationToken.None);

            Assert.That(res.ColumnaNumero, Is.EqualTo(2));
            Assert.That(res.Modo, Is.EqualTo(ModoValorizacionColumna.PorVolumen.ToString()));
            Assert.That(res.Version, Is.GreaterThanOrEqualTo(0));

            // Efecto observable: la columna #2 quedó en modo PorVolumen
            var colId = IdentificadorColumnaPrecio.DesdeNumero(2);
            var persisted = await repo.ObtenerActivaAsync();
            var modo = persisted!.Plantilla.Columnas.Single(c => c.Id.Equals(colId)).Modo;
            Assert.That(modo, Is.EqualTo(ModoValorizacionColumna.PorVolumen));
        }

        [Test]
        public async Task CambiarModo_Exito_DePorVolumenAFijo()
        {
            var repo = new InMemoryListaPrecioRepository();
            var uow = new InMemoryUow();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var sut = new CambiarModoColumnaUseCase(repo, uow, tenant.Object);

            var lista = CrearListaConBaseYExtra(numeroExtra: 3, modoExtra: ModoValorizacionColumna.PorVolumen);
            repo.Seed(lista);

            var req = new CambiarModoColumnaUseCase.Request(
                ColumnaNumero: 3,
                NuevoModo: ModoValorizacionColumna.Fijo
            );

            var res = await sut.Handle(req, CancellationToken.None);

            Assert.That(res.ColumnaNumero, Is.EqualTo(3));
            Assert.That(res.Modo, Is.EqualTo(ModoValorizacionColumna.Fijo.ToString()));

            var colId = IdentificadorColumnaPrecio.DesdeNumero(3);
            var persisted = await repo.ObtenerActivaAsync();
            var modo = persisted!.Plantilla.Columnas.Single(c => c.Id.Equals(colId)).Modo;
            Assert.That(modo, Is.EqualTo(ModoValorizacionColumna.Fijo));
        }

        [Test]
        public void CambiarModo_Falla_SiNoHayListaActiva()
        {
            var repo = new InMemoryListaPrecioRepository { ListaActiva = null };
            var uow = new InMemoryUow();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var sut = new CambiarModoColumnaUseCase(repo, uow, tenant.Object);

            var req = new CambiarModoColumnaUseCase.Request(
                ColumnaNumero: 2,
                NuevoModo: ModoValorizacionColumna.PorVolumen
            );

            Assert.That(async () => await sut.Handle(req, CancellationToken.None),
                        Throws.TypeOf<NotFoundException>());
        }

        [Test]
        public void CambiarModo_Falla_SiColumnaNoExiste()
        {
            var repo = new InMemoryListaPrecioRepository();
            var uow = new InMemoryUow();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var sut = new CambiarModoColumnaUseCase(repo, uow, tenant.Object);

            var lista = CrearListaConBaseYExtra(numeroExtra: 2, modoExtra: ModoValorizacionColumna.Fijo);
            repo.Seed(lista);

            var req = new CambiarModoColumnaUseCase.Request(
                ColumnaNumero: 9, // no existe
                NuevoModo: ModoValorizacionColumna.PorVolumen
            );

            Assert.That(async () => await sut.Handle(req, CancellationToken.None),
                        Throws.TypeOf<NotFoundException>());
        }

        [Test]
        public void CambiarModo_FallaPorConcurrencia_SiVersionCambiaEntreLoadYSave()
        {
            var repo = new InMemoryListaPrecioRepository { SimularConcurrencia = true };
            var uow = new InMemoryUow();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var sut = new CambiarModoColumnaUseCase(repo, uow, tenant.Object);

            var lista = CrearListaConBaseYExtra(numeroExtra: 2, modoExtra: ModoValorizacionColumna.Fijo);
            repo.Seed(lista);

            var req = new CambiarModoColumnaUseCase.Request(
                ColumnaNumero: 2,
                NuevoModo: ModoValorizacionColumna.PorVolumen,
                Usuario: "tester"
            );

            Assert.That(async () => await sut.Handle(req, CancellationToken.None),
                        Throws.TypeOf<ConcurrencyException>());
        }

        
    }
}
