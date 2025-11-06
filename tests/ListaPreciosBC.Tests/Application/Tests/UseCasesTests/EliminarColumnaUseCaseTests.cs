using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.Interfaces; // IUnitOfWork
using ListaPreciosBC.Application.UseCases;   // EliminarColumnaUseCase
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
    public class EliminarColumnaUseCaseTests
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
                    throw new ConcurrencyException("ListaPrecio", id.ToString(), expectedVersion, loaded);

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

        // ---------------------- Builders con invariantes ----------------------

        private static ListaPrecio CrearListaConBaseYDosExtras()
        {
            // Usar el factory estático y construir la plantilla
            var baseCfg = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.DesdeNumero(1),
                NombreColumnaPrecio.Crear("Base"),
                ModoValorizacionColumna.Fijo,
                esBase: true,
                visible: true,
                orden: 1
            );
            var extra2 = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.DesdeNumero(2),
                NombreColumnaPrecio.Crear("Minorista"),
                ModoValorizacionColumna.Fijo,
                esBase: false,
                visible: true,
                orden: 2
            );
            var extra3 = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.DesdeNumero(3),
                NombreColumnaPrecio.Crear("Mayorista"),
                ModoValorizacionColumna.PorVolumen,
                esBase: false,
                visible: true,
                orden: 3
            );
            var plantilla = PlantillaColumnasPrecio.Crear(new[] { baseCfg, extra2, extra3 });
            var lista = ListaPrecio.CrearNueva(EmpresaId.From("EMP-01"), Guid.NewGuid(), plantilla);
            return lista;
        }

        // ---------------------- Tests ----------------------

        [Test]
        public async Task EliminarColumna_Exito_EliminaExtra_ReordenaSinDuplicados_YPersiste()
        {
            var repo = new InMemoryListaPrecioRepository();
            var uow = new InMemoryUow();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var sut = new EliminarColumnaUseCase(repo, uow, tenant.Object);

            var lista = CrearListaConBaseYDosExtras();
            repo.Seed(lista);

            // Eliminar la columna #3 (no base)
            var req = new EliminarColumnaUseCase.Request(
                ColumnaNumero: 3,
                Usuario: "tester"
            );

            var res = await sut.Handle(req, CancellationToken.None);

            Assert.That(res.ColumnaNumero, Is.EqualTo(3));
            Assert.That(res.Version, Is.GreaterThanOrEqualTo(0));

            // Verificar que #3 ya no existe y que órdenes siguen únicos/contiguos
            var persisted = await repo.ObtenerActivaAsync();
            var existe3 = persisted!.Plantilla.Columnas.Any(c => c.Id.Equals(IdentificadorColumnaPrecio.DesdeNumero(3)));
            Assert.That(existe3, Is.False);

            var ordenes = persisted.Plantilla.Columnas.Select(c => c.Orden).ToList();
            Assert.That(ordenes.Distinct().Count(), Is.EqualTo(ordenes.Count)); // unicidad
            Assert.That(ordenes.Min(), Is.GreaterThanOrEqualTo((byte)1));
        }

        [Test]
        public void EliminarColumna_Falla_SiNoHayListaActiva()
        {
            var repo = new InMemoryListaPrecioRepository { ListaActiva = null };
            var uow = new InMemoryUow();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var sut = new EliminarColumnaUseCase(repo, uow, tenant.Object);

            var req = new EliminarColumnaUseCase.Request(ColumnaNumero: 2);

            Assert.That(async () => await sut.Handle(req, CancellationToken.None),
                        Throws.TypeOf<NotFoundException>());
        }

        [Test]
        public void EliminarColumna_Falla_SiColumnaNoExiste()
        {
            var repo = new InMemoryListaPrecioRepository();
            var uow = new InMemoryUow();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var sut = new EliminarColumnaUseCase(repo, uow, tenant.Object);

            var lista = CrearListaConBaseYDosExtras();
            repo.Seed(lista);

            var req = new EliminarColumnaUseCase.Request(ColumnaNumero: 9); // no existe

            Assert.That(async () => await sut.Handle(req, CancellationToken.None),
                        Throws.TypeOf<NotFoundException>());
        }

        [Test]
        public void EliminarColumna_Falla_SiEsBase()
        {
            var repo = new InMemoryListaPrecioRepository();
            var uow = new InMemoryUow();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var sut = new EliminarColumnaUseCase(repo, uow, tenant.Object);

            var lista = CrearListaConBaseYDosExtras();
            repo.Seed(lista);

            var req = new EliminarColumnaUseCase.Request(ColumnaNumero: 1); // base

            Assert.That(async () => await sut.Handle(req, CancellationToken.None),
                        Throws.TypeOf<BusinessRuleException>());
        }

        [Test]
        public void EliminarColumna_FallaPorConcurrencia_SiVersionCambiaEntreLoadYSave()
        {
            var repo = new InMemoryListaPrecioRepository { SimularConcurrencia = true };
            var uow = new InMemoryUow();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var sut = new EliminarColumnaUseCase(repo, uow, tenant.Object);

            var lista = CrearListaConBaseYDosExtras();
            repo.Seed(lista);

            var req = new EliminarColumnaUseCase.Request(
                ColumnaNumero: 3,
                Usuario: "tester"
            );

            Assert.That(async () => await sut.Handle(req, CancellationToken.None),
                        Throws.TypeOf<ConcurrencyException>());
        }

        
    }
}
