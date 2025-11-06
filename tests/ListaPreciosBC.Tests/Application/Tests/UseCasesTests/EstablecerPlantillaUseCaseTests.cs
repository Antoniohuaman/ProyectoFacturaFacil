using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.Interfaces; // IUnitOfWork
using ListaPreciosBC.Application.UseCases;   // EstablecerPlantillaUseCase
using ListaPreciosBC.Domain.Repositories;    // IListaPrecioRepository
using ListaPreciosBC.Domain.Aggregates;      // ListaPrecio
using ListaPreciosBC.Domain.ValueObjects;    // IdentificadorColumnaPrecio, NombreColumnaPrecio, ModoValorizacionColumna, ConfiguracionColumnaPrecio
using NUnit.Framework;
using SharedKernel.Exceptions;
using SharedKernel.Application.Interfaces;   // ITenantContext
using SharedKernel.ValueObjects;             // EmpresaId, TenantId
using Moq;

namespace ListaPreciosBC.Tests.Application.UseCases
{
    public class EstablecerPlantillaUseCaseTests
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
                ListaActiva = aggregate; // mantener activa en el test
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

        // ---------------------- Builder base (lista válida inicial) ----------------------

        private static ListaPrecio CrearListaInicialConBase()
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
            var extraCfg = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.DesdeNumero(2),
                NombreColumnaPrecio.Crear("Minorista"),
                ModoValorizacionColumna.Fijo,
                esBase: false,
                visible: true,
                orden: 2
            );
            var plantilla = PlantillaColumnasPrecio.Crear(new[] { baseCfg, extraCfg });
            var lista = ListaPrecio.CrearNueva(EmpresaId.From("EMP-01"), Guid.NewGuid(), plantilla);
            return lista;
        }

        // ---------------------- Tests ----------------------

        [Test]
        public async Task EstablecerPlantilla_Exito_ReemplazaPlantilla_RespetaInvariantes()
        {
            var repo = new InMemoryListaPrecioRepository();
            var uow = new InMemoryUow();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var sut = new EstablecerPlantillaUseCase(repo, uow, tenant.Object);

            // Lista activa con una plantilla anterior
            var lista = CrearListaInicialConBase();
            repo.Seed(lista);

            var columnas = new List<EstablecerPlantillaUseCase.Columna>
            {
                new(Numero: 2, Nombre: "NuevaBase",  Modo: ModoValorizacionColumna.Fijo,       EsBase: true,  Visible: true,  Orden: 1),
                new(Numero: 3, Nombre: "Mayorista",  Modo: ModoValorizacionColumna.PorVolumen, EsBase: false, Visible: true,  Orden: 2),
                new(Numero: 4, Nombre: "VIP",        Modo: ModoValorizacionColumna.Fijo,       EsBase: false, Visible: false, Orden: 3),
            };

            var req = new EstablecerPlantillaUseCase.Request(
                Columnas: columnas,
                Usuario: "tester"
            );

            var res = await sut.Handle(req, CancellationToken.None);

            Assert.That(res.CantidadColumnas, Is.EqualTo(3));
            Assert.That(res.ColumnaBaseNumero, Is.EqualTo((byte)2));
            Assert.That(res.Version, Is.GreaterThanOrEqualTo(0));

            // Verificación: la plantilla actual contiene exactamente esas 3 columnas,
            // una sola base y con los nombres/modos indicados.
            var persisted = await repo.ObtenerActivaAsync();
            Assert.That(persisted, Is.Not.Null);
            Assert.That(persisted!.Plantilla.Columnas.Count, Is.EqualTo(3));
            Assert.That(persisted.Plantilla.Columnas.Count(c => c.EsBase), Is.EqualTo(1));

            var nombres = persisted.Plantilla.Columnas.Select(c => c.Nombre.Valor).ToList();
            Assert.That(nombres, Is.EquivalentTo(new[] { "NuevaBase", "Mayorista", "VIP" }));

            var modoMayorista = persisted.Plantilla.Columnas.Single(c => c.Nombre.Valor == "Mayorista").Modo;
            Assert.That(modoMayorista, Is.EqualTo(ModoValorizacionColumna.PorVolumen));
        }

        [Test]
        public void EstablecerPlantilla_Falla_SiNoHayListaActiva()
        {
            var repo = new InMemoryListaPrecioRepository { ListaActiva = null };
            var uow = new InMemoryUow();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var sut = new EstablecerPlantillaUseCase(repo, uow, tenant.Object);

            var columnas = new List<EstablecerPlantillaUseCase.Columna>
            {
                new(1, "Base", ModoValorizacionColumna.Fijo, true, true, 1)
            };

            var req = new EstablecerPlantillaUseCase.Request(Columnas: columnas);

            Assert.That(async () => await sut.Handle(req, CancellationToken.None),
                        Throws.TypeOf<NotFoundException>());
        }

        [Test]
        public void EstablecerPlantilla_Falla_SiPlantillaVacia()
        {
            var repo = new InMemoryListaPrecioRepository();
            var uow = new InMemoryUow();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var sut = new EstablecerPlantillaUseCase(repo, uow, tenant.Object);

            var lista = CrearListaInicialConBase();
            repo.Seed(lista);

            var req = new EstablecerPlantillaUseCase.Request(
                Columnas: Array.Empty<EstablecerPlantillaUseCase.Columna>()
            );

            Assert.That(async () => await sut.Handle(req, CancellationToken.None),
                        Throws.TypeOf<BusinessRuleException>());
        }

        [Test]
        public void EstablecerPlantilla_Falla_SiHayMasDeUnaBase()
        {
            var repo = new InMemoryListaPrecioRepository();
            var uow = new InMemoryUow();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var sut = new EstablecerPlantillaUseCase(repo, uow, tenant.Object);

            var lista = CrearListaInicialConBase();
            repo.Seed(lista);

            var columnas = new List<EstablecerPlantillaUseCase.Columna>
            {
                new(1, "BaseA",  ModoValorizacionColumna.Fijo,       true,  true,  1),
                new(2, "BaseB",  ModoValorizacionColumna.PorVolumen, true,  true,  2),
            };

            var req = new EstablecerPlantillaUseCase.Request(Columnas: columnas);

            Assert.That(async () => await sut.Handle(req, CancellationToken.None),
                        Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void EstablecerPlantilla_Falla_SiNoHayBase()
        {
            var repo = new InMemoryListaPrecioRepository();
            var uow = new InMemoryUow();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var sut = new EstablecerPlantillaUseCase(repo, uow, tenant.Object);

            var lista = CrearListaInicialConBase();
            repo.Seed(lista);

            var columnas = new List<EstablecerPlantillaUseCase.Columna>
            {
                new(2, "Minorista",  ModoValorizacionColumna.Fijo,       false, true, 1),
                new(3, "Mayorista",  ModoValorizacionColumna.PorVolumen, false, true, 2),
            };

            var req = new EstablecerPlantillaUseCase.Request(Columnas: columnas);

            Assert.That(async () => await sut.Handle(req, CancellationToken.None),
                        Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void EstablecerPlantilla_FallaPorConcurrencia_SiVersionCambiaEntreLoadYSave()
        {
            var repo = new InMemoryListaPrecioRepository { SimularConcurrencia = true };
            var uow = new InMemoryUow();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var sut = new EstablecerPlantillaUseCase(repo, uow, tenant.Object);

            var lista = CrearListaInicialConBase();
            repo.Seed(lista);

            var columnas = new List<EstablecerPlantillaUseCase.Columna>
            {
                new(2, "NuevaBase",  ModoValorizacionColumna.Fijo,       true,  true,  1),
                new(3, "Mayorista",  ModoValorizacionColumna.PorVolumen, false, true,  2),
            };

            var req = new EstablecerPlantillaUseCase.Request(
                Columnas: columnas,
                Usuario: "tester"
            );

            Assert.That(async () => await sut.Handle(req, CancellationToken.None),
                        Throws.TypeOf<ConcurrencyException>());
        }

        
    }
}
