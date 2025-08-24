using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.UseCases;   // ObtenerPlantillaUseCase
using ListaPreciosBC.Domain.Repositories;    // IListaPrecioRepository
using ListaPreciosBC.Domain.Aggregates;      // ListaPrecio
using ListaPreciosBC.Domain.ValueObjects;    // IdentificadorColumnaPrecio, NombreColumnaPrecio, ModoValorizacionColumna
using NUnit.Framework;
using SharedKernel.Exceptions;

namespace ListaPreciosBC.Tests.Application.UseCases
{
    public class ObtenerPlantillaUseCaseTests
    {
        // ---------------------- Fake InMemory ----------------------

        private sealed class InMemoryListaPrecioRepository : IListaPrecioRepository
        {
            private readonly Dictionary<Guid, ListaPrecio> _store = new();
            public ListaPrecio? ListaActiva { get; set; }

            public Task<ListaPrecio?> ObtenerActivaAsync(CancellationToken ct = default)
                => Task.FromResult(ListaActiva);

            public Task<ListaPrecio?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default)
            {
                _store.TryGetValue(id, out var lp);
                return Task.FromResult(lp);
            }

            public Task GuardarAsync(ListaPrecio aggregate, int expectedVersion, CancellationToken ct = default)
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

        // ---------------------- Builders con invariantes ----------------------

        private static ListaPrecio CrearListaConBaseYDosExtras()
        {
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
                visible: false, // la probamos oculta
                orden: 3
            );
            var plantilla = PlantillaColumnasPrecio.Crear(new[] { baseCfg, extra2, extra3 });
            var lista = ListaPrecio.CrearNueva(Guid.NewGuid(), plantilla);
            return lista;
        }

        // ---------------------- Tests ----------------------

        [Test]
        public async Task ObtenerPlantilla_Exito_DevuelveColumnasOrdenadas_ConUnicaBase()
        {
            var repo = new InMemoryListaPrecioRepository();
            var sut = new ObtenerPlantillaUseCase(repo);

            var lista = CrearListaConBaseYDosExtras();
            repo.Seed(lista);

            var res = await sut.Handle(new ObtenerPlantillaUseCase.Request(), CancellationToken.None);

            Assert.That(res.Version, Is.GreaterThanOrEqualTo(0));
            Assert.That(res.CantidadColumnas, Is.EqualTo(3));
            Assert.That(res.Columnas.Length, Is.EqualTo(3));

            // Orden por 'Orden'
            Assert.That(res.Columnas.Select(c => c.Orden).ToArray(), Is.EqualTo(new byte[] { 1, 2, 3 }));

            // Unicidad de base
            Assert.That(res.Columnas.Count(c => c.EsBase), Is.EqualTo(1));

            // Nombres/modes esperados (sin depender del número)
            var nombres = res.Columnas.Select(c => c.Nombre).ToList();
            Assert.That(nombres, Is.EquivalentTo(new[] { "Base", "Minorista", "Mayorista" }));

            var modoMayorista = res.Columnas.Single(c => c.Nombre == "Mayorista").Modo;
            Assert.That(modoMayorista, Is.EqualTo(ModoValorizacionColumna.PorVolumen.ToString()));

            // Visibilidad de la última
            Assert.That(res.Columnas.Single(c => c.Nombre == "Mayorista").Visible, Is.False);
        }

        [Test]
        public void ObtenerPlantilla_Falla_SiNoHayListaActiva()
        {
            var repo = new InMemoryListaPrecioRepository { ListaActiva = null };
            var sut = new ObtenerPlantillaUseCase(repo);

            Assert.That(async () => await sut.Handle(new ObtenerPlantillaUseCase.Request(), CancellationToken.None),
                        Throws.TypeOf<NotFoundException>());
        }

        [Test]
        public async Task ObtenerPlantilla_Exito_SoloBase()
        {
            var repo = new InMemoryListaPrecioRepository();
            var sut = new ObtenerPlantillaUseCase(repo);

            // Solo la base
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
            repo.Seed(lista);

            var res = await sut.Handle(new ObtenerPlantillaUseCase.Request(), CancellationToken.None);

            Assert.That(res.CantidadColumnas, Is.EqualTo(1));
            Assert.That(res.Columnas.Single().Nombre, Is.EqualTo("Base"));
            Assert.That(res.Columnas.Single().EsBase, Is.True);
        }
    }
}
