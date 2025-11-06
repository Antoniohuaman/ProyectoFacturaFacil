using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.Interfaces; // IUnitOfWork
using ListaPreciosBC.Application.UseCases;   // CambiarOrdenColumnaUseCase
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
    public class CambiarOrdenColumnaUseCaseTests
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
            public int CommitCount { get; private set; }
            public Task CommitAsync(CancellationToken ct = default)
            {
                CommitCount++;
                return Task.CompletedTask;
            }
        }

        // ---------------------- Builder con invariantes ----------------------

        private static ListaPrecio CrearListaConBaseYDosExtras()
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
        public async Task CambiarOrden_Exito_MueveColumnaYPersiste_SinDuplicarOrdenes()
        {
            var repo = new InMemoryListaPrecioRepository();
            var uow = new InMemoryUow();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var sut = new CambiarOrdenColumnaUseCase(repo, uow, tenant.Object);

            var lista = CrearListaConBaseYDosExtras();
            repo.Seed(lista);

            // mover columna #3 (Mayorista) a orden 2
            var req = new CambiarOrdenColumnaUseCase.Request(
                ColumnaNumero: 3,
                NuevoOrden: 2,
                Usuario: "tester"
            );

            var res = await sut.Handle(req, CancellationToken.None);

            Assert.That(res.ColumnaNumero, Is.EqualTo(3));
            Assert.That(res.Orden, Is.EqualTo(2));
            Assert.That(res.Version, Is.GreaterThanOrEqualTo(0));

            // verificación de efecto: orden actualizado y sin duplicados
            var persisted = await repo.ObtenerActivaAsync();
            var colId3 = IdentificadorColumnaPrecio.DesdeNumero(3);
            var orden3 = persisted!.Plantilla.Columnas.Single(c => c.Id.Equals(colId3)).Orden;
            Assert.That(orden3, Is.EqualTo(2));

            var ordenes = persisted!.Plantilla.Columnas.Select(c => c.Orden).ToList();
            Assert.That(ordenes.Distinct().Count(), Is.EqualTo(ordenes.Count)); // unicidad
        }

        [Test]
        public void CambiarOrden_Falla_SiNoHayListaActiva()
        {
            var repo = new InMemoryListaPrecioRepository { ListaActiva = null };
            var uow = new InMemoryUow();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var sut = new CambiarOrdenColumnaUseCase(repo, uow, tenant.Object);

            var req = new CambiarOrdenColumnaUseCase.Request(
                ColumnaNumero: 2,
                NuevoOrden: 2
            );

            Assert.That(async () => await sut.Handle(req, CancellationToken.None),
                        Throws.TypeOf<NotFoundException>());
        }

        [Test]
        public void CambiarOrden_Falla_SiColumnaNoExiste()
        {
            var repo = new InMemoryListaPrecioRepository();
            var uow = new InMemoryUow();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var sut = new CambiarOrdenColumnaUseCase(repo, uow, tenant.Object);

            var lista = CrearListaConBaseYDosExtras();
            repo.Seed(lista);

            var req = new CambiarOrdenColumnaUseCase.Request(
                ColumnaNumero: 9, // no existe
                NuevoOrden: 2
            );

            Assert.That(async () => await sut.Handle(req, CancellationToken.None),
                        Throws.TypeOf<NotFoundException>());
        }

        [Test]
        public void CambiarOrden_Falla_SiOrdenEsMenorQueUno()
        {
            var repo = new InMemoryListaPrecioRepository();
            var uow = new InMemoryUow();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var sut = new CambiarOrdenColumnaUseCase(repo, uow, tenant.Object);

            var lista = CrearListaConBaseYDosExtras();
            repo.Seed(lista);

            var req = new CambiarOrdenColumnaUseCase.Request(
                ColumnaNumero: 2,
                NuevoOrden: 0 // inválido
            );

            Assert.That(async () => await sut.Handle(req, CancellationToken.None),
                        Throws.TypeOf<BusinessRuleException>());
        }

        [Test]
        public void CambiarOrden_FallaPorConcurrencia_SiVersionCambiaEntreLoadYSave()
        {
            var repo = new InMemoryListaPrecioRepository { SimularConcurrencia = true };
            var uow = new InMemoryUow();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var sut = new CambiarOrdenColumnaUseCase(repo, uow, tenant.Object);

            var lista = CrearListaConBaseYDosExtras();
            repo.Seed(lista);

            var req = new CambiarOrdenColumnaUseCase.Request(
                ColumnaNumero: 3,
                NuevoOrden: 2,
                Usuario: "tester"
            );

            Assert.That(async () => await sut.Handle(req, CancellationToken.None),
                        Throws.TypeOf<ConcurrencyException>());
        }

        
    }
}
