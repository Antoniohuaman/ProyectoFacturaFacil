using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

using ConfiguracionSistemaBC.Application.UseCases.Series;   // UseCase + DTOs
using ConfiguracionSistemaBC.Domain.Aggregates;             // SerieComprobante
using ConfiguracionSistemaBC.Domain.Repositories;           // ISerieComprobanteRepository, IConfiguracionEmpresaRepository, IUnitOfWork
using ConfiguracionSistemaBC.Domain.ValueObjects;           // TipoComprobanteCodigo, SerieCodigo, TipoOperacion, Correlativo
using SharedKernel.Application.Interfaces;                   // ITenantContext
using SharedKernel.ValueObjects;                             // EmpresaId, EstablecimientoId
using Moq;

namespace ConfiguracionSistemaBC.Tests.Application.Series
{
    [TestFixture]
    public class RegistrarSerieComprobanteUseCaseTests
    {
        private const string EmpresaCanonica = "20600893409";

        // -------------------- Fakes --------------------

        

        private sealed class FakeUow : IUnitOfWork
        {
            public int SaveCount { get; private set; }
            public Task SaveChangesAsync(CancellationToken ct = default)
            {
                SaveCount++;
                return Task.CompletedTask;
            }
        }

        private sealed class FakeConfiguracionEmpresaRepository : IConfiguracionEmpresaRepository
        {
            public bool EstablecimientoExisteFlag { get; set; } = true;

            public Task<bool> EstablecimientoExisteAsync(EmpresaId empresaId, EstablecimientoId establecimientoId, CancellationToken ct = default)
                => Task.FromResult(EstablecimientoExisteFlag);

            // Los siguientes miembros no son usados por el caso de uso, se stubbean.

            public Task<ConfiguracionEmpresa?> GetByEmpresaIdAsync(EmpresaId empresaId, CancellationToken ct = default)
                => Task.FromResult<ConfiguracionEmpresa?>(null);

            public Task AddAsync(ConfiguracionEmpresa aggregate, CancellationToken ct = default)
                => Task.CompletedTask;

            public Task UpdateAsync(ConfiguracionEmpresa aggregate, CancellationToken ct = default)
                => Task.CompletedTask;

            public Task<bool> UpdateIfVersionMatchAsync(ConfiguracionEmpresa aggregate, int expectedVersion, CancellationToken ct = default)
                => Task.FromResult(true);

            public Task DeleteAsync(EmpresaId empresaId, CancellationToken ct = default)
                => Task.CompletedTask;

            public Task<ConfiguracionEmpresa?> FindByRucAsync(ConfiguracionSistemaBC.Domain.ValueObjects.Ruc ruc, CancellationToken ct = default)
                => Task.FromResult<ConfiguracionEmpresa?>(null);

            public Task<bool> SerieExisteAsync(EmpresaId empresaId, ConfiguracionSistemaBC.Domain.ValueObjects.TipoComprobanteCodigo tipo, ConfiguracionSistemaBC.Domain.ValueObjects.SerieCodigo serie, CancellationToken ct = default)
                => Task.FromResult(false);

            public Task<bool> UnidadDeMedidaExisteAsync(EmpresaId empresaId, UnidadDeMedida unidad, CancellationToken ct = default)
                => Task.FromResult(false);
        }

        private sealed class FakeSerieComprobanteRepository : ISerieComprobanteRepository
        {
            public bool ExistsByTipoSerieFlag { get; set; } = false;
            public bool UnsetDefaultCalled { get; private set; } = false;
            public int AddCount { get; private set; } = 0;
            public SerieComprobante? LastAdded { get; private set; }

            public Task<bool> ExistsByTipoSerieAsync(EmpresaId empresaId, TipoComprobanteCodigo tipo, SerieCodigo serie, CancellationToken ct = default)
                => Task.FromResult(ExistsByTipoSerieFlag);

            public Task UnsetDefaultForTipoAsync(EmpresaId empresaId, TipoComprobanteCodigo tipo, CancellationToken ct = default)
            {
                UnsetDefaultCalled = true;
                return Task.CompletedTask;
            }

