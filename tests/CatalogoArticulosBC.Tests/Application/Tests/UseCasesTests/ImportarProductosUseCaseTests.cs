using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CatalogoArticulosBC.Application.DTOs;
using CatalogoArticulosBC.Application.Services;
using CatalogoArticulosBC.Application.UseCases;
using CatalogoArticulosBC.Adapters.Output.Persistence.InMemory;
using CatalogoArticulosBC.Application.Interfaces;
using CatalogoArticulosBC.Domain.Repositories;
using NUnit.Framework;
using SharedKernel.Application.Interfaces;
using ConfiguracionSistemaBC.Domain.Repositories;
using ConfiguracionSistemaBC.Domain.Aggregates;
using ConfiguracionSistemaBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace CatalogoArticulosBC.Tests.Application.Tests.UseCasesTests
{
    [TestFixture]
    public class ImportarProductosUseCaseTests
    {
        private DefaultImportSchemaProvider _schemaProvider = null!;
        private InMemoryCatalogoArticulosRepository _repo = null!;
        private InMemoryUnitOfWork _uow = null!;
        private ITenantContext _tenant = null!;
            private IConfiguracionEmpresaRepository _configRepo = null!;
            private Guid _registeredEstId;

        [SetUp]
        public void SetUp()
        {
            _schemaProvider = new DefaultImportSchemaProvider();
            _repo = new InMemoryCatalogoArticulosRepository();
            _uow = new InMemoryUnitOfWork();
            _tenant = new FakeTenantContext();
            // Config repo with a configuration that contains a numeric 4-digit establishment code "0001"
            var ruc = Ruc.From("20100070970");
            var config = ConfiguracionEmpresa.RegistrarNueva(ruc, "Empresa Test", DomicilioFiscal.FromPeru("Calle 1", ubigeo: "010101", departamento: "Lima", provincia: "Lima", distrito: "Lima", addressTypeCode: "0000"), Moneda.PEN());
            _registeredEstId = config.RegistrarEstablecimiento("0001", "Establecimiento 0001", DomicilioFiscal.FromPeru("Calle 1", ubigeo: "010101", departamento: "Lima", provincia: "Lima", distrito: "Lima", addressTypeCode: "0000"));
            _configRepo = new FakeConfigRepo(config);
        }

        class FakeConfigRepo : IConfiguracionEmpresaRepository
        {
            private readonly ConfiguracionEmpresa _cfg;
            public FakeConfigRepo(ConfiguracionEmpresa cfg) { _cfg = cfg; }
            public Task AddAsync(ConfiguracionEmpresa aggregate, System.Threading.CancellationToken ct = default) => throw new NotImplementedException();
            public Task DeleteAsync(SharedKernel.ValueObjects.EmpresaId empresaId, System.Threading.CancellationToken ct = default) => throw new NotImplementedException();
            public Task<ConfiguracionEmpresa?> FindByRucAsync(ConfiguracionSistemaBC.Domain.ValueObjects.Ruc ruc, System.Threading.CancellationToken ct = default) => Task.FromResult<ConfiguracionEmpresa?>(_cfg);
            public Task<ConfiguracionEmpresa?> GetByEmpresaIdAsync(SharedKernel.ValueObjects.EmpresaId empresaId, System.Threading.CancellationToken ct = default) => Task.FromResult<ConfiguracionEmpresa?>(_cfg);
            public Task<bool> EstablecimientoExisteAsync(SharedKernel.ValueObjects.EmpresaId empresaId, SharedKernel.ValueObjects.EstablecimientoId establecimientoId, System.Threading.CancellationToken ct = default) => Task.FromResult(true);
            public Task<bool> SerieExisteAsync(SharedKernel.ValueObjects.EmpresaId empresaId, ConfiguracionSistemaBC.Domain.ValueObjects.TipoComprobanteCodigo tipo, ConfiguracionSistemaBC.Domain.ValueObjects.SerieCodigo serie, System.Threading.CancellationToken ct = default) => throw new NotImplementedException();
            public Task<bool> UnidadDeMedidaExisteAsync(SharedKernel.ValueObjects.EmpresaId empresaId, SharedKernel.ValueObjects.UnidadDeMedida unidad, System.Threading.CancellationToken ct = default) => Task.FromResult(true);
            public Task UpdateAsync(ConfiguracionEmpresa aggregate, System.Threading.CancellationToken ct = default) => throw new NotImplementedException();
            public Task<bool> UpdateIfVersionMatchAsync(ConfiguracionEmpresa aggregate, int expectedVersion, System.Threading.CancellationToken ct = default) => throw new NotImplementedException();
        }

        class FakeTenantContext : ITenantContext
        {
            public TenantId TenantId => TenantId.New();
            public EmpresaId EmpresaId => EmpresaId.From("EMP1");
        }

        class FakeParser : IImportExportService
        {
            private readonly ParsedFileDto _parsed;

            public FakeParser(ParsedFileDto parsed) => _parsed = parsed;

            public Task ImportarAsync(Stream csvStream, System.Threading.CancellationToken ct = default) => Task.CompletedTask;
            public Task<Stream> ExportarAsync(System.Threading.CancellationToken ct = default) => Task.FromResult<Stream>(new MemoryStream());
            public Task<ResultadoImportacionDto> ImportarDesdeExcelAsync(string filePath) => Task.FromResult<ResultadoImportacionDto>(null!);
            public Task<ResultadoExportacionDto> ExportarProductosAsync(string filePath) => Task.FromResult<ResultadoExportacionDto>(null!);
            public Task<ParsedFileDto> ParseAsync(Stream stream, string fileName, System.Threading.CancellationToken ct = default) => Task.FromResult(_parsed);
        }

        private static Dictionary<string, string?> Row(params (string k, string? v)[] cols)
        {
            return cols.ToDictionary(x => x.k, x => x.v);
        }

        [Test]
        public async Task CreateOnly_Xlsx_ThreeValidRows_Created3()
        {
            var headers = _schemaProvider.GetBasicaHeaders().ToArray();
            var rows = new List<Dictionary<string, string?>>
            {
                Row((headers[0], "SKU1"),(headers[1], "Prod1"),(headers[2], "UND"),(headers[3], "10"),(headers[4], Guid.NewGuid().ToString()),(headers[5], Guid.NewGuid().ToString())),
                Row((headers[0], "SKU2"),(headers[1], "Prod2"),(headers[2], "UND"),(headers[3], "10"),(headers[4], Guid.NewGuid().ToString()),(headers[5], Guid.NewGuid().ToString())),
                Row((headers[0], "SKU3"),(headers[1], "Prod3"),(headers[2], "UND"),(headers[3], "10"),(headers[4], Guid.NewGuid().ToString()),(headers[5], Guid.NewGuid().ToString()))
            };

            var parsed = new ParsedFileDto { Headers = headers, Rows = rows };
            var parser = new FakeParser(parsed);

            var uc = new ImportarProductosUseCase(parser, _schemaProvider, _repo, _uow, _tenant, _configRepo);
            using var ms = new MemoryStream();
            var req = new ImportarProductosUseCase.Request(ms, "file.xlsx", ImportarProductosUseCase.Mode.CreateOnly, ImportarProductosUseCase.OnSkuConflict.Error, null, false);
            var resp = await uc.Handle(req);

            Assert.That(resp.Resumen.Creadas, Is.EqualTo(3));
            Assert.That(resp.Resumen.Actualizadas, Is.EqualTo(0));
            Assert.That(resp.Errores.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task Upsert_Csv_ExistingSkus_Updates()
        {
            // Pre-seed repository with two products
            var headers = _schemaProvider.GetBasicaHeaders().ToArray();
            var sku1 = "EX1"; var sku2 = "EX2";
            // create existing
            var createParsed = new ParsedFileDto { Headers = headers, Rows = new List<Dictionary<string, string?>> { Row((headers[0], sku1),(headers[1], "P1"),(headers[2], "UND"),(headers[3], "10"),(headers[4], Guid.NewGuid().ToString()),(headers[5], Guid.NewGuid().ToString())), Row((headers[0], sku2),(headers[1], "P2"),(headers[2], "UND"),(headers[3], "10"),(headers[4], Guid.NewGuid().ToString()),(headers[5], Guid.NewGuid().ToString())) } };
            var parserCreate = new FakeParser(createParsed);
            var ucCreate = new ImportarProductosUseCase(parserCreate, _schemaProvider, _repo, _uow, _tenant, _configRepo);
            await ucCreate.Handle(new ImportarProductosUseCase.Request(new MemoryStream(), "init.xlsx", ImportarProductosUseCase.Mode.CreateOnly, ImportarProductosUseCase.OnSkuConflict.Error));

            // Now upsert with same SKUs but different names
            var rows = new List<Dictionary<string, string?>> {
                Row((headers[0], sku1),(headers[1], "P1-updated"),(headers[2], "UND"),(headers[3], "10"),(headers[4], Guid.NewGuid().ToString()),(headers[5], Guid.NewGuid().ToString())),
                Row((headers[0], sku2),(headers[1], "P2-updated"),(headers[2], "UND"),(headers[3], "10"),(headers[4], Guid.NewGuid().ToString()),(headers[5], Guid.NewGuid().ToString()))
            };
            var parsed = new ParsedFileDto { Headers = headers, Rows = rows };
            var parser = new FakeParser(parsed);
            var uc = new ImportarProductosUseCase(parser, _schemaProvider, _repo, _uow, _tenant, _configRepo);

            var resp = await uc.Handle(new ImportarProductosUseCase.Request(new MemoryStream(), "file.csv", ImportarProductosUseCase.Mode.Upsert, ImportarProductosUseCase.OnSkuConflict.Update));
            Assert.That(resp.Resumen.Actualizadas, Is.EqualTo(2));
            Assert.That(resp.Resumen.Creadas, Is.EqualTo(0));
        }

        [Test]
        public async Task MultipleEstablecimientos_AssignedToAggregate()
        {
            var headers = _schemaProvider.GetBasicaHeaders().ToArray();
            var estA = Guid.NewGuid().ToString();
            var estB = Guid.NewGuid().ToString();
            var estC = Guid.NewGuid().ToString();
            var sku = "MULTI1";
            var rows = new List<Dictionary<string, string?>> { Row((headers[0], sku),(headers[1], "M1"),(headers[2], "UND"),(headers[3], "10"),(headers[4], Guid.NewGuid().ToString()),(headers[5], estA+";"+estB+";"+estC)) };
            var parsed = new ParsedFileDto { Headers = headers, Rows = rows };
            var parser = new FakeParser(parsed);
            var uc = new ImportarProductosUseCase(parser, _schemaProvider, _repo, _uow, _tenant, _configRepo);

            var resp = await uc.Handle(new ImportarProductosUseCase.Request(new MemoryStream(), "file.xlsx", ImportarProductosUseCase.Mode.CreateOnly, ImportarProductosUseCase.OnSkuConflict.Error));
            Assert.That(resp.Resumen.Creadas, Is.EqualTo(1));
            var prod = await _repo.GetBySkuAsync(SharedKernel.ValueObjects.Sku.Crear(sku), EmpresaId.From("EMP1"));
            Assert.That(prod, Is.Not.Null);
            Assert.That(prod!.EstablecimientosAsignados.Count, Is.EqualTo(3));
        }

        [Test]
        public void MissingMinimum_ThrowsBusinessRuleException()
        {
            var headers = _schemaProvider.GetBasicaHeaders().Where(h => h != "UnidadMedida").ToArray(); // omit one minimum
            var parsed = new ParsedFileDto { Headers = headers, Rows = new List<Dictionary<string, string?>> { } };
            var parser = new FakeParser(parsed);
            var uc = new ImportarProductosUseCase(parser, _schemaProvider, _repo, _uow, _tenant, _configRepo);

            Assert.That(async () => await uc.Handle(new ImportarProductosUseCase.Request(new MemoryStream(), "file.xlsx")), Throws.TypeOf<SharedKernel.Exceptions.BusinessRuleException>());
        }

        [Test]
        public async Task OnSkuConflict_Skip_OmitsExisting()
        {
            var headers = _schemaProvider.GetBasicaHeaders().ToArray();
            var sku = "SKUSKIP";
            // create existing
            var createParsed = new ParsedFileDto { Headers = headers, Rows = new List<Dictionary<string, string?>> { Row((headers[0], sku),(headers[1], "P1"),(headers[2], "UND"),(headers[3], "10"),(headers[4], Guid.NewGuid().ToString()),(headers[5], Guid.NewGuid().ToString())) } };
            var parserCreate = new FakeParser(createParsed);
            var ucCreate = new ImportarProductosUseCase(parserCreate, _schemaProvider, _repo, _uow, _tenant, _configRepo);
            await ucCreate.Handle(new ImportarProductosUseCase.Request(new MemoryStream(), "init.xlsx", ImportarProductosUseCase.Mode.CreateOnly, ImportarProductosUseCase.OnSkuConflict.Error));

            // Now attempt import with same SKU but Skip
            var rows = new List<Dictionary<string, string?>> { Row((headers[0], sku),(headers[1], "P1-new"),(headers[2], "UND"),(headers[3], "10"),(headers[4], Guid.NewGuid().ToString()),(headers[5], Guid.NewGuid().ToString())) };
            var parsed = new ParsedFileDto { Headers = headers, Rows = rows };
            var parser = new FakeParser(parsed);
            var uc = new ImportarProductosUseCase(parser, _schemaProvider, _repo, _uow, _tenant, _configRepo);
            var resp = await uc.Handle(new ImportarProductosUseCase.Request(new MemoryStream(), "file.csv", ImportarProductosUseCase.Mode.CreateOnly, ImportarProductosUseCase.OnSkuConflict.Skip));
            Assert.That(resp.Resumen.Omitidas, Is.EqualTo(1));
        }

        [Test]
        public async Task InvalidRow_DoesNotAbort_RecordsError()
        {
            var headers = _schemaProvider.GetBasicaHeaders().ToArray();
            var rows = new List<Dictionary<string, string?>> {
                Row((headers[0], "OK1"),(headers[1], "P1"),(headers[2], "UND"),(headers[3], "10"),(headers[4], Guid.NewGuid().ToString()),(headers[5], Guid.NewGuid().ToString())),
                Row((headers[0], ""),(headers[1], "P-invalid"),(headers[2], ""),(headers[3], ""),(headers[4], ""),(headers[5], "")) // invalid missing sku
            };
            var parsed = new ParsedFileDto { Headers = headers, Rows = rows };
            var parser = new FakeParser(parsed);
            var uc = new ImportarProductosUseCase(parser, _schemaProvider, _repo, _uow, _tenant, _configRepo);
            var resp = await uc.Handle(new ImportarProductosUseCase.Request(new MemoryStream(), "file.xlsx", ImportarProductosUseCase.Mode.CreateOnly));
            Assert.That(resp.Resumen.TotalFilas, Is.EqualTo(2));
            Assert.That(resp.Resumen.ConError, Is.EqualTo(1));
        }

        [Test]
        public async Task Batching_CommitsPerBatchSize()
        {
            var headers = _schemaProvider.GetBasicaHeaders().ToArray();
            var rows = new List<Dictionary<string, string?>>();
            for (int i = 0; i < 5; i++)
                rows.Add(Row((headers[0], "B"+i),(headers[1], "P"+i),(headers[2], "UND"),(headers[3], "10"),(headers[4], Guid.NewGuid().ToString()),(headers[5], Guid.NewGuid().ToString())));
            var parsed = new ParsedFileDto { Headers = headers, Rows = rows };
            var parser = new FakeParser(parsed);
            var uc = new ImportarProductosUseCase(parser, _schemaProvider, _repo, _uow, _tenant, _configRepo);

            var resp = await uc.Handle(new ImportarProductosUseCase.Request(new MemoryStream(), "file.xlsx", ImportarProductosUseCase.Mode.CreateOnly, ImportarProductosUseCase.OnSkuConflict.Error, BatchSize:2));
            Assert.That(resp.Resumen.Creadas, Is.EqualTo(5));
            // InMemoryUnitOfWork marks committed when CommitAsync called; WasCommitted true
            Assert.That(_uow.WasCommitted, Is.True);
        }

        [Test]
        public async Task Numeric4DigitEstablishmentCode_IsResolvedAndAssigned()
        {
            var headers = _schemaProvider.GetBasicaHeaders().ToArray();
            var sku = "NUMEST1";
            // Use the numeric 4-digit code registered in SetUp: "0001"
            var rows = new List<Dictionary<string, string?>> { Row((headers[0], sku),(headers[1], "Producto"),(headers[2], "UND"),(headers[3], "10"),(headers[4], Guid.NewGuid().ToString()),(headers[5], "0001")) };
            var parsed = new ParsedFileDto { Headers = headers, Rows = rows };
            var parser = new FakeParser(parsed);

            var uc = new ImportarProductosUseCase(parser, _schemaProvider, _repo, _uow, _tenant, _configRepo);
            var resp = await uc.Handle(new ImportarProductosUseCase.Request(new MemoryStream(), "file.xlsx", ImportarProductosUseCase.Mode.CreateOnly));

            Assert.That(resp.Resumen.Creadas, Is.EqualTo(1));
            var prod = await _repo.GetBySkuAsync(SharedKernel.ValueObjects.Sku.Crear(sku), EmpresaId.From("EMP1"));
            Assert.That(prod, Is.Not.Null);
            Assert.That(prod!.EstablecimientosAsignados.Count, Is.EqualTo(1));
            Assert.That(prod.EstablecimientosAsignados.First().Value, Is.EqualTo(_registeredEstId));
        }
    }
}
