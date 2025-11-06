using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.UseCases;   // ConsultarPrecioVigenteUseCase
using ListaPreciosBC.Domain.Repositories;    // IListaPrecioRepository, IPrecioProductoRepository
using ListaPreciosBC.Domain.Aggregates;      // ListaPrecio, PrecioProducto
using ListaPreciosBC.Domain.ValueObjects;    // IdentificadorColumnaPrecio, NombreColumnaPrecio, ModoValorizacionColumna, TramoVolumen, MatrizVolumen, ValorPrecio, PeriodoVigencia, Sku
using NUnit.Framework;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;             // Moneda
using SharedKernel.Application.Interfaces;   // ITenantContext
using Moq;

namespace ListaPreciosBC.Tests.Application.UseCases
{
    public class ConsultarPrecioVigenteUseCaseTests
    {
        // ---------------------- Fakes InMemory ----------------------

        private sealed class InMemoryListaPrecioRepository : IListaPrecioRepository
        {
            public ListaPrecio? ListaActiva { get; set; }
            public Task<ListaPrecio?> ObtenerActivaAsync(EmpresaId empresaId, Guid? sucursalId = null, CancellationToken ct = default)
                => Task.FromResult(ListaActiva);

            public Task<ListaPrecio?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default)
                => Task.FromResult<ListaPrecio?>(ListaActiva is not null && ListaActiva.Id == id ? ListaActiva : null);

            public Task GuardarAsync(ListaPrecio aggregate, EmpresaId empresaId, Guid? sucursalId, int expectedVersion, CancellationToken ct = default)
                => Task.CompletedTask;

            public void Seed(ListaPrecio lista) => ListaActiva = lista;
        }

        private sealed class InMemoryPrecioProductoRepository : IPrecioProductoRepository
        {
            private readonly Dictionary<string, PrecioProducto> _store = new();
            private string? _lastLookupSku;

            public Task<PrecioProducto?> ObtenerPorSkuAsync(EmpresaId empresaId, Guid? sucursalId, Sku sku, CancellationToken ct = default)
            {
                _lastLookupSku = sku.Valor;
                _store.TryGetValue(sku.Valor, out var agg);
                return Task.FromResult<PrecioProducto?>(agg);
            }


            public Task GuardarAsync(PrecioProducto aggregate, EmpresaId empresaId, Guid? sucursalId, int expectedVersion, CancellationToken ct = default)
            {
                var key = _lastLookupSku ?? throw new InvalidOperationException("Debe consultarse por SKU antes de guardar.");
                _store[key] = aggregate;
                return Task.CompletedTask;
            }

            public void Seed(string sku, PrecioProducto agg) => _store[sku] = agg;

                public Task EliminarAsync(EmpresaId empresaId, Guid? sucursalId, Sku sku, int? expectedVersion = null, CancellationToken ct = default)
                {
                    _store.Remove(sku.Valor);
                    return Task.CompletedTask;
                }
        }

        

        // ---------------------- Builders con invariantes ----------------------