            public Task AddAsync(SerieComprobante aggregate, CancellationToken ct = default)
            {
                AddCount++;
                LastAdded = aggregate;
                return Task.CompletedTask;
            }

            // No usados por este caso, pero deben existir:
            public Task<SerieComprobante?> GetByIdAsync(Guid id, CancellationToken ct = default)
                => Task.FromResult<SerieComprobante?>(null);

            public Task<SerieComprobante?> GetByEmpresaTipoSerieAsync(EmpresaId empresaId, TipoComprobanteCodigo tipo, SerieCodigo serie, CancellationToken ct = default)
                => Task.FromResult<SerieComprobante?>(null);

            public Task<SerieComprobante?> GetDefaultByTipoAsync(EmpresaId empresaId, TipoComprobanteCodigo tipo, CancellationToken ct = default)
                => Task.FromResult<SerieComprobante?>(null);

            public Task<System.Collections.Generic.IReadOnlyList<SerieComprobante>> ListAsync(
                EmpresaId empresaId,
                TipoComprobanteCodigo? tipo = null,
                bool? habilitada = null,
                EstablecimientoId? establecimientoId = null,
                int skip = 0,
                int take = 100,
                CancellationToken ct = default)
                => Task.FromResult<System.Collections.Generic.IReadOnlyList<SerieComprobante>>(Array.Empty<SerieComprobante>());

            public Task<int> CountAsync(EmpresaId empresaId, TipoComprobanteCodigo? tipo = null, bool? habilitada = null, EstablecimientoId? establecimientoId = null, CancellationToken ct = default)
                => Task.FromResult(0);

            public Task UpdateAsync(SerieComprobante aggregate, int expectedVersion, CancellationToken ct = default)
                => Task.CompletedTask;

            public Task DeleteAsync(Guid id, int expectedVersion, CancellationToken ct = default)
                => Task.CompletedTask;
        }

        private static RegistrarSerieComprobanteUseCase BuildUseCase(
            FakeSerieComprobanteRepository seriesRepo,
            FakeConfiguracionEmpresaRepository empresaRepo,
            FakeUow uow)
        {
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From(EmpresaCanonica));
            return new RegistrarSerieComprobanteUseCase(seriesRepo, empresaRepo, uow, tenant.Object);
        }

        private static RegistrarSerieComprobanteInputDto MakeInput(
            string tipo = "01", string serie = "FE01", bool porDefecto = true, bool habilitada = true, int correlativoInicial = 1, string tipoOperacion = "0101")
        {
            return new RegistrarSerieComprobanteInputDto
            {
                TipoComprobante = tipo,
                Serie = serie,
                EstablecimientoId = Guid.NewGuid().ToString("D"),
                TipoOperacion = tipoOperacion,
                CorrelativoInicial = correlativoInicial,
                EsPorDefecto = porDefecto,
                Habilitada = habilitada
            };
        }

        // -------------------- Tests --------------------

        [Test]
        public async Task Crea_FE01_para_Factura_y_marca_default_desmarcando_previas()
        {
            var seriesRepo = new FakeSerieComprobanteRepository();
            var empresaRepo = new FakeConfiguracionEmpresaRepository { EstablecimientoExisteFlag = true };
            var uow = new FakeUow();
            var useCase = BuildUseCase(seriesRepo, empresaRepo, uow);

            var input = MakeInput(
                tipo: "01",             // Factura
                serie: "FE01",          // Prefijo F correcto
                porDefecto: true,
                habilitada: true,
                correlativoInicial: 1,
                tipoOperacion: "0101"   // Venta interna (Default)
            );

            var outDto = await useCase.HandleAsync(input, CancellationToken.None);

            Assert.That(outDto, Is.Not.Null);
            Assert.That(outDto.EmpresaId, Is.EqualTo(EmpresaCanonica));
            Assert.That(outDto.TipoComprobante, Is.EqualTo("01"));
            Assert.That(outDto.Serie, Is.EqualTo("FE01"));
            Assert.That(outDto.CorrelativoInicial, Is.EqualTo(1));
            Assert.That(outDto.EsPorDefecto, Is.True);
            Assert.That(outDto.Habilitada, Is.True);
            Assert.That(outDto.Version, Is.EqualTo(0)); // recién creada

            Assert.That(seriesRepo.AddCount, Is.EqualTo(1));
            Assert.That(uow.SaveCount, Is.EqualTo(1));
            Assert.That(seriesRepo.UnsetDefaultCalled, Is.True); // exclusividad del default
            Assert.That(seriesRepo.LastAdded, Is.Not.Null);
            Assert.That(seriesRepo.LastAdded!.TipoOperacion.Codigo, Is.EqualTo("0101"));
        }

