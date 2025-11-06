using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.UseCases;   // ListarPreciosDeSkuUseCase
using ListaPreciosBC.Domain.Repositories;    // IListaPrecioRepository, IPrecioProductoRepository
using ListaPreciosBC.Domain.Aggregates;      // ListaPrecio, PrecioProducto
using ListaPreciosBC.Domain.ValueObjects;    // IdentificadorColumnaPrecio, NombreColumnaPrecio, ModoValorizacionColumna, ValorPrecio, PeriodoVigencia, TramoVolumen, MatrizVolumen
using NUnit.Framework;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;             // Moneda
using SharedKernel.Application.Interfaces;   // ITenantContext
using Moq;

namespace ListaPreciosBC.Tests.Application.UseCases
{
    public class ListarPreciosDeSkuUseCaseTests
    {
        // ---------------------- Fakes InMemory ----------------------

        private sealed class InMemoryListaPrecioRepository : IListaPrecioRepository
        {
            public ListaPrecio? ListaActiva { get; set; }

            public Task<ListaPrecio?> ObtenerActivaAsync(EmpresaId empresaId, Guid? sucursalId = null, CancellationToken ct = default)
                => Task.FromResult(ListaActiva);

            public Task<ListaPrecio?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default)
                => Task.FromResult(ListaActiva is not null && ListaActiva.Id == id ? ListaActiva : null);

            public Task GuardarAsync(ListaPrecio aggregate, EmpresaId empresaId, Guid? sucursalId, int expectedVersion, CancellationToken ct = default)
                => Task.CompletedTask;

            public void Seed(ListaPrecio lista) => ListaActiva = lista;
        }

        private sealed class InMemoryPrecioProductoRepository : IPrecioProductoRepository
        {
            private readonly Dictionary<string, PrecioProducto> _store = new();

            private static string Key(ProductoId productoId) => productoId.Value.ToString();

            public Task<PrecioProducto?> ObtenerPorProductoIdAsync(EmpresaId empresaId, EstablecimientoId? establecimientoId, ProductoId productoId, CancellationToken ct = default)
            {
                _store.TryGetValue(Key(productoId), out var agg);
                return Task.FromResult<PrecioProducto?>(agg);
            }

            public Task GuardarAsync(PrecioProducto aggregate, EmpresaId empresaId, EstablecimientoId? establecimientoId, int expectedVersion, CancellationToken ct = default)
                => Task.CompletedTask;

            public void Seed(PrecioProducto agg) => _store[Key(agg.ProductoId)] = agg;

            public Task EliminarAsync(EmpresaId empresaId, EstablecimientoId? establecimientoId, ProductoId productoId, int? expectedVersion = null, CancellationToken ct = default)
            {
                _store.Remove(Key(productoId));
                return Task.CompletedTask;
            }

            // Helper para asserts si hiciera falta
            public Task<PrecioProducto?> ObtenerPorProductoIdAsync(ProductoId productoId, CancellationToken ct = default)
            {
                _store.TryGetValue(Key(productoId), out var agg);
                return Task.FromResult(agg);
            }
        }

        

        // ---------------------- Builders con invariantes ----------------------

