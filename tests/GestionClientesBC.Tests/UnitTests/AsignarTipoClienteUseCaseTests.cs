using System;
using System.Linq;
using System.Threading.Tasks;
using GestionClientesBC.Application.UseCases;
using GestionClientesBC.Adapters.Output.Persistence.InMemory;
using GestionClientesBC.Domain.ValueObjects;
using GestionClientesBC.Application.Interfaces;
using NUnit.Framework;

namespace GestionClientesBC.Tests.UnitTests.UseCases
{
    public class AsignarTipoClienteUseCaseTests
    {
        private class FakeUserContext : IUserContext
        {
            public bool HasPermission(string permission) => permission == "EditarCliente";
        }

        private class FakeUserSinPermiso : IUserContext
        {
            public bool HasPermission(string permission) => false;
        }

        [Test]
        public async Task AsignarAsync_TipoValido_CambiaTipoCliente()
        {
            var repo = new InMemoryGestionClientesRepository();
            var uow = new InMemoryUnitOfWork();
            var useCase = new AsignarTipoClienteUseCase(repo, uow);

            var cliente = new GestionClientesBC.Domain.Aggregates.Cliente(
                Guid.NewGuid(),
                new DocumentoIdentidad(TipoDocumento.DNI, "12345678"),
                "Cliente Test",
                "cliente@mail.com",
                "999999999",
                "Calle 1",
                TipoCliente.Minorista,
                EstadoCliente.Activo
            );
            await repo.AddAsync(cliente);

            await useCase.AsignarAsync(cliente.ClienteId, "Mayorista", new FakeUserContext());

            var actualizado = await repo.GetByIdAsync(cliente.ClienteId);
            Assert.That(actualizado, Is.Not.Null);
            Assert.That(actualizado.TipoCliente, Is.EqualTo(TipoCliente.Mayorista));
            Assert.That(actualizado.DomainEvents.Any(e => e.GetType().Name == "ClienteModificado"), Is.True);
        }

        [Test]
        public void AsignarAsync_TipoNoReconocido_LanzaExcepcion()
        {
            var repo = new InMemoryGestionClientesRepository();
            var uow = new InMemoryUnitOfWork();
            var useCase = new AsignarTipoClienteUseCase(repo, uow);

            var cliente = new GestionClientesBC.Domain.Aggregates.Cliente(
                Guid.NewGuid(),
                new DocumentoIdentidad(TipoDocumento.DNI, "12345678"),
                "Cliente Test",
                "cliente@mail.com",
                "999999999",
                "Calle 1",
                TipoCliente.Minorista,
                EstadoCliente.Activo
            );
            repo.AddAsync(cliente).Wait();

            Assert.ThrowsAsync<ValueObjectValidationException>(async () =>
                await useCase.AsignarAsync(cliente.ClienteId, "TipoInexistente", new FakeUserContext()));
        }

        [Test]
        public void AsignarAsync_ClienteNoExiste_LanzaExcepcion()
        {
            var repo = new InMemoryGestionClientesRepository();
            var uow = new InMemoryUnitOfWork();
            var useCase = new AsignarTipoClienteUseCase(repo, uow);

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await useCase.AsignarAsync(Guid.NewGuid(), "Mayorista", new FakeUserContext()));
        }

        [Test]
        public void AsignarAsync_SinPermiso_LanzaExcepcion()
        {
            var repo = new InMemoryGestionClientesRepository();
            var uow = new InMemoryUnitOfWork();
            var useCase = new AsignarTipoClienteUseCase(repo, uow);

            var cliente = new GestionClientesBC.Domain.Aggregates.Cliente(
                Guid.NewGuid(),
                new DocumentoIdentidad(TipoDocumento.DNI, "12345678"),
                "Cliente Test",
                "cliente@mail.com",
                "999999999",
                "Calle 1",
                TipoCliente.Minorista,
                EstadoCliente.Activo
            );
            repo.AddAsync(cliente).Wait();

            Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
                await useCase.AsignarAsync(cliente.ClienteId, "Mayorista", new FakeUserSinPermiso()));
        }

        [Test]
        public async Task AsignarAsync_TipoIgual_NoHaceNada()
        {
            var repo = new InMemoryGestionClientesRepository();
            var uow = new InMemoryUnitOfWork();
            var useCase = new AsignarTipoClienteUseCase(repo, uow);

            var cliente = new GestionClientesBC.Domain.Aggregates.Cliente(
                Guid.NewGuid(),
                new DocumentoIdentidad(TipoDocumento.DNI, "12345678"),
                "Cliente Test",
                "cliente@mail.com",
                "999999999",
                "Calle 1",
                TipoCliente.Minorista,
                EstadoCliente.Activo
            );
            await repo.AddAsync(cliente);

            await useCase.AsignarAsync(cliente.ClienteId, "Minorista", new FakeUserContext());

            var actualizado = await repo.GetByIdAsync(cliente.ClienteId);
            Assert.That(actualizado, Is.Not.Null);
            Assert.That(actualizado.TipoCliente, Is.EqualTo(TipoCliente.Minorista));
            Assert.That(actualizado.DomainEvents.Any(e => e.GetType().Name == "ClienteModificado"), Is.False);
        }
    }
}