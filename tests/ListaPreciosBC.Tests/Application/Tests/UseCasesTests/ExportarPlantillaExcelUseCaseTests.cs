using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.UseCases;   // ExportarPlantillaExcelUseCase
using ListaPreciosBC.Domain.Repositories;    // IListaPrecioRepository
using ListaPreciosBC.Domain.Aggregates;      // ListaPrecio
using ListaPreciosBC.Domain.ValueObjects;    // IdentificadorColumnaPrecio, NombreColumnaPrecio, ModoValorizacionColumna
using NUnit.Framework;
using Moq;
using SharedKernel.Exceptions;
using SharedKernel.Application.Interfaces;   // ITenantContext
using SharedKernel.ValueObjects;             // EmpresaId, TenantId

namespace ListaPreciosBC.Tests.Application.UseCases
{
    public class ExportarPlantillaExcelUseCaseTests
    {
        // ---------------------- Fake InMemory ----------------------

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

        

        // ---------------------- Builders con invariantes ----------------------

        private static ListaPrecio CrearListaConBaseVolumenYOcultaYNombreEspecial()
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
            var especialCfg = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.DesdeNumero(3),
                NombreColumnaPrecio.Crear("VIP; \"Promo\""),
                ModoValorizacionColumna.Fijo,
                esBase: false,
                visible: false, // oculta
                orden: 3
            );
            var plantilla = PlantillaColumnasPrecio.Crear(new[] { baseCfg, mayoristaCfg, especialCfg });
            return ListaPrecio.CrearNueva(Guid.NewGuid(), plantilla);
        }

        // ---------------------- Helpers ----------------------

        private static string CsvToString(byte[] data) => Encoding.UTF8.GetString(data);

        // ---------------------- Tests ----------------------

        [Test]
        public async Task Exportar_Exito_IncluyeTodasLasColumnas_OrdenadasYConHeader()
        {
            var repo = new InMemoryListaPrecioRepository { ListaActiva = CrearListaConBaseVolumenYOcultaYNombreEspecial() };
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var sut  = new ExportarPlantillaExcelUseCase(repo, tenant.Object);

            var res = await sut.Handle(new ExportarPlantillaExcelUseCase.Request(SoloVisibles: false), CancellationToken.None);

            Assert.That(res.NombreArchivo.EndsWith(".csv"), Is.True);
            Assert.That(res.ContentType.StartsWith("text/csv"), Is.True);
            Assert.That(res.VersionLista, Is.GreaterThanOrEqualTo(0));
            Assert.That(res.ColumnasIncluidas, Is.EqualTo(3));

            var csv = CsvToString(res.Contenido);
            var lines = csv.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            // header + 3 rows (puede existir línea final vacía según plataforma)
            Assert.That(lines[0], Does.Contain("ColumnaNumero;Nombre;Modo;EsBase;Visible;Orden"));

            // Base
            Assert.That(csv, Does.Contain("1;Base;Fijo;True;True;1"));

            // Mayorista (PorVolumen)
            Assert.That(csv, Does.Contain("2;Mayorista;PorVolumen;False;True;2"));

            // VIP con caracteres especiales -> debe ir entre comillas y con comillas duplicadas
            Assert.That(csv, Does.Contain("3;\"VIP; \"\"Promo\"\"\";Fijo;False;False;3"));
        }

        [Test]
        public async Task Exportar_Exito_SoloVisibles_ExcluyeOcultas()
        {
            var repo = new InMemoryListaPrecioRepository { ListaActiva = CrearListaConBaseVolumenYOcultaYNombreEspecial() };
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var sut  = new ExportarPlantillaExcelUseCase(repo, tenant.Object);

            var res = await sut.Handle(new ExportarPlantillaExcelUseCase.Request(SoloVisibles: true), CancellationToken.None);

            Assert.That(res.ColumnasIncluidas, Is.EqualTo(2)); // Base + Mayorista
            var csv = CsvToString(res.Contenido);

            Assert.That(csv, Does.Contain("1;Base;Fijo;True;True;1"));
            Assert.That(csv, Does.Contain("2;Mayorista;PorVolumen;False;True;2"));
            Assert.That(csv, Does.Not.Contain("VIP")); // oculto excluido
        }

        [Test]
        public void Exportar_Falla_SiNoHayListaActiva()
        {
            var repo = new InMemoryListaPrecioRepository { ListaActiva = null };
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var sut  = new ExportarPlantillaExcelUseCase(repo, tenant.Object);

            Assert.That(
                async () => await sut.Handle(new ExportarPlantillaExcelUseCase.Request(), CancellationToken.None),
                Throws.TypeOf<NotFoundException>());
        }
        }
}