        private static ListaPrecio CrearListaConBaseVolumenYExtraOculta()
        {
            var baseCfg = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.DesdeNumero(1),
                NombreColumnaPrecio.Crear("Base"),
                ModoValorizacionColumna.Fijo,
                esBase: true,
                visible: true,
                orden: 1
            );
            var volCfg = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.DesdeNumero(2),
                NombreColumnaPrecio.Crear("Mayorista"),
                ModoValorizacionColumna.PorVolumen,
                esBase: false,
                visible: true,
                orden: 2
            );
            var ocultaCfg = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.DesdeNumero(3),
                NombreColumnaPrecio.Crear("VIP"),
                ModoValorizacionColumna.Fijo,
                esBase: false,
                visible: false,   // oculta
                orden: 3
            );
            var plantilla = PlantillaColumnasPrecio.Crear(new[] { baseCfg, volCfg, ocultaCfg });
            var lista = ListaPrecio.CrearNueva(EmpresaId.From("EMP-01"), Guid.NewGuid(), plantilla);
            return lista;
        }

        private static PrecioProducto CrearPrecioProducto(string sku)
            => PrecioProducto.CrearNuevo(EmpresaId.From("EMP-01"), ProductoId.New());

        // ---------------------- Tests ----------------------

        [Test]
        public async Task ListarPrecios_Exito_TraePorCadaColumna_PreciosVigentesYNulls()
        {
            var listaRepo = new InMemoryListaPrecioRepository { ListaActiva = CrearListaConBaseVolumenYExtraOculta() };
            var precioRepo = new InMemoryPrecioProductoRepository();

            const string sku = "SKU-001";
            var agg = CrearPrecioProducto(sku);

            // Base (fijo) vigente
            agg.UpsertPrecioFijo(
                IdentificadorColumnaPrecio.DesdeNumero(1),
                ValorPrecio.DesdeMonto(10.50m, Moneda.PEN(), true),
                PeriodoVigencia.Crear(DateTimeOffset.UtcNow.AddDays(-30), null),
                "seed",
                DateTimeOffset.UtcNow.AddDays(-30)
            );

            // Mayorista (volumen) vigente
            var tramos = new List<TramoVolumen>
            {
                TramoVolumen.Crear(1, 9, ValorPrecio.DesdeMonto(9m, Moneda.PEN(), true)),
                TramoVolumen.Crear(10, null, ValorPrecio.DesdeMonto(8.5m, Moneda.PEN(), true)),
            };
            agg.UpsertMatrizVolumen(
                IdentificadorColumnaPrecio.DesdeNumero(2),
                MatrizVolumen.Crear(tramos),
                "seed",
                DateTimeOffset.UtcNow.AddDays(-30),
                cantidadReferenciaParaEventoBase: 1
            );

            // VIP (oculta) — NO definimos precio para probar Monto=null
            precioRepo.Seed(agg);

            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var catalogo = new Moq.Mock<ListaPreciosBC.Application.Interfaces.ICatalogoReadModel>();
            catalogo.Setup(c => c.TryGetProductoIdBySkuAsync(It.IsAny<EmpresaId>(), It.Is<string>(s => s == sku), It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg.ProductoId);
            var sut = new ListarPreciosDeSkuUseCase(listaRepo, precioRepo, tenant.Object, catalogo.Object);

            var res = await sut.Handle(
                new ListarPreciosDeSkuUseCase.Request(
                    Sku: sku,
                    Cantidad: 1,
                    Fecha: DateTimeOffset.UtcNow,
                    SoloVisibles: false // incluir también la oculta
                ),
                CancellationToken.None
            );

            Assert.That(res.Sku, Is.EqualTo(sku));
            Assert.That(res.Cantidad, Is.EqualTo(1));
            Assert.That(res.PreciosPorColumna.Length, Is.EqualTo(3));
            Assert.That(res.VersionAgregado, Is.GreaterThanOrEqualTo(0));

            // Base
            var baseItem = res.PreciosPorColumna.Single(p => p.ColumnaNumero == 1);
            Assert.That(baseItem.NombreColumna, Is.EqualTo("Base"));
            Assert.That(baseItem.ModoColumna, Is.EqualTo(ModoValorizacionColumna.Fijo.ToString()));
            Assert.That(baseItem.Monto, Is.EqualTo(10.50m));
            Assert.That(baseItem.Moneda, Is.EqualTo(Moneda.PEN().Codigo));
            Assert.That(baseItem.IncluyeImpuesto, Is.True);
            Assert.That(baseItem.EsBase, Is.True);
            Assert.That(baseItem.Visible, Is.True);

            // Mayorista (volumen) para cantidad 1 ⇒ 9
            var mayItem = res.PreciosPorColumna.Single(p => p.ColumnaNumero == 2);
            Assert.That(mayItem.ModoColumna, Is.EqualTo(ModoValorizacionColumna.PorVolumen.ToString()));
            Assert.That(mayItem.Monto, Is.EqualTo(9m));
            Assert.That(mayItem.Visible, Is.True);

            // VIP (oculta) sin precio ⇒ nulos
            var vipItem = res.PreciosPorColumna.Single(p => p.ColumnaNumero == 3);
            Assert.That(vipItem.Visible, Is.False);
            Assert.That(vipItem.Monto.HasValue, Is.False);
            Assert.That(vipItem.Moneda, Is.Null);
            Assert.That(vipItem.IncluyeImpuesto.HasValue, Is.False);
        }

        [Test]
        public async Task ListarPrecios_Exito_SoloVisibles_ExcluyeOcultas()
        {
            var listaRepo = new InMemoryListaPrecioRepository { ListaActiva = CrearListaConBaseVolumenYExtraOculta() };
            var precioRepo = new InMemoryPrecioProductoRepository();

            const string sku = "SKU-002";
            var agg = CrearPrecioProducto(sku);
            agg.UpsertPrecioFijo(
                IdentificadorColumnaPrecio.DesdeNumero(1),
                ValorPrecio.DesdeMonto(11m, Moneda.PEN(), true),
                PeriodoVigencia.Crear(DateTimeOffset.UtcNow.AddDays(-5), null),
                "seed",
                DateTimeOffset.UtcNow.AddDays(-5)
            );
            precioRepo.Seed(agg);

            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var catalogo2 = new Moq.Mock<ListaPreciosBC.Application.Interfaces.ICatalogoReadModel>();
            catalogo2.Setup(c => c.TryGetProductoIdBySkuAsync(It.IsAny<EmpresaId>(), It.Is<string>(s => s == sku), It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg.ProductoId);
            var sut = new ListarPreciosDeSkuUseCase(listaRepo, precioRepo, tenant.Object, catalogo2.Object);

            var res = await sut.Handle(
                new ListarPreciosDeSkuUseCase.Request(
                    Sku: sku,
                    Cantidad: 1,
                    Fecha: DateTimeOffset.UtcNow,
                    SoloVisibles: true // ← debe excluir la #3 (VIP)
                ),
                CancellationToken.None
            );

            Assert.That(res.PreciosPorColumna.Any(p => p.ColumnaNumero == 3), Is.False);
            Assert.That(res.PreciosPorColumna.Length, Is.EqualTo(2)); // Base + Mayorista
        }

        [Test]
        public void ListarPrecios_Falla_SiNoHayListaActiva()
        {
            var listaRepo = new InMemoryListaPrecioRepository { ListaActiva = null };
            var precioRepo = new InMemoryPrecioProductoRepository();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var sut = new ListarPreciosDeSkuUseCase(listaRepo, precioRepo, tenant.Object);

            Assert.That(
                async () => await sut.Handle(new ListarPreciosDeSkuUseCase.Request("SKU-X", 1), CancellationToken.None),
                Throws.TypeOf<NotFoundException>());
        }

        [Test]
        public void ListarPrecios_Falla_SiNoExisteSku()
        {
            var listaRepo = new InMemoryListaPrecioRepository { ListaActiva = CrearListaConBaseVolumenYExtraOculta() };
            var precioRepo = new InMemoryPrecioProductoRepository();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var sut = new ListarPreciosDeSkuUseCase(listaRepo, precioRepo, tenant.Object);

            Assert.That(
                async () => await sut.Handle(new ListarPreciosDeSkuUseCase.Request("SKU-404", 1), CancellationToken.None),
                Throws.TypeOf<NotFoundException>());
        }

        [Test]
        public async Task ListarPrecios_ConFechaFueraDeVigencia_DaNullEnEsaColumna()
        {
            var listaRepo = new InMemoryListaPrecioRepository { ListaActiva = CrearListaConBaseVolumenYExtraOculta() };
            var precioRepo = new InMemoryPrecioProductoRepository();

            const string sku = "SKU-003";
            var agg = CrearPrecioProducto(sku);
            // Precio base vencido ayer
            agg.UpsertPrecioFijo(
                IdentificadorColumnaPrecio.DesdeNumero(1),
                ValorPrecio.DesdeMonto(15m, Moneda.PEN(), true),
                PeriodoVigencia.Crear(DateTimeOffset.UtcNow.AddDays(-10), DateTimeOffset.UtcNow.AddDays(-1)),
                "seed",
                DateTimeOffset.UtcNow.AddDays(-10)
            );
            precioRepo.Seed(agg);

            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var catalogo3 = new Moq.Mock<ListaPreciosBC.Application.Interfaces.ICatalogoReadModel>();
            catalogo3.Setup(c => c.TryGetProductoIdBySkuAsync(It.IsAny<EmpresaId>(), It.Is<string>(s => s == sku), It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg.ProductoId);
            var sut = new ListarPreciosDeSkuUseCase(listaRepo, precioRepo, tenant.Object, catalogo3.Object);

            var res = await sut.Handle(
                new ListarPreciosDeSkuUseCase.Request(
                    Sku: sku,
                    Cantidad: 1,
                    Fecha: DateTimeOffset.UtcNow // fuera de vigencia
                ),
                CancellationToken.None
            );

            var baseItem = res.PreciosPorColumna.Single(p => p.ColumnaNumero == 1);
            Assert.That(baseItem.Monto, Is.Null);
            Assert.That(baseItem.Moneda, Is.Null);
            Assert.That(baseItem.IncluyeImpuesto, Is.Null);
        }
    }
}
