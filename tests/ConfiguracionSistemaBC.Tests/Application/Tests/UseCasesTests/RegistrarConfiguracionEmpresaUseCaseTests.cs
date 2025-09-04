using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConfiguracionSistemaBC.Application.UseCases;
using ConfiguracionSistemaBC.Domain.Aggregates;
using ConfiguracionSistemaBC.Domain.Repositories; // <- tus interfaces
using ConfiguracionSistemaBC.Domain.ValueObjects; // ValueObjects propios del BC
using SharedKernel.ValueObjects; // ValueObjects compartidos
using NUnit.Framework;

namespace ConfiguracionSistemaBC.Tests.Application.UseCases
{
    // Stubs de infraestructura para el test (in-memory)
    internal sealed class InMemoryConfiguracionEmpresaRepository : IConfiguracionEmpresaRepository
    {
        public List<ConfiguracionEmpresa> Saved { get; } = new();

        public Task AddAsync(ConfiguracionEmpresa agregado, CancellationToken ct)
        {
            Saved.Add(agregado);
            return Task.CompletedTask;
        }

        // Métodos stub requeridos por la interfaz
        public Task<ConfiguracionEmpresa?> FindByIdAsync(EmpresaId id, CancellationToken ct) => Task.FromResult<ConfiguracionEmpresa?>(null);
        public Task<ConfiguracionEmpresa?> FindByRucAsync(Ruc ruc, CancellationToken ct) => Task.FromResult<ConfiguracionEmpresa?>(null);
        public Task UpdateAsync(ConfiguracionEmpresa agregado, CancellationToken ct) => Task.CompletedTask;
        public Task RemoveAsync(EmpresaId id, CancellationToken ct) => Task.CompletedTask;
        
        // Métodos faltantes según la interfaz
        public Task<ConfiguracionEmpresa?> GetByEmpresaIdAsync(EmpresaId empresaId, CancellationToken ct) => Task.FromResult<ConfiguracionEmpresa?>(null);
        public Task DeleteAsync(EmpresaId empresaId, CancellationToken ct) => Task.CompletedTask;
        public Task<ConfiguracionEmpresa?> FindByRucAsync(string ruc, CancellationToken ct) => Task.FromResult<ConfiguracionEmpresa?>(null);
    public Task<bool> UpdateIfVersionMatchAsync(ConfiguracionEmpresa agregado, int expectedVersion, CancellationToken ct = default) => Task.FromResult(false);
    }

    internal sealed class NoopUnitOfWork : IUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    [TestFixture]
    public class RegistrarConfiguracionEmpresaUseCaseTests
    {
    private static Ruc MakeRuc() => Ruc.From("20100070970"); // SharedKernel.ValueObjects

        private static DomicilioFiscal MakeDomicilio()
        {
            // Si DomicilioFiscal está en el dominio del BC, usa su método/factory
            return DomicilioFiscal.FromPeru(
                linea: "AV. PRUEBA 123",
                ubigeo: "150101",
                departamento: "LIMA",
                provincia: "LIMA",
                distrito: "LIMA"
            );
        }

        [Test]
        public async Task RegistrarConfiguracion_Completa_RespetaDefaultsYPreferencias()
        {
            // Arrange
            var repo = new InMemoryConfiguracionEmpresaRepository();
            var uow  = new NoopUnitOfWork();
            var uc = new RegistrarConfiguracionEmpresaUseCase(repo, uow);

            var input = new RegistrarConfiguracionEmpresaInputDto
            {
                Ruc = MakeRuc(),
                RazonSocial = "ACME S.A.C.",
                DireccionFiscal = MakeDomicilio(),
                MonedaBase = Moneda.PEN(),

                NombreComercial = "ACME",
                Telefono = Telefono.FromTexto("+51 999 999 999"), // SharedKernel.ValueObjects
                Emails = new List<Email> { Email.Create("facturacion@acme.test") }, // SharedKernel.ValueObjects
                PieDePagina = PieDePagina.FromTextoPlano("Gracias por su preferencia"), // SharedKernel.ValueObjects
                MostrarImagenEnComprobanteImpresa = true,

                FormasDePagoVisibles = new[] { "Contado", "Efectivo", "Tarjeta" },
                UnidadesDeMedidaVisibles = new[] { "NIU", "ZZ" },

                FormaDePagoPorDefectoNombre = "Contado",
                UnidadDeMedidaPorDefectoCodigo = "NIU"
            };

            // Act
            var output = await uc.Handle(input);

            // Assert (persistencia)
            Assert.That(repo.Saved.Count, Is.EqualTo(1));

            var agg = repo.Saved.Single();

            // Perfil base
            Assert.That(agg.Ruc, Is.EqualTo(input.Ruc));
            Assert.That(agg.RazonSocial, Is.EqualTo("ACME S.A.C."));
            Assert.That(agg.MonedaBase, Is.EqualTo(Moneda.PEN()));

            // Establecimiento principal
            var estPrincipal = agg.ObtenerEstablecimientoPrincipal();
            Assert.That(estPrincipal, Is.Not.Null);
            Assert.That(estPrincipal!.Codigo, Is.EqualTo("01"));
            Assert.That(estPrincipal.Habilitado, Is.True);

            // Formas de pago default + visibilidad
            var fpDefault = agg.ObtenerFormaDePagoPorDefecto();
            Assert.That(fpDefault, Is.Not.Null);
            Assert.That(fpDefault!.Nombre, Is.EqualTo("Contado"));
            Assert.That(fpDefault.Valor.EsContado, Is.True);

            var visiblesFP = agg.ListarFormasDePago().Where(f => f.Visible).Select(f => f.Nombre).ToHashSet();
            Assert.That(visiblesFP, Does.Contain("Contado"));
            Assert.That(visiblesFP, Does.Contain("Efectivo"));
            Assert.That(visiblesFP, Does.Contain("Tarjeta"));
            Assert.That(agg.ListarFormasDePago().First(f => f.Nombre == "Transferencia").Visible, Is.False);

            // Unidades de medida default + visibilidad
            var umDefault = agg.ObtenerUnidadDeMedidaPorDefecto();
            Assert.That(umDefault, Is.Not.Null);
            Assert.That(umDefault!.Unidad.Codigo, Is.EqualTo("NIU"));

            var visiblesUM = agg.ListarUnidadesDeMedida().Where(u => u.Visible).Select(u => u.Unidad.Codigo).ToHashSet();
            Assert.That(visiblesUM, Does.Contain("NIU"));
            Assert.That(visiblesUM, Does.Contain("ZZ"));
            Assert.That(agg.ListarUnidadesDeMedida().First(u => u.Unidad.Codigo == "KGM").Visible, Is.False);

            // Output snapshot coherente
            Assert.That(output.EmpresaId, Is.EqualTo(agg.EmpresaId));
            Assert.That(output.Ruc, Is.EqualTo(agg.Ruc));
            Assert.That(output.MonedaBase, Is.EqualTo(Moneda.PEN()));
            Assert.That(output.EstablecimientoPrincipal!.Codigo, Is.EqualTo("01"));
            Assert.That(output.FormaDePagoPorDefecto!.Nombre, Is.EqualTo("Contado"));
            Assert.That(output.UnidadDeMedidaPorDefecto!.Unidad.Codigo, Is.EqualTo("NIU"));
        }
    }
}
