using System;
using System.Linq;
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
    public class EliminarPrecioFijo_EventoIncluyeTenantTests
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
            private readonly System.Collections.Generic.Dictionary<string, PrecioProducto> _store = new();
            private readonly System.Collections.Generic.Dictionary<string, int> _loadedVersion = new();
            private string? _lastKey;
            private static string Key(ProductoId id) => id.Value.ToString();

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
                agg.ClearDomainEvents(); // limpiar eventos previos
                _store[k] = agg;
                _loadedVersion[k] = agg.Version;
            }

            public Task<PrecioProducto?> ObtenerPorProductoIdAsync(ProductoId productoId, CancellationToken ct = default)
                => Task.FromResult(_store.TryGetValue(Key(productoId), out var agg) ? agg : null);
        }

        private sealed class UnitOfWorkInMemory : ListaPreciosBC.Application.Interfaces.IUnitOfWork
        {
            public int CommitCount { get; private set; }
            public Task CommitAsync(CancellationToken ct = default) { CommitCount++; return Task.CompletedTask; }
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

        private static PrecioProducto SeedAggConFijo(EmpresaId empresaId, Guid? sucursalId, out ProductoId productoId)
        {
            productoId = ProductoId.New();
            var agg = PrecioProducto.CrearNuevo(empresaId, productoId, sucursalId);
            agg.UpsertPrecioFijo(
                IdentificadorColumnaPrecio.DesdeNumero(1),
                ValorPrecio.DesdeMonto(10m, Moneda.PEN(), true),
                PeriodoVigencia.Crear(DateTimeOffset.UtcNow.AddDays(-10), null),
                usuario: "seed",
                cuando: DateTimeOffset.UtcNow.AddDays(-10)
            );
            agg.ClearDomainEvents();
            return agg;
        }

        [Test]
        public async Task EliminarPrecioFijo_EventoIncluyeTenant_Est()
        {
            // Arrange
            var empresa = EmpresaId.From("EMP-01");
            var sucursal = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var listaRepo = new InMemoryListaPrecioRepository { ListaActiva = CrearListaActivaConColumnaFija(1) };
            var precioRepo = new InMemoryPrecioProductoRepository();
            var uow = new UnitOfWorkInMemory();

            var agg = SeedAggConFijo(empresa, sucursal, out var productoId);
            precioRepo.Seed(agg);

            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(empresa);

            var catalogo = new Mock<ICatalogoReadModel>();
            catalogo.Setup(c => c.TryGetProductoIdBySkuAsync(empresa, "SKU-DEL", It.IsAny<CancellationToken>()))
                .ReturnsAsync(productoId);

            var sut = new EliminarPrecioFijoUseCase(precioRepo, listaRepo, uow, tenant.Object, catalogo.Object);

            // Act
            var res = await sut.Handle(new EliminarPrecioFijoUseCase.Request(
                Sku: "SKU-DEL",
                ColumnaNumero: 1,
                UnidadMedidaCodigo: UnidadDeMedida.NIU.Codigo,
                LanzarSiNoExiste: true,
                Usuario: "tester",
                Cuando: DateTimeOffset.UtcNow
            ), CancellationToken.None);

            // Assert: UoW commit once
            Assert.That(uow.CommitCount, Is.EqualTo(1));
            Assert.That(res.Sku, Is.EqualTo("SKU-DEL"));
            Assert.That(res.ColumnaNumero, Is.EqualTo(1));
            Assert.That(res.UnidadMedidaCodigo, Is.EqualTo(UnidadDeMedida.NIU.Codigo));

            var post = await precioRepo.ObtenerPorProductoIdAsync(productoId);
            Assert.That(post, Is.Not.Null);
            var evt = post!.DomainEvents.OfType<PrecioColumnaActualizada>().LastOrDefault();
            Assert.That(evt, Is.Not.Null);
            Assert.That(evt!.EmpresaId, Is.EqualTo(empresa));
            Assert.That(evt!.EstablecimientoId, Is.EqualTo(sucursal));
        }
    }
}