        [Test]
        public void Lanza_si_establecimiento_no_existe_en_la_empresa()
        {
            var seriesRepo = new FakeSerieComprobanteRepository();
            var empresaRepo = new FakeConfiguracionEmpresaRepository { EstablecimientoExisteFlag = false }; // <- no existe
            var uow = new FakeUow();
            var useCase = BuildUseCase(seriesRepo, empresaRepo, uow);

            var input = MakeInput();

            Assert.That(
                async () => await useCase.HandleAsync(input, CancellationToken.None),
                Throws.InvalidOperationException.With.Message.Contains("El establecimiento no existe"));
        }

        [Test]
        public void Lanza_si_ya_existe_la_misma_serie_para_el_tipo()
        {
            var seriesRepo = new FakeSerieComprobanteRepository { ExistsByTipoSerieFlag = true };
            var empresaRepo = new FakeConfiguracionEmpresaRepository { EstablecimientoExisteFlag = true };
            var uow = new FakeUow();
            var useCase = BuildUseCase(seriesRepo, empresaRepo, uow);

            var input = MakeInput();

            Assert.That(
                async () => await useCase.HandleAsync(input, CancellationToken.None),
                Throws.InvalidOperationException.With.Message.Contains("Ya existe una serie"));
        }

        [Test]
        public void Lanza_si_se_marca_por_defecto_y_esta_inhabilitada()
        {
            var seriesRepo = new FakeSerieComprobanteRepository();
            var empresaRepo = new FakeConfiguracionEmpresaRepository { EstablecimientoExisteFlag = true };
            var uow = new FakeUow();
            var useCase = BuildUseCase(seriesRepo, empresaRepo, uow);

            var input = MakeInput(porDefecto: true, habilitada: false);

            Assert.That(
                async () => await useCase.HandleAsync(input, CancellationToken.None),
                Throws.InvalidOperationException.With.Message.Contains("por defecto una serie inhabilitada"));
        }

        [Test]
        public void Lanza_si_prefijo_de_serie_no_corresponde_al_tipo()
        {
            var seriesRepo = new FakeSerieComprobanteRepository();
            var empresaRepo = new FakeConfiguracionEmpresaRepository { EstablecimientoExisteFlag = true };
            var uow = new FakeUow();
            var useCase = BuildUseCase(seriesRepo, empresaRepo, uow);

            // Tipo Boleta (03) exige prefijo 'B', usamos 'FE01' para forzar error
            var input = MakeInput(tipo: "03", serie: "FE01");

            Assert.That(
                async () => await useCase.HandleAsync(input, CancellationToken.None),
                Throws.TypeOf<ArgumentException>().With.Message.Contains("no es válida para el tipo"));
        }

        [Test]
        public async Task No_desmarca_default_si_no_se_pide_por_defecto()
        {
            var seriesRepo = new FakeSerieComprobanteRepository();
            var empresaRepo = new FakeConfiguracionEmpresaRepository { EstablecimientoExisteFlag = true };
            var uow = new FakeUow();
            var useCase = BuildUseCase(seriesRepo, empresaRepo, uow);

            var input = MakeInput(porDefecto: false, habilitada: true); // <- no default

            var outDto = await useCase.HandleAsync(input, CancellationToken.None);

            Assert.That(outDto.EsPorDefecto, Is.False);
            Assert.That(seriesRepo.UnsetDefaultCalled, Is.False);
            Assert.That(seriesRepo.AddCount, Is.EqualTo(1));
            Assert.That(uow.SaveCount, Is.EqualTo(1));
        }
    }
}