    private static ListaPrecio CrearListaBase()
        {
            var baseCfg = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.DesdeNumero(1),
                NombreColumnaPrecio.Crear("Base"),
                ModoValorizacionColumna.Fijo,
                esBase: true,
                visible: true,
                orden: 1
            );
            var plantilla = PlantillaColumnasPrecio.Crear(new[] { baseCfg });
            var lista = ListaPrecio.CrearNueva(EmpresaId.From("EMP-01"), Guid.NewGuid(), plantilla);
            return lista;
        }

        private static ListaPrecio CrearListaConBaseYVolumen()
        {
            var lista = CrearListaBase();

            var volCfg = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.DesdeNumero(2),
                NombreColumnaPrecio.Crear("Mayorista"),
                ModoValorizacionColumna.PorVolumen,
                esBase: false,
                visible: true,
                orden: 2
            );
            lista.AgregarColumna(volCfg);

            return lista;
        }

        private static PrecioProducto CrearPrecioProducto(string sku)
            => PrecioProducto.CrearNuevo(EmpresaId.From("EMP-01"), ProductoId.New());

        // ---------------------- Tests ----------------------

        [Test]
        public async Task Consulta_Exito_ColumnaFija_RetornaMontoYMoneda()
        {
            var listaRepo = new InMemoryListaPrecioRepository { ListaActiva = CrearListaBase() };
            var precioRepo = new InMemoryPrecioProductoRepository();

            var sku = "SKU-001";
            var agg = CrearPrecioProducto(sku);
            agg.UpsertPrecioFijo(
                IdentificadorColumnaPrecio.DesdeNumero(1),
                ValorPrecio.DesdeMonto(10.50m, Moneda.PEN(), true),
                PeriodoVigencia.Crear(DateTimeOffset.UtcNow.AddDays(-30), null),
                "seed",
                DateTimeOffset.UtcNow.AddDays(-30)
            );
            precioRepo.Seed(sku, agg);

            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var catalogo = new Moq.Mock<ListaPreciosBC.Application.Interfaces.ICatalogoReadModel>();
            catalogo.Setup(c => c.TryGetProductoIdBySkuAsync(It.IsAny<EmpresaId>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProductoId.New());
            var sut = new ConsultarPrecioVigenteUseCase(listaRepo, precioRepo, tenant.Object, catalogo.Object);

            var req = new ConsultarPrecioVigenteUseCase.Request(
                Sku: sku,
                ColumnaNumero: 1,
                Cantidad: 1,
                Fecha: DateTimeOffset.UtcNow
            );

            var res = await sut.Handle(req, CancellationToken.None);

            Assert.That(res.Sku, Is.EqualTo(sku));
            Assert.That(res.ColumnaNumero, Is.EqualTo((byte)1));
            Assert.That(res.Cantidad, Is.EqualTo(1));
            Assert.That(res.ModoColumna, Is.EqualTo(ModoValorizacionColumna.Fijo.ToString()));
            Assert.That(res.Monto, Is.EqualTo(10.50m));
            Assert.That(res.IncluyeImpuesto, Is.True);
            Assert.That(res.Moneda, Is.EqualTo(Moneda.PEN().Codigo)); // ADAPTA si tu Moneda expone otra propiedad
            Assert.That(res.VersionAgregado, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public async Task Consulta_Exito_ColumnaVolumen_ResuelvePorTramo()
        {
            var listaRepo = new InMemoryListaPrecioRepository { ListaActiva = CrearListaConBaseYVolumen() };
            var precioRepo = new InMemoryPrecioProductoRepository();

            var sku = "SKU-002";
            var agg = CrearPrecioProducto(sku);

            var tramos = new List<TramoVolumen>
            {
                TramoVolumen.Crear(1, 9,  ValorPrecio.DesdeMonto(12m, Moneda.PEN(), true)),
                TramoVolumen.Crear(10, null, ValorPrecio.DesdeMonto(10m, Moneda.PEN(), true))
            };
            var matriz = MatrizVolumen.Crear(tramos);
            agg.UpsertMatrizVolumen(
                IdentificadorColumnaPrecio.DesdeNumero(2),
                matriz,
                "seed",
                DateTimeOffset.UtcNow.AddDays(-30),
                cantidadReferenciaParaEventoBase: 1
            );
            precioRepo.Seed(sku, agg);

            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var catalogo = new Moq.Mock<ListaPreciosBC.Application.Interfaces.ICatalogoReadModel>();
            catalogo.Setup(c => c.TryGetProductoIdBySkuAsync(It.IsAny<EmpresaId>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProductoId.New());
            var sut = new ConsultarPrecioVigenteUseCase(listaRepo, precioRepo, tenant.Object, catalogo.Object);

            // Cantidad 1 → tramo 1..9 → 12
            var res1 = await sut.Handle(new ConsultarPrecioVigenteUseCase.Request(sku, 2, 1, DateTimeOffset.UtcNow), CancellationToken.None);
            Assert.That(res1.ModoColumna, Is.EqualTo(ModoValorizacionColumna.PorVolumen.ToString()));
            Assert.That(res1.Monto, Is.EqualTo(12m));

            // Cantidad 10 → tramo 10..∞ → 10
            var res2 = await sut.Handle(new ConsultarPrecioVigenteUseCase.Request(sku, 2, 10, DateTimeOffset.UtcNow), CancellationToken.None);
            Assert.That(res2.Monto, Is.EqualTo(10m));
            Assert.That(res2.Moneda, Is.EqualTo(Moneda.PEN().Codigo));
        }

        [Test]
        public void Consulta_Falla_SiNoHayListaActiva()
        {
            var listaRepo = new InMemoryListaPrecioRepository { ListaActiva = null };
            var precioRepo = new InMemoryPrecioProductoRepository();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var catalogo = new Moq.Mock<ListaPreciosBC.Application.Interfaces.ICatalogoReadModel>();
            catalogo.Setup(c => c.TryGetProductoIdBySkuAsync(It.IsAny<EmpresaId>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProductoId.New());
            var sut = new ConsultarPrecioVigenteUseCase(listaRepo, precioRepo, tenant.Object, catalogo.Object);

            Assert.That(
                async () => await sut.Handle(new ConsultarPrecioVigenteUseCase.Request("SKU-X", 1, 1, DateTimeOffset.UtcNow), CancellationToken.None),
                Throws.TypeOf<NotFoundException>());
        }

        [Test]
        public void Consulta_Falla_SiColumnaNoExiste()
        {
            var listaRepo = new InMemoryListaPrecioRepository { ListaActiva = CrearListaBase() };
            var precioRepo = new InMemoryPrecioProductoRepository();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var catalogo = new Moq.Mock<ListaPreciosBC.Application.Interfaces.ICatalogoReadModel>();
            catalogo.Setup(c => c.TryGetProductoIdBySkuAsync(It.IsAny<EmpresaId>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProductoId.New());
            var sut = new ConsultarPrecioVigenteUseCase(listaRepo, precioRepo, tenant.Object, catalogo.Object);

            Assert.That(
                async () => await sut.Handle(new ConsultarPrecioVigenteUseCase.Request("SKU-X", 9, 1, DateTimeOffset.UtcNow), CancellationToken.None),
                Throws.TypeOf<NotFoundException>());
        }

        [Test]
        public void Consulta_Falla_SiSkuNoExiste()
        {
            var listaRepo = new InMemoryListaPrecioRepository { ListaActiva = CrearListaBase() };
            var precioRepo = new InMemoryPrecioProductoRepository();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var catalogo = new Moq.Mock<ListaPreciosBC.Application.Interfaces.ICatalogoReadModel>();
            catalogo.Setup(c => c.TryGetProductoIdBySkuAsync(It.IsAny<EmpresaId>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProductoId.New());
            var sut = new ConsultarPrecioVigenteUseCase(listaRepo, precioRepo, tenant.Object, catalogo.Object);

            Assert.That(
                async () => await sut.Handle(new ConsultarPrecioVigenteUseCase.Request("SKU-404", 1, 1, DateTimeOffset.UtcNow), CancellationToken.None),
                Throws.TypeOf<NotFoundException>());
        }

        [Test]
        public void Consulta_Falla_SiNoHayPrecioVigente()
        {
            var listaRepo = new InMemoryListaPrecioRepository { ListaActiva = CrearListaBase() };
            var precioRepo = new InMemoryPrecioProductoRepository();

            var sku = "SKU-003";
            var agg = CrearPrecioProducto(sku);
            // Seed con vigencia expirada
            agg.UpsertPrecioFijo(
                IdentificadorColumnaPrecio.DesdeNumero(1),
                ValorPrecio.DesdeMonto(15m, Moneda.PEN(), true),
                PeriodoVigencia.Crear(DateTimeOffset.UtcNow.AddDays(-30), DateTimeOffset.UtcNow.AddDays(-1)),
                "seed",
                DateTimeOffset.UtcNow.AddDays(-30)
            );
            precioRepo.Seed(sku, agg);

            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var catalogo = new Moq.Mock<ListaPreciosBC.Application.Interfaces.ICatalogoReadModel>();
            catalogo.Setup(c => c.TryGetProductoIdBySkuAsync(It.IsAny<EmpresaId>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProductoId.New());
            var sut = new ConsultarPrecioVigenteUseCase(listaRepo, precioRepo, tenant.Object, catalogo.Object);

            Assert.That(
                async () => await sut.Handle(new ConsultarPrecioVigenteUseCase.Request(sku, 1, 1, DateTimeOffset.UtcNow), CancellationToken.None),
                Throws.TypeOf<NotFoundException>());
        }
    }
}
