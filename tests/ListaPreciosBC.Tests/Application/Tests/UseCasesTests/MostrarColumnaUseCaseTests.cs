using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.UseCases;   // MostrarColumnaUseCase
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
    public class MostrarColumnaUseCaseTests
    {
        // ---------------------- Fake InMemory ----------------------

    private sealed class InMemoryListaPrecioRepository : IListaPrecioRepository
        {
            private readonly Dictionary<Guid, ListaPrecio> _store = new();
            public ListaPrecio? ListaActiva { get; set; }

            public Task<ListaPrecio?> ObtenerActivaAsync(EmpresaId empresaId, Guid? sucursalId = null, CancellationToken ct = default)
                => Task.FromResult(ListaActiva);

            public Task<ListaPrecio?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default)
            {
                _store.TryGetValue(id, out var lp);
                return Task.FromResult(lp);
            }

            public Task GuardarAsync(ListaPrecio aggregate, EmpresaId empresaId, Guid? sucursalId, int expectedVersion, CancellationToken ct = default)
            {
                _store[aggregate.Id] = aggregate;
                return Task.CompletedTask;
            }

            public void Seed(ListaPrecio lista)
            {
                _store[lista.Id] = lista;
                ListaActiva = lista;
            }
        }

        // ---------------------- Builder con invariantes ----------------------
        private static ListaPrecio CrearListaConBaseYExtra()
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
                visible: true,
                orden: 2
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
        public async Task MostrarColumna_Exito_DevuelveDatosDeColumna()
        {
            var repo = new InMemoryListaPrecioRepository();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var sut = new MostrarColumnaUseCase(repo, tenant.Object);

            var lista = CrearListaConBaseYExtra();
            repo.Seed(lista);

            var req = new MostrarColumnaUseCase.Request(ColumnaNumero: 2);
            var res = await sut.Handle(req, CancellationToken.None);

            Assert.That(res.ColumnaNumero, Is.EqualTo(2));
            Assert.That(res.Nombre, Is.EqualTo("Mayorista"));
            Assert.That(res.Modo, Is.EqualTo(ModoValorizacionColumna.PorVolumen.ToString()));
            Assert.That(res.EsBase, Is.False);
            Assert.That(res.Visible, Is.True);
            Assert.That(res.Orden, Is.EqualTo((byte)2));
        }

        [Test]
        public async Task MostrarColumna_Exito_ColumnaBase()
        {
            var repo = new InMemoryListaPrecioRepository();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var sut = new MostrarColumnaUseCase(repo, tenant.Object);

            var lista = CrearListaConBaseYExtra();
            repo.Seed(lista);

            var req = new MostrarColumnaUseCase.Request(ColumnaNumero: 1);
            var res = await sut.Handle(req, CancellationToken.None);

            Assert.That(res.ColumnaNumero, Is.EqualTo(1));
            Assert.That(res.Nombre, Is.EqualTo("Base"));
            Assert.That(res.Modo, Is.EqualTo(ModoValorizacionColumna.Fijo.ToString()));
            Assert.That(res.EsBase, Is.True);
            Assert.That(res.Visible, Is.True);
            Assert.That(res.Orden, Is.EqualTo((byte)1));
        }

        [Test]
        public void MostrarColumna_Falla_SiNoHayListaActiva()
        {
            var repo = new InMemoryListaPrecioRepository { ListaActiva = null };
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var sut = new MostrarColumnaUseCase(repo, tenant.Object);

            var req = new MostrarColumnaUseCase.Request(ColumnaNumero: 1);

            Assert.That(async () => await sut.Handle(req, CancellationToken.None),
                        Throws.TypeOf<NotFoundException>());
        }

        [Test]
        public void MostrarColumna_Falla_SiColumnaNoExiste()
        {
            var repo = new InMemoryListaPrecioRepository();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var sut = new MostrarColumnaUseCase(repo, tenant.Object);

            var lista = CrearListaConBaseYExtra();
            repo.Seed(lista);

            var req = new MostrarColumnaUseCase.Request(ColumnaNumero: 9); // no existe

            Assert.That(async () => await sut.Handle(req, CancellationToken.None),
                        Throws.TypeOf<NotFoundException>());
        }

        
    }
}
