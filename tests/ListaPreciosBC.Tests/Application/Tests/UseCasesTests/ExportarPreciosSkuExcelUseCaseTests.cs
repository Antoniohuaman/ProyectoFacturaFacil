using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.UseCases;   // ExportarPreciosSkuExcelUseCase
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
    public class ExportarPreciosSkuExcelUseCaseTests
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

            public Task EliminarAsync(EmpresaId empresaId, Guid? sucursalId, Sku sku, int? expectedVersion = null, CancellationToken ct = default)
            {
                _store.Remove(sku.Valor);
                return Task.CompletedTask;
            }

            public void Seed(string sku, PrecioProducto agg) => _store[sku] = agg;
        }

        

        // ---------------------- Builders con invariantes ----------------------

        private static ListaPrecio CrearListaConBaseVolumenYOculta()
        {
            var baseCfg = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.DesdeNumero(1),
                NombreColumnaPrecio.Crear("Base"),
                ModoValorizacionColumna.Fijo,
                esBase: true,
                visible: true,
                orden: 1
            );
            var mayoristaCfg = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.DesdeNumero(2),
                NombreColumnaPrecio.Crear("Mayorista"),
                ModoValorizacionColumna.PorVolumen,
                esBase: false,
                visible: true,
                orden: 2
            );
            var vipOcultaCfg = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.DesdeNumero(3),
                NombreColumnaPrecio.Crear("VIP"),
                ModoValorizacionColumna.Fijo,
                esBase: false,
                visible: false,
                orden: 3
            );
            var plantilla = PlantillaColumnasPrecio.Crear(new[] { baseCfg, mayoristaCfg, vipOcultaCfg });
            return ListaPrecio.CrearNueva(EmpresaId.From("EMP-01"), Guid.NewGuid(), plantilla);
        }

        private static PrecioProducto CrearAggConPrecios(string sku)
        {
            var agg = PrecioProducto.CrearNuevo(EmpresaId.From("EMP-01"), ProductoId.New());

            // Base Fija vigente
            agg.UpsertPrecioFijo(
                IdentificadorColumnaPrecio.DesdeNumero(1),
                ValorPrecio.DesdeMonto(10.50m, Moneda.PEN(), true),
                PeriodoVigencia.Crear(DateTime.UtcNow.AddDays(-30), null),
                "seed",
                DateTimeOffset.UtcNow.AddDays(-30)
            );

            // Mayorista Volumen vigente (1..9 = 9; 10..∞ = 8.5)
            var tramos = new List<TramoVolumen>
            {
                TramoVolumen.Crear(1, 9,   ValorPrecio.DesdeMonto(9m,   Moneda.PEN(), true)),
                TramoVolumen.Crear(10, null, ValorPrecio.DesdeMonto(8.5m, Moneda.PEN(), true))
            };
            agg.UpsertMatrizVolumen(
                IdentificadorColumnaPrecio.DesdeNumero(2),
                MatrizVolumen.Crear(tramos),
                "seed",
                DateTimeOffset.UtcNow.AddDays(-30),
                cantidadReferenciaParaEventoBase: 1
            );

            // VIP oculta sin precio
            return agg;
        }

        // ---------------------- Helpers ----------------------

        private static string CsvToString(byte[] data) => Encoding.UTF8.GetString(data);

        // ---------------------- Tests ----------------------

        [Test]
        public async Task Exportar_Exito_IncluyeTodasLasColumnas_ConYSinPrecio()
        {
            var listaRepo  = new InMemoryListaPrecioRepository { ListaActiva = CrearListaConBaseVolumenYOculta() };
            var precioRepo = new InMemoryPrecioProductoRepository();

            const string sku = "SKU-001";
            var agg = CrearAggConPrecios(sku);
            precioRepo.Seed(sku, agg);

            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var catalogo = new Moq.Mock<ListaPreciosBC.Application.Interfaces.ICatalogoReadModel>();
            catalogo.Setup(c => c.TryGetProductoIdBySkuAsync(It.IsAny<EmpresaId>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProductoId.New());
            var sut = new ExportarPreciosSkuExcelUseCase(listaRepo, precioRepo, tenant.Object, catalogo.Object);

            var res = await sut.Handle(new ExportarPreciosSkuExcelUseCase.Request(
                Sku: sku,
                Cantidad: 1,
                Fecha: DateTimeOffset.UtcNow,
                SoloVisibles: false
            ), CancellationToken.None);

            Assert.That(res.NombreArchivo.EndsWith(".csv"), Is.True);
            Assert.That(res.ContentType.StartsWith("text/csv"), Is.True);
            Assert.That(res.VersionAgregado, Is.GreaterThanOrEqualTo(0));
            Assert.That(res.ColumnasIncluidas, Is.EqualTo(3));

            var csv = CsvToString(res.Contenido);
            var lines = csv.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            // header + 3 rows (puede tener línea final vacía)
            Assert.That(lines[0], Does.Contain("SKU;Fecha;Cantidad;ColumnaNumero;Nombre;Modo;EsBase;Visible;Monto;IncluyeImpuesto;Moneda"));

            // Base: monto 10.50
            Assert.That(csv, Does.Contain("Base;Fijo;True;True;10.50;True;PEN"));

            // Mayorista (volumen) con cantidad 1 => 9
            Assert.That(csv, Does.Contain("Mayorista;PorVolumen;False;True;9;True;PEN"));

            // VIP oculta sin precio (campos vacíos al final de la fila)
            Assert.That(csv, Does.Contain("VIP;Fijo;False;False;;;"));
        }

        [Test]
        public async Task Exportar_Exito_SoloVisibles_ExcluyeOcultas()
        {
            var listaRepo  = new InMemoryListaPrecioRepository { ListaActiva = CrearListaConBaseVolumenYOculta() };
            var precioRepo = new InMemoryPrecioProductoRepository();

            const string sku = "SKU-002";
            var agg2 = CrearAggConPrecios(sku);
            precioRepo.Seed(sku, agg2);

            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var catalogo2 = new Moq.Mock<ListaPreciosBC.Application.Interfaces.ICatalogoReadModel>();
            catalogo2.Setup(c => c.TryGetProductoIdBySkuAsync(It.IsAny<EmpresaId>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProductoId.New());
            var sut = new ExportarPreciosSkuExcelUseCase(listaRepo, precioRepo, tenant.Object, catalogo2.Object);

            var res = await sut.Handle(new ExportarPreciosSkuExcelUseCase.Request(
                Sku: sku,
                Cantidad: 10, // asegura tramo 10..∞
                Fecha: DateTimeOffset.UtcNow,
                SoloVisibles: true
            ), CancellationToken.None);

            Assert.That(res.ColumnasIncluidas, Is.EqualTo(2)); // Base + Mayorista
            var csv = CsvToString(res.Contenido);

            Assert.That(csv, Does.Contain("Base;Fijo;True;True;10.50;True;PEN"));
            Assert.That(csv, Does.Contain("Mayorista;PorVolumen;False;True;8.5;True;PEN"));
            Assert.That(csv, Does.Not.Contain("VIP")); // excluida
        }

        [Test]
        public void Exportar_Falla_SiNoHayListaActiva()
        {
            var listaRepo  = new InMemoryListaPrecioRepository { ListaActiva = null };
            var precioRepo = new InMemoryPrecioProductoRepository();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var catalogo3 = new Moq.Mock<ListaPreciosBC.Application.Interfaces.ICatalogoReadModel>();
            catalogo3.Setup(c => c.TryGetProductoIdBySkuAsync(It.IsAny<EmpresaId>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProductoId.New());
            var sut        = new ExportarPreciosSkuExcelUseCase(listaRepo, precioRepo, tenant.Object, catalogo3.Object);

            Assert.That(async () => await sut.Handle(new ExportarPreciosSkuExcelUseCase.Request("SKU-X", 1), CancellationToken.None),
                        Throws.TypeOf<NotFoundException>());
        }

        [Test]
        public void Exportar_Falla_SiSkuNoExiste()
        {
            var listaRepo  = new InMemoryListaPrecioRepository { ListaActiva = CrearListaConBaseVolumenYOculta() };
            var precioRepo = new InMemoryPrecioProductoRepository();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var catalogo4 = new Moq.Mock<ListaPreciosBC.Application.Interfaces.ICatalogoReadModel>();
            catalogo4.Setup(c => c.TryGetProductoIdBySkuAsync(It.IsAny<EmpresaId>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProductoId.New());
            var sut        = new ExportarPreciosSkuExcelUseCase(listaRepo, precioRepo, tenant.Object, catalogo4.Object);

            Assert.That(async () => await sut.Handle(new ExportarPreciosSkuExcelUseCase.Request("SKU-404", 1), CancellationToken.None),
                        Throws.TypeOf<NotFoundException>());
        }
    }
}
