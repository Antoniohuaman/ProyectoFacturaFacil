using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ConfiguracionSistemaBC.Application.UseCases;
using ConfiguracionSistemaBC.Application.UseCases.DTOs;
using ConfiguracionSistemaBC.Domain.Aggregates;
using ConfiguracionSistemaBC.Domain.Repositories;
using ConfiguracionSistemaBC.Domain.ValueObjects;
using NUnit.Framework;
using SharedKernel.ValueObjects;

namespace ConfiguracionSistemaBC.Tests.Application.UseCases
{
    public class CambiarAmbienteEmpresaUseCaseTests
    {
        // ==== Infraestructura en memoria ====

        private sealed class InMemoryConfiguracionEmpresaRepository : IConfiguracionEmpresaRepository
        {
            private readonly Dictionary<string, ConfiguracionEmpresa> _byRuc = new(StringComparer.Ordinal);
            public void Save(ConfiguracionEmpresa agg) => _byRuc[agg.Ruc.Canonizado] = agg;

            // Implementación correcta del método requerido por el caso de uso
            public Task<ConfiguracionEmpresa?> FindByRucAsync(Ruc ruc, CancellationToken ct = default)
            {
                _byRuc.TryGetValue(ruc.Canonizado, out var found);
                return Task.FromResult(found);
            }

            // Métodos requeridos por la interfaz, implementados como NotImplemented
            public Task<ConfiguracionEmpresa?> GetByEmpresaIdAsync(EmpresaId empresaId, CancellationToken ct = default) => throw new NotImplementedException();
            public Task AddAsync(ConfiguracionEmpresa aggregate, CancellationToken ct = default) => throw new NotImplementedException();
            public Task UpdateAsync(ConfiguracionEmpresa aggregate, CancellationToken ct = default) => throw new NotImplementedException();
            public Task DeleteAsync(EmpresaId empresaId, CancellationToken ct = default) => throw new NotImplementedException();
            public Task<bool> UpdateIfVersionMatchAsync(ConfiguracionEmpresa aggregate, int expectedVersion, CancellationToken ct = default) => throw new NotImplementedException();
        }

        private sealed class InMemoryUnitOfWork : IUnitOfWork
        {
            public int Commits { get; private set; }
            public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                Commits++;
                return Task.CompletedTask;
            }
        }

        [Test]
        public async Task Handle_PRUEBA_a_PRODUCCION_CambiaAmbiente_IncrementaVersion_Y_ConfirmaUoW()
        {
            // Arrange
            var repo = new InMemoryConfiguracionEmpresaRepository();
            var uow  = new InMemoryUnitOfWork();

            var ruc = Ruc.From("20600893409");
            var direccion = CrearDomicilioFiscalPeru("150101", "AV. PRINCIPAL 123");
            var empresa = ConfiguracionEmpresa.RegistrarNueva(ruc, "MI EMPRESA S.A.C.", direccion, Moneda.PEN());
            Assert.That(empresa.Ambiente, Is.EqualTo(AmbienteFe.PRUEBA));

            var versionInicial = empresa.Version;
            repo.Save(empresa);

            var useCase = new CambiarAmbienteEmpresaUseCase(repo, uow);

            var input = new CambiarAmbienteEmpresaInputDto(
                Ruc: ruc.Canonizado,
                Destino: AmbienteFe.PRODUCCION // debe existir en tu SK
            );

            // Act
            var result = await useCase.Handle(input);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.EmpresaId, Is.EqualTo(empresa.EmpresaId.Value));
            Assert.That(result.Ruc.Canonizado, Is.EqualTo(ruc.Canonizado));
            Assert.That(result.AmbienteAnterior, Is.EqualTo(AmbienteFe.PRUEBA));
            Assert.That(result.AmbienteActual, Is.EqualTo(AmbienteFe.PRODUCCION));
            Assert.That(result.Version, Is.GreaterThan(versionInicial));
            Assert.That(uow.Commits, Is.EqualTo(1));

            // Estado del aggregate también quedó actualizado
            Assert.That(empresa.Ambiente, Is.EqualTo(AmbienteFe.PRODUCCION));
        }

        [Test]
        public void Handle_TransicionNoValida_LanzaExcepcionDeDominio()
        {
            // Arrange: primero llevamos a PRODUCCION y luego intentamos volver (asumiendo no es válido)
            var repo = new InMemoryConfiguracionEmpresaRepository();
            var uow  = new InMemoryUnitOfWork();

            var ruc = Ruc.From("20600893409");
            var direccion = CrearDomicilioFiscalPeru("150101", "CALLE UNO 999");
            var empresa = ConfiguracionEmpresa.RegistrarNueva(ruc, "X S.A.C.", direccion, Moneda.PEN());
            empresa.CambiarAmbiente(AmbienteFe.PRODUCCION);
            repo.Save(empresa);

            var useCase = new CambiarAmbienteEmpresaUseCase(repo, uow);

            var inputInvalido = new CambiarAmbienteEmpresaInputDto(
                Ruc: ruc.Canonizado,
                Destino: AmbienteFe.PRUEBA
            );

            // Act + Assert: no conocemos el tipo exacto que lanza ValidarTransicion
            Assert.That(async () => await useCase.Handle(inputInvalido),
                Throws.Exception);
            Assert.That(uow.Commits, Is.EqualTo(0));
        }

        // ==== Helpers de prueba (idénticos estilo a los que ya usas) ====

        private static DomicilioFiscal CrearDomicilioFiscalPeru(string ubigeo, string direccionLinea1)
        {
            // Usar la fábrica pública FromPeru, con datos dummy para divisiones y addressTypeCode
            return DomicilioFiscal.FromPeru(
                linea: direccionLinea1,
                ubigeo: ubigeo,
                departamento: "LIMA",
                provincia: "LIMA",
                distrito: "LIMA",
                addressTypeCode: "0000"
            );
        }
    }
}
