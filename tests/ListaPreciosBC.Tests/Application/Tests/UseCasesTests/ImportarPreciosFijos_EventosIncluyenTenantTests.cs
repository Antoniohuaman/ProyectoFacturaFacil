using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.UseCases;
using ListaPreciosBC.Domain.Aggregates;
using ListaPreciosBC.Domain.Repositories;
using ListaPreciosBC.Domain.ValueObjects;
using ListaPreciosBC.Domain.Events;
using NUnit.Framework;
using Moq;
using SharedKernel.ValueObjects;
using SharedKernel.Application.Interfaces;   // ITenantContext
using ListaPreciosBC.Application.Interfaces; // ICatalogoReadModel

namespace ListaPreciosBC.Tests.Application.UseCases
{
    public class ImportarPreciosFijos_EventosIncluyenTenantTests
    {
        // ---------------------- InMemory fakes ----------------------
        private sealed class InMemoryListaPrecioRepository : IListaPrecioRepository
        {
            public ListaPrecio? ListaActiva { get; set; }

            public Task<ListaPrecio?> ObtenerActivaAsync(EmpresaId empresaId, Guid? sucursalId = null, CancellationToken ct = default)
                => Task.FromResult(ListaActiva);

            public Task<ListaPrecio?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default)
                => Task.FromResult(ListaActiva is not null && ListaActiva.Id == id ? ListaActiva : null);

            public Task GuardarAsync(ListaPrecio aggregate, EmpresaId empresaId, Guid? sucursalId, int expectedVersion, CancellationToken ct = default)
                => Task.CompletedTask;
        }

        private sealed class InMemoryPrecioProductoRepository : IPrecioProductoRepository
        {
            private readonly Dictionary<string, PrecioProducto> _store = new();
            private readonly Dictionary<string, int> _loadedVersion = new();
            private static string Key(ProductoId id) => id.Value.ToString();
            private string? _lastKey;

            public Task<PrecioProducto?> ObtenerPorProductoIdAsync(EmpresaId empresaId, EstablecimientoId? establecimientoId, ProductoId productoId, CancellationToken ct = default)
            {
                var k = Key(productoId);
                _store.TryGetValue(k, out var agg);
                if (agg is not null) _loadedVersion[k] = agg.Version;
                _lastKey = k;
                return Task.FromResult<PrecioProducto?>(agg);
            }

            public Task GuardarAsync(PrecioProducto aggregate, EmpresaId empresaId, EstablecimientoId? establecimientoId, int expectedVersion, CancellationToken ct = default)
            {
                var k = _lastKey ?? Key(aggregate.ProductoId);
                if (_loadedVersion.TryGetValue(k, out var loaded) && loaded != expectedVersion)
                    throw new SharedKernel.Exceptions.ConcurrencyException("Versión inesperada.", k, expectedVersion, loaded);
                _store[k] = aggregate;
                _loadedVersion[k] = aggregate.Version;
                return Task.CompletedTask;
            }

            public Task EliminarAsync(EmpresaId empresaId, EstablecimientoId? establecimientoId, ProductoId productoId, int? expectedVersion = null, CancellationToken ct = default)
            {
                var k = Key(productoId);
                _store.Remove(k);
                _loadedVersion.Remove(k);
                return Task.CompletedTask;
            }

            public void Seed(PrecioProducto agg)
            {
                var k = Key(agg.ProductoId);
                agg.ClearDomainEvents();
                _store[k] = agg;
                _loadedVersion[k] = agg.Version;
            }

            public Task<PrecioProducto?> ObtenerPorProductoIdAsync(ProductoId productoId, CancellationToken ct = default)
                => Task.FromResult(_store.TryGetValue(Key(productoId), out var agg) ? agg : null);
        }

        private sealed class UnitOfWorkInMemory : ListaPreciosBC.Application.Interfaces.IUnitOfWork
        {
            public int SaveChangesCount { get; private set; }
            public Task SaveChangesAsync(CancellationToken ct = default) { SaveChangesCount++; return Task.CompletedTask; }
        }

        private static ListaPrecio CrearListaActivaConColumnaFija(byte numero = 1)
        {
            var cfg = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.DesdeNumero(numero),
                NombreColumnaPrecio.Crear("Base"),
                ModoValorizacionColumna.Fijo,
                esBase: numero == 1,
                visible: true,
                orden: numero
            );
            var plantilla = PlantillaColumnasPrecio.Crear(new[] { cfg });
            return ListaPrecio.CrearNueva(EmpresaId.From("EMP-01"), Guid.NewGuid(), plantilla);
        }

        private static PrecioProducto NuevoAggConSucursal(EmpresaId empresaId, Guid sucursalId, out ProductoId productoId)
        {
            productoId = ProductoId.New();
            var agg = PrecioProducto.CrearNuevo(empresaId, productoId, sucursalId);
            agg.ClearDomainEvents();
            return agg;
        }

        [Test]
        public async Task ImportarPreciosFijos_EventosIncluyenTenant_paraCadaAgregado()
        {
            // Arrange
            var empresa = EmpresaId.From("EMP-01");
            var sucursal = Guid.Parse("11111111-2222-3333-4444-555555555555");
            var listaRepo = new InMemoryListaPrecioRepository { ListaActiva = CrearListaActivaConColumnaFija(1) };
            var precioRepo = new InMemoryPrecioProductoRepository();
            var uow = new UnitOfWorkInMemory();

            // Seed dos agregados para SKU-1 y SKU-2 con esa sucursal, para que eventos lleven EstablecimientoId
            var agg1 = NuevoAggConSucursal(empresa, sucursal, out var prod1);
            var agg2 = NuevoAggConSucursal(empresa, sucursal, out var prod2);
            precioRepo.Seed(agg1);
            precioRepo.Seed(agg2);

            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(empresa);

            var catalogo = new Mock<ICatalogoReadModel>();
            catalogo.Setup(c => c.TryGetProductoIdBySkuAsync(empresa, "SKU-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(prod1);
            catalogo.Setup(c => c.TryGetProductoIdBySkuAsync(empresa, "SKU-2", It.IsAny<CancellationToken>()))
                .ReturnsAsync(prod2);

            var sut = new ImportarPreciosFijosUseCase(listaRepo, precioRepo, uow, tenant.Object, catalogo.Object);

            var filas = new List<ImportarPreciosFijosUseCase.Fila>
            {
                new("SKU-1", new List<ImportarPreciosFijosUseCase.ItemPrecio>{ new(1, 25m, DateTime.UtcNow.Date, null, true) }),
                new("SKU-2", new List<ImportarPreciosFijosUseCase.ItemPrecio>{ new(1, 30m, DateTime.UtcNow.Date, null, true) })
            };

            // Act
            var resp = await sut.Handle(new ImportarPreciosFijosUseCase.Request(
                Filas: filas,
                Usuario: "tester",
                Cuando: DateTimeOffset.UtcNow,
                CantidadReferenciaParaEventoBase: 1,
                DetenerAntePrimerError: false
            ), CancellationToken.None);

            // Assert: single commit
            Assert.That(uow.SaveChangesCount, Is.EqualTo(1));
            Assert.That(resp.AgregadosAfectados, Is.EqualTo(2));

            var post1 = await precioRepo.ObtenerPorProductoIdAsync(prod1);
            var post2 = await precioRepo.ObtenerPorProductoIdAsync(prod2);
            Assert.That(post1, Is.Not.Null);
            Assert.That(post2, Is.Not.Null);

            var ev1 = post1!.DomainEvents.OfType<PrecioFijoActualizado>().LastOrDefault();
            var ev2 = post2!.DomainEvents.OfType<PrecioFijoActualizado>().LastOrDefault();
            Assert.That(ev1, Is.Not.Null);
            Assert.That(ev2, Is.Not.Null);
            Assert.That(ev1!.EmpresaId, Is.EqualTo(empresa));
            Assert.That(ev2!.EmpresaId, Is.EqualTo(empresa));
            Assert.That(ev1!.EstablecimientoId, Is.EqualTo(sucursal));
            Assert.That(ev2!.EstablecimientoId, Is.EqualTo(sucursal));
        }
    }
}
