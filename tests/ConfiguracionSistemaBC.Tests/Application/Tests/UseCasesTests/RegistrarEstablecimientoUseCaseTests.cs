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
using ConfiguracionSistemaBC.Domain.Events;
using NUnit.Framework;
using SharedKernel.ValueObjects;

namespace ConfiguracionSistemaBC.Tests.Application.UseCases
{
    public class RegistrarEstablecimientoUseCaseTests
    {
        // ===== Infra en memoria (simple) =====

        private sealed class InMemoryConfiguracionEmpresaRepository : IConfiguracionEmpresaRepository
        {
            private readonly Dictionary<string, ConfiguracionEmpresa> _store = new(StringComparer.Ordinal);

            public Task<ConfiguracionEmpresa?> FindByRucAsync(Ruc ruc, CancellationToken ct = default)
            {
                _store.TryGetValue(ruc.Canonizado, out var agg);
                return Task.FromResult(agg);
            }

            public void Save(ConfiguracionEmpresa agg) => _store[agg.Ruc.Canonizado] = agg;

            // Si tu implementación real requiere Update explícito, añade:
            // public void Update(ConfiguracionEmpresa agg) => _store[agg.Ruc.Canonizado] = agg;
                // Métodos stub requeridos por la interfaz
                public Task<ConfiguracionEmpresa?> GetByEmpresaIdAsync(EmpresaId empresaId, CancellationToken ct = default) => Task.FromResult<ConfiguracionEmpresa?>(null);
                public Task AddAsync(ConfiguracionEmpresa aggregate, CancellationToken ct = default) => Task.CompletedTask;
                public Task UpdateAsync(ConfiguracionEmpresa aggregate, CancellationToken ct = default) => Task.CompletedTask;
                public Task DeleteAsync(EmpresaId empresaId, CancellationToken ct = default) => Task.CompletedTask;
                    public Task<bool> UpdateIfVersionMatchAsync(ConfiguracionEmpresa aggregate, int expectedVersion, CancellationToken ct = default) => Task.FromResult(true);
        }

        private sealed class InMemoryUnitOfWork : IUnitOfWork
        {
            public int SaveChangesCalls { get; private set; }
            public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                SaveChangesCalls++;
                return Task.CompletedTask;
            }
        }

        // ===== Helpers =====

        private static DomicilioFiscal CrearDomicilioFiscalPeru(string ubigeo, string direccionLinea1)
        {
            // Buscamos una fábrica estática tipo DomicilioFiscal.Peru(...)
            var t = typeof(DomicilioFiscal);
            var metodo = t.GetMethods(BindingFlags.Public | BindingFlags.Static)
                          .FirstOrDefault(m => string.Equals(m.Name, "Peru", StringComparison.OrdinalIgnoreCase));
            if (metodo is null)
            {
                Assert.Inconclusive("Ajusta el helper: no se encontró fábrica estática DomicilioFiscal.Peru(...)");
                throw new InvalidOperationException("Inalcanzable.");
            }

            var ps = metodo.GetParameters();
            var args = new object?[ps.Length];
            for (int i = 0; i < ps.Length; i++)
            {
                var p = ps[i];
                var pname = p.Name ?? string.Empty;
                if (p.ParameterType == typeof(string))
                {
                    if (pname.Contains("ubigeo", StringComparison.OrdinalIgnoreCase)) args[i] = ubigeo;
                    else if (pname.Contains("direccion", StringComparison.OrdinalIgnoreCase)) args[i] = direccionLinea1;
                    else args[i] = "LIMA";
                }
                else
                {
                    args[i] = Activator.CreateInstance(p.ParameterType);
                }
            }
            return (DomicilioFiscal)metodo.Invoke(null, args)!;
        }

        // ===== Tests =====

