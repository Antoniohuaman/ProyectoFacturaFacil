using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ConfiguracionSistemaBC.Application.DTOs;
using ConfiguracionSistemaBC.Domain.Repositories;
using ConfiguracionSistemaBC.Application.UseCases;
using ConfiguracionSistemaBC.Domain.Aggregates;
using SharedKernel.ValueObjects; // DomicilioFiscal, Moneda
using ConfiguracionSistemaBC.Domain.ValueObjects; // Ruc
using System.Linq;

namespace ConfiguracionSistemaBC.Tests.Application.UseCases
{
    public class RegistrarUnidadDeMedidaUseCaseTests
    {
        private sealed class InMemoryRepo : IConfiguracionEmpresaRepository
        {
            private readonly Dictionary<string, ConfiguracionEmpresa> _db = new();

            public void Seed(ConfiguracionEmpresa agg)
                => _db[agg.EmpresaId.Value] = agg;

            public Task<ConfiguracionEmpresa?> GetByEmpresaIdAsync(EmpresaId empresaId, CancellationToken ct = default)
                => Task.FromResult(_db.TryGetValue(empresaId.Value, out var a) ? a : null);

            public Task AddAsync(ConfiguracionEmpresa aggregate, CancellationToken ct = default)
                => Task.CompletedTask;

            public Task UpdateAsync(ConfiguracionEmpresa aggregate, CancellationToken ct = default)
                => Task.CompletedTask;

            public Task DeleteAsync(EmpresaId empresaId, CancellationToken ct = default)
                => Task.CompletedTask;

            public Task<ConfiguracionEmpresa?> FindByRucAsync(Ruc ruc, CancellationToken ct = default)
                => Task.FromResult<ConfiguracionEmpresa?>(null);

            public Task<bool> UpdateIfVersionMatchAsync(ConfiguracionEmpresa aggregate, int version, CancellationToken ct = default)
                => Task.FromResult(true);
        }

        private sealed class InMemoryUow : IUnitOfWork
        {
            public int Commits { get; private set; }
            public Task SaveChangesAsync(CancellationToken ct = default) { Commits++; return Task.CompletedTask; }
        }

        /// <summary>
        /// Crea una empresa nueva usando el RUC indicado por el usuario (20600893409)
        /// con domicilio fiscal en Perú y moneda base PEN.
        /// </summary>
        private static ConfiguracionEmpresa CrearEmpresaDemo()
        {
            var ruc = Ruc.From("20600893409");
            var direccion = DomicilioFiscal.FromPeru(
                linea: "AV. DEMO 123",
                ubigeo: "150101",
                departamento: "LIMA",
                provincia: "LIMA",
                distrito: "LIMA"
            );
            return ConfiguracionEmpresa.RegistrarNueva(
                ruc,
                razonSocial: "EMPRESA DEMO S.A.C.",
                direccionFiscal: direccion,
                monedaBase: Moneda.PEN()
            );
        }

        [Test]
        public async Task Registra_unidades_y_cambia_la_por_defecto()
        {
            // Arrange
            var empresa = CrearEmpresaDemo();
            var repo = new InMemoryRepo();
            repo.Seed(empresa);

            var uow = new InMemoryUow();
            var uc = new RegistrarUnidadDeMedidaUseCase(repo, uow);

            var input = new RegistrarUnidadDeMedidaInputDto
            {
                EmpresaId = empresa.EmpresaId.Value,
                Items = new List<RegistrarUnidadDeMedidaInputDto.UnidadDeMedidaItem>
                {
                    // códigos no presentes en el bootstrap del aggregate
                    new() { Codigo = "BAG", Nombre = "BOLSA", Visible = true, Orden = 50, EsPorDefecto = true },
                    new() { Codigo = "PKT", Nombre = "PAQUETE", Visible = true, Orden = 51 }
                }
            };

            // Act
            var output = await uc.ExecuteAsync(input);

            // Assert
            Assert.That(output.EmpresaId, Is.EqualTo(empresa.EmpresaId.Value));
            Assert.That(output.TotalAgregadas, Is.EqualTo(2));
            Assert.That(uow.Commits, Is.EqualTo(1));

            var def = empresa.ObtenerUnidadDeMedidaPorDefecto();
            Assert.That(def, Is.Not.Null);
            Assert.That(def!.Unidad.Codigo, Is.EqualTo("BAG"));
            Assert.That(def.Nombre, Is.EqualTo("BOLSA"));

            var todas = empresa.ListarUnidadesDeMedida();
            Assert.That(todas.Any(u => u.Unidad.Codigo == "PKT" && u.Nombre == "PAQUETE"), Is.True);
        }

        [Test]
        public async Task Rechaza_codigo_duplicado()
        {
            // Arrange
            var empresa = CrearEmpresaDemo();
            var repo = new InMemoryRepo();
            repo.Seed(empresa);
            var uow = new InMemoryUow();
            var uc = new RegistrarUnidadDeMedidaUseCase(repo, uow);

            var primera = new RegistrarUnidadDeMedidaInputDto
            {
                EmpresaId = empresa.EmpresaId.Value,
                Items = new()
                {
                    new() { Codigo = "BXG", Nombre = "CAJA GRANDE", Visible = true }
                }
            };
            await uc.ExecuteAsync(primera);

            var duplicada = new RegistrarUnidadDeMedidaInputDto
            {
                EmpresaId = empresa.EmpresaId.Value,
                Items = new()
                {
                    new() { Codigo = "BXG", Nombre = "CAJA GRANDE", Visible = true } // mismo código
                }
            };

            // Act / Assert
            Assert.That(async () => await uc.ExecuteAsync(duplicada),
                        Throws.TypeOf<InvalidOperationException>());
        }
    }
}
