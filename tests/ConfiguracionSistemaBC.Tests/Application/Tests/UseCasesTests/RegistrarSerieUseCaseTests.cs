using System;
using System.Threading;
using System.Threading.Tasks;
using ConfiguracionSistemaBC.Application.UseCases;
using ConfiguracionSistemaBC.Application.UseCases.Dtos;
using ConfiguracionSistemaBC.Domain.Aggregates;
using ConfiguracionSistemaBC.Domain.Repositories;
using ConfiguracionSistemaBC.Domain.ValueObjects;
using NUnit.Framework;
using SharedKernel.ValueObjects;

namespace ConfiguracionSistemaBC.Tests.Application.UseCases
{
    public class RegistrarSerieUseCaseTests
    {
        private InMemoryRepo _repo = null!;
        private InMemoryUow _uow = null!;
        private RegistrarSerieUseCase _sut = null!;
        private ConfiguracionEmpresa _empresa = null!;

        [SetUp]
        public void SetUp()
        {
            _repo = new InMemoryRepo();
            _uow = new InMemoryUow();

            // Arrange: empresa con bootstrap (establecimiento principal + FE01/BE01 y catálogos)
            var ruc = Ruc.From("20600893409");
            var domicilio = DomicilioFiscal.FromPeru(
                linea: "Av. Prueba 123",
                ubigeo: "150101",
                departamento: "LIMA",
                provincia: "LIMA",
                distrito: "LIMA");

            _empresa = ConfiguracionEmpresa.RegistrarNueva(
                ruc,
                "ACME S.A.C.",
                domicilio,
                Moneda.PEN());

            _repo.Store(_empresa);

            _sut = new RegistrarSerieUseCase(_repo, _uow);
        }

        [Test]
        public async Task Registra_nueva_serie_para_factura_y_la_deja_por_defecto_ok()
        {
            // FE01 ya existe por bootstrap, por eso usamos FE02
            var principal = _empresa.ObtenerEstablecimientoPrincipal();
            Assert.That(principal, Is.Not.Null);

            var input = new RegistrarSerieInputDto
            {
                Ruc = _empresa.Ruc.Canonizado,
                EstablecimientoId = principal!.Id,
                TipoComprobanteCodigo = "01",     // Factura
                Serie = "FE02",
                CorrelativoInicial = 1,
                // si no mandamos tipo operación, el agregado usa Default (0101)
                EsPorDefecto = true
            };

            var outDto = await _sut.Handle(input);

            Assert.That(outDto.Serie, Is.EqualTo("FE02"));
            Assert.That(outDto.EsPorDefecto, Is.True);
            Assert.That(outDto.Bloqueada, Is.False);

            // Verificar que el default de Factura ahora apunte a FE02
            var defFactura = _empresa.ObtenerSeriePorDefecto(TipoComprobanteCodigo.Factura);
            Assert.That(defFactura, Is.Not.Null);
            Assert.That(defFactura!.Serie, Is.EqualTo("FE02"));
        }

        [Test]
        public void No_permite_duplicar_serie_para_mismo_tipo()
        {
            var principal = _empresa.ObtenerEstablecimientoPrincipal()!;
            // FE01 ya existe (bootstrap). Intentar crearla otra vez debe fallar.
            var input = new RegistrarSerieInputDto
            {
                Ruc = _empresa.Ruc.Canonizado,
                EstablecimientoId = principal.Id,
                TipoComprobanteCodigo = "01", // Factura
                Serie = "FE01",                // duplicada
                CorrelativoInicial = 1,
                EsPorDefecto = false
            };

            Assert.That(async () => await _sut.Handle(input),
                Throws.TypeOf<InvalidOperationException>()
                      .With.Message.Contains("ya existe"));
        }

        [Test]
        public void Valida_prefijo_por_tipo_factura()
        {
            var principal = _empresa.ObtenerEstablecimientoPrincipal()!;
            // Para Factura, el prefijo debe ser F*; usar BE99 debe reventar por ValidarSegunTipo
            var input = new RegistrarSerieInputDto
            {
                Ruc = _empresa.Ruc.Canonizado,
                EstablecimientoId = principal.Id,
                TipoComprobanteCodigo = "01", // Factura
                Serie = "BE99",
                CorrelativoInicial = 1
            };

            Assert.That(async () => await _sut.Handle(input),
                Throws.Exception); // El agregado lanza según su validación
        }

        [Test]
        public async Task Si_no_envio_tipo_operacion_usa_Default_y_actualiza_version()
        {
            var principal = _empresa.ObtenerEstablecimientoPrincipal()!;
            var versionAntes = _empresa.Version;

            var input = new RegistrarSerieInputDto
            {
                Ruc = _empresa.Ruc.Canonizado,
                EstablecimientoId = principal.Id,
                TipoComprobanteCodigo = "03", // Boleta
                Serie = "BE02",
                CorrelativoInicial = 5,
                TipoOperacionCodigo = null,
                EsPorDefecto = false
            };

            var outDto = await _sut.Handle(input);

            Assert.That(outDto.TipoOperacionCodigo, Is.EqualTo(TipoOperacion.Default.Codigo));
            Assert.That(_empresa.Version, Is.GreaterThan(versionAntes));
        }

        // ===== In-memory doubles =====

        private sealed class InMemoryRepo : IConfiguracionEmpresaRepository
        {
            private ConfiguracionEmpresa? _current;

            public void Store(ConfiguracionEmpresa agg) => _current = agg;

            public Task<ConfiguracionEmpresa?> GetByEmpresaIdAsync(EmpresaId empresaId, CancellationToken ct = default)
                => Task.FromResult(_current?.EmpresaId == empresaId ? _current : null);

            public Task AddAsync(ConfiguracionEmpresa aggregate, CancellationToken ct = default)
            {
                _current = aggregate;
                return Task.CompletedTask;
            }

            public Task UpdateAsync(ConfiguracionEmpresa aggregate, CancellationToken ct = default)
            {
                _current = aggregate;
                return Task.CompletedTask;
            }

            public Task DeleteAsync(EmpresaId empresaId, CancellationToken ct = default)
            {
                if (_current?.EmpresaId == empresaId) _current = null;
                return Task.CompletedTask;
            }

            public Task<ConfiguracionEmpresa?> FindByRucAsync(Ruc ruc, CancellationToken ct = default)
                => Task.FromResult(_current?.Ruc.Canonizado == ruc.Canonizado ? _current : null);

            public Task<bool> UpdateIfVersionMatchAsync(ConfiguracionEmpresa aggregate, int expectedVersion, CancellationToken ct = default)
            {
                if (_current != null && _current.Version == expectedVersion)
                {
                    _current = aggregate;
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            }
        }

        private sealed class InMemoryUow : IUnitOfWork
        {
            public int Saves { get; private set; }
            public Task SaveChangesAsync(CancellationToken ct = default)
            {
                Saves++;
                return Task.CompletedTask;
            }
        }
    }
}
