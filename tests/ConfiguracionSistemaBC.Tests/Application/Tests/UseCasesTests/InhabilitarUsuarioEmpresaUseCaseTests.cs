using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

using ConfiguracionSistemaBC.Application.UseCases;
using ConfiguracionSistemaBC.Domain.Aggregates;
using ConfiguracionSistemaBC.Domain.Repositories;
using SharedKernel.ValueObjects;

namespace ConfiguracionSistemaBC.Tests.Application
{
    [TestFixture]
    public class InhabilitarUsuarioEmpresaUseCaseTests
    {
        private const string EmpresaCanonica = "20600893409";

        // ---------- Fakes in-memory ----------

        private sealed class FakeUnitOfWork : IUnitOfWork
        {
            public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
        }

        private sealed class FakeUsuarioEmpresaRepository : IUsuarioEmpresaRepository
        {
            private readonly Dictionary<(string emp, Guid user), UsuarioEmpresa> _store = new();
            public bool UpdateCalled { get; private set; }

            public Task<UsuarioEmpresa?> GetAsync(EmpresaId empresaId, UsuarioId usuarioId, CancellationToken ct = default)
            {
                _store.TryGetValue((empresaId.Value, usuarioId.Value), out var agg);
                return Task.FromResult<UsuarioEmpresa?>(agg);
            }

            public Task<bool> ExistsAsync(EmpresaId empresaId, UsuarioId usuarioId, CancellationToken ct = default)
                => Task.FromResult(_store.ContainsKey((empresaId.Value, usuarioId.Value)));

            public Task<UsuarioEmpresa?> GetByEmailAsync(EmpresaId empresaId, Email email, CancellationToken ct = default)
                => Task.FromResult<UsuarioEmpresa?>(null);

            public Task<IReadOnlyList<UsuarioEmpresa>> ListAsync(EmpresaId empresaId, UsuarioEmpresaEstado? estado = null, EstablecimientoId? establecimientoId = null, Guid? rolId = null, int skip = 0, int take = 50, CancellationToken ct = default)
                => Task.FromResult<IReadOnlyList<UsuarioEmpresa>>(_store.Values.Where(x => x.EmpresaId.Value == empresaId.Value).ToList());

            public Task<int> CountAsync(EmpresaId empresaId, UsuarioEmpresaEstado? estado = null, CancellationToken ct = default)
                => Task.FromResult(_store.Values.Count(x => x.EmpresaId.Value == empresaId.Value));

            public Task<IReadOnlyList<EstablecimientoId>> ListEstablecimientosAsync(EmpresaId empresaId, UsuarioId usuarioId, CancellationToken ct = default)
                => Task.FromResult<IReadOnlyList<EstablecimientoId>>(new List<EstablecimientoId>());

            public Task<bool> TieneAccesoEstablecimientoAsync(EmpresaId empresaId, UsuarioId usuarioId, EstablecimientoId establecimientoId, CancellationToken ct = default)
                => Task.FromResult(false);

            public Task<bool> EmailExisteEnEmpresaAsync(EmpresaId empresaId, Email email, CancellationToken ct = default)
                => Task.FromResult(false);

            public Task AddAsync(UsuarioEmpresa agregado, CancellationToken ct = default)
            {
                _store[(agregado.EmpresaId.Value, agregado.UsuarioId.Value)] = agregado;
                return Task.CompletedTask;
            }

            public Task UpdateAsync(UsuarioEmpresa agregado, int expectedVersion, CancellationToken ct = default)
            {
                UpdateCalled = true;

                // Concurrency check (expectedVersion debe ser la versión previa al cambio)
                var key = (agregado.EmpresaId.Value, agregado.UsuarioId.Value);
                if (!_store.TryGetValue(key, out var current))
                    throw new KeyNotFoundException("No existe el agregado para actualizar.");

                // current.Version ya fue incrementada por la operación de dominio.
                // Por lo tanto, la versión esperada debe ser (current.Version - 1).
                if (current.Version != expectedVersion + 1)
                    throw new InvalidOperationException("Versión inesperada (conflicto de concurrencia).");

                _store[key] = agregado;
                return Task.CompletedTask;
            }

            public Task DeleteAsync(EmpresaId empresaId, UsuarioId usuarioId, int expectedVersion, CancellationToken ct = default)
            {
                _store.Remove((empresaId.Value, usuarioId.Value));
                return Task.CompletedTask;
            }
        }

        private static UsuarioEmpresa NuevoUsuario(EmpresaId empresaId, string email)
        {
            var agg = UsuarioEmpresa.Crear(
                empresaId,
                UsuarioId.New(),
                documento: null,
                nombre: NombrePersona.Crear("Juan", "Prueba"),
                emailContacto: Email.Create(email),
                telefonoContacto: null,
                rolesEmpresaIds: null,
                accesosIniciales: new (EstablecimientoId EstablecimientoId, IEnumerable<Guid> RolIds)[] {
                    (EstablecimientoId.New(), new [] { Guid.NewGuid() })
                }
            );
            return agg;
        }

        // ---------- Tests ----------

        [Test]
        public async Task Inhabilita_usuario_habilitado_incrementa_version_y_persiste()
        {
            // Arrange
            var repo = new FakeUsuarioEmpresaRepository();
            var uow  = new FakeUnitOfWork();
            var useCase = new InhabilitarUsuarioEmpresaUseCase(repo, uow, tenantContext: null);

            var empresaId = EmpresaId.From(EmpresaCanonica);
            var usuario = NuevoUsuario(empresaId, "test@acme.com");
            // Lo pasamos a Habilitado para simular que ya estaba operativo
            usuario.MarcarConfirmadoPorIdentidad(); // cambia estado y +1 versión
            usuario.ForzarVersionParaPruebas(5);    // para controlar concurrencia en test
            await repo.AddAsync(usuario, CancellationToken.None);

            var input = new InhabilitarUsuarioEmpresaInputDto
            {
                EmpresaId = empresaId.Value,
                UsuarioId = usuario.UsuarioId.Value,
                Razon = "Baja temporal",
                ExpectedVersion = 5 // versión previa al cambio
            };

            // Act
            var result = await useCase.HandleAsync(input, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.EmpresaId, Is.EqualTo(empresaId.Value));
            Assert.That(result.UsuarioId, Is.EqualTo(usuario.UsuarioId.Value));
            Assert.That(result.Estado, Is.EqualTo(UsuarioEmpresaEstado.Inhabilitado.ToString()));
            Assert.That(result.NuevaVersion, Is.EqualTo(6)); // 5 + 1
            Assert.That(result.YaEstabaInhabilitado, Is.False);
            Assert.That(repo.UpdateCalled, Is.True);
        }

        [Test]
        public async Task Si_ya_estaba_inhabilitado_no_actualiza_ni_guarda()
        {
            // Arrange
            var repo = new FakeUsuarioEmpresaRepository();
            var uow  = new FakeUnitOfWork();
            var useCase = new InhabilitarUsuarioEmpresaUseCase(repo, uow, tenantContext: null);

            var empresaId = EmpresaId.From(EmpresaCanonica);
            var usuario = NuevoUsuario(empresaId, "ya@inhabilitado.com");
            usuario.ForzarVersionParaPruebas(3);
            // Inhabilitar previamente
            usuario.Inhabilitar("pre");
            await repo.AddAsync(usuario, CancellationToken.None);

            var input = new InhabilitarUsuarioEmpresaInputDto
            {
                EmpresaId = empresaId.Value,
                UsuarioId = usuario.UsuarioId.Value,
                Razon = "otra vez",
                ExpectedVersion = usuario.Version // da igual, no se usará
            };

            // Act
            var result = await useCase.HandleAsync(input, CancellationToken.None);

            // Assert
            Assert.That(result.YaEstabaInhabilitado, Is.True);
            Assert.That(result.Estado, Is.EqualTo(UsuarioEmpresaEstado.Inhabilitado.ToString()));
            Assert.That(repo.UpdateCalled, Is.False);
        }

        [Test]
        public void Lanza_si_no_existe()
        {
            // Arrange
            var repo = new FakeUsuarioEmpresaRepository();
            var uow  = new FakeUnitOfWork();
            var useCase = new InhabilitarUsuarioEmpresaUseCase(repo, uow, tenantContext: null);

            var input = new InhabilitarUsuarioEmpresaInputDto
            {
                EmpresaId = EmpresaCanonica,
                UsuarioId = Guid.NewGuid(),
                Razon = "X",
                ExpectedVersion = 0
            };

            // Act / Assert
            Assert.That(async () => await useCase.HandleAsync(input, CancellationToken.None),
                Throws.TypeOf<KeyNotFoundException>().With.Message.Contains("no encontrado"));
        }

        [Test]
        public async Task Lanza_concurrencia_si_expectedVersion_no_coincide()
        {
            // Arrange
            var repo = new FakeUsuarioEmpresaRepository();
            var uow  = new FakeUnitOfWork();
            var useCase = new InhabilitarUsuarioEmpresaUseCase(repo, uow, tenantContext: null);

            var empresaId = EmpresaId.From(EmpresaCanonica);
            var usuario = NuevoUsuario(empresaId, "conc@acme.com");
            usuario.ForzarVersionParaPruebas(2);
            await repo.AddAsync(usuario, CancellationToken.None);

            // Wrong expected version
            var input = new InhabilitarUsuarioEmpresaInputDto
            {
                EmpresaId = empresaId.Value,
                UsuarioId = usuario.UsuarioId.Value,
                Razon = "R",
                ExpectedVersion = 1 // debería ser 2
            };

            // Act / Assert
            Assert.That(async () => await useCase.HandleAsync(input, CancellationToken.None),
                Throws.InvalidOperationException.With.Message.Contains("concurrencia"));
        }
    }
}