        [Test]
        public async Task Registrar_Establecimiento_Exitoso_Persiste_IncrementaVersion_Y_RetornaReadModel()
        {
            // Arrange
            var repo = new InMemoryConfiguracionEmpresaRepository();
            var uow  = new InMemoryUnitOfWork();

            var ruc = Ruc.From("20600893409");
            var domFiscal = CrearDomicilioFiscalPeru("150101", "AV. PRINCIPAL 123");
            var empresa = ConfiguracionEmpresa.RegistrarNueva(ruc, "MI EMPRESA S.A.C.", domFiscal, Moneda.PEN());
            var versionInicial = empresa.Version;
            var principalAntes = empresa.ObtenerEstablecimientoPrincipal();
            Assert.That(principalAntes, Is.Not.Null);
            repo.Save(empresa);

            var useCase = new RegistrarEstablecimientoUseCase(repo, uow);

            var input = new RegistrarEstablecimientoInputDto(
                Ruc: ruc.Canonizado,
                Codigo: "02",
                Nombre: "Sucursal San Isidro",
                Direccion: CrearDomicilioFiscalPeru("150131", "AV. DOS 456"),
                Telefono: null,
                Email: null
            );

            // Act
            var output = await useCase.Handle(input);

            // Assert
            Assert.That(output, Is.Not.Null);
            Assert.That(output.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(output.EmpresaId, Is.EqualTo(empresa.EmpresaId.Value));
            Assert.That(output.Codigo, Is.EqualTo("02"));
            Assert.That(output.Nombre, Is.EqualTo("Sucursal San Isidro"));
            Assert.That(output.Direccion, Is.Not.Null);
            Assert.That(output.Habilitado, Is.True);

            // Validar que se agregó al agregado y versión subió
            var encontrado = empresa.BuscarEstablecimientoPorCodigo("02");
            Assert.That(encontrado, Is.Not.Null);
            Assert.That(empresa.Version, Is.GreaterThan(versionInicial));

            // Validar UoW commit
            Assert.That(uow.SaveChangesCalls, Is.EqualTo(1));

            // El principal se mantiene siendo el inicial
            var principalDespues = empresa.ObtenerEstablecimientoPrincipal();
            Assert.That(principalDespues!.Id, Is.EqualTo(principalAntes!.Id));

            // Evento de dominio agregado
            var ultimoEvt = empresa.DomainEvents.LastOrDefault();
            Assert.That(ultimoEvt, Is.TypeOf<EstablecimientoRegistrado>());
            var evt = (EstablecimientoRegistrado)ultimoEvt!;
            Assert.That(evt.EmpresaId.Value, Is.EqualTo(empresa.EmpresaId.Value));
            Assert.That(evt.EstablecimientoCodigo, Is.EqualTo("02"));
        }

        [Test]
        public async Task Registrar_Establecimiento_CodigoDuplicado_LanzaInvalidOperation_Y_NoCommit()
        {
            // Arrange
            var repo = new InMemoryConfiguracionEmpresaRepository();
            var uow  = new InMemoryUnitOfWork();

            var ruc = Ruc.From("20600893409");
            var domFiscal = CrearDomicilioFiscalPeru("150101", "JR. CENTRAL 100");
            var empresa = ConfiguracionEmpresa.RegistrarNueva(ruc, "EMPRESA XYZ S.A.C.", domFiscal, Moneda.PEN());
            repo.Save(empresa);

            // Ya existe "01" por bootstrap; intentemos registrar otro con "01"
            var useCase = new RegistrarEstablecimientoUseCase(repo, uow);
            var inputDuplicado = new RegistrarEstablecimientoInputDto(
                Ruc: ruc.Canonizado,
                Codigo: "01",
                Nombre: "Copia del Principal",
                Direccion: CrearDomicilioFiscalPeru("150101", "JR. CENTRAL 101"),
                Telefono: null,
                Email: null
            );

            // Act + Assert
            Assert.That(async () => await useCase.Handle(inputDuplicado),
                Throws.TypeOf<InvalidOperationException>());

            Assert.That(uow.SaveChangesCalls, Is.EqualTo(0));
                await Task.CompletedTask;
        }

        [Test]
        public void Registrar_Establecimiento_ConDatosInvalidos_ValidaEntrada()
        {
            var repo = new InMemoryConfiguracionEmpresaRepository();
            var uow  = new InMemoryUnitOfWork();
            var useCase = new RegistrarEstablecimientoUseCase(repo, uow);

            // RUC vacío
            var bad1 = new RegistrarEstablecimientoInputDto(
                Ruc: "",
                Codigo: "02",
                Nombre: "Suc",
                Direccion: CrearDomicilioFiscalPeru("150101", "X"),
                Telefono: null,
                Email: null
            );
            Assert.That(async () => await useCase.Handle(bad1), Throws.TypeOf<ArgumentException>());

            // Código vacío
            var bad2 = new RegistrarEstablecimientoInputDto(
                Ruc: "20111111111",
                Codigo: " ",
                Nombre: "Suc",
                Direccion: CrearDomicilioFiscalPeru("150101", "X"),
                Telefono: null,
                Email: null
            );
            Assert.That(async () => await useCase.Handle(bad2), Throws.TypeOf<ArgumentException>());

            // Nombre vacío
            var bad3 = new RegistrarEstablecimientoInputDto(
                Ruc: "20111111111",
                Codigo: "02",
                Nombre: " ",
                Direccion: CrearDomicilioFiscalPeru("150101", "X"),
                Telefono: null,
                Email: null
            );
            Assert.That(async () => await useCase.Handle(bad3), Throws.TypeOf<ArgumentException>());

            // Dirección nula
            var bad4 = new RegistrarEstablecimientoInputDto(
                Ruc: "20111111111",
                Codigo: "02",
                Nombre: "Suc",
                Direccion: null!,
                Telefono: null,
                Email: null
            );
            Assert.That(async () => await useCase.Handle(bad4), Throws.TypeOf<ArgumentNullException>());
        }
    }
}
