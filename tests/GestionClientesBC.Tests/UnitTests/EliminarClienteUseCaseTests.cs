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
    public class EliminarClienteUseCaseTests
    {
        private class FakeUserContext : IUserContext
        {
            public bool HasPermission(string permission) => permission == "EliminarCliente";
        }

        private class FakeUserSinPermiso : IUserContext
        {
            public bool HasPermission(string permission) => false;
        }

        [Test]
        public async Task EliminarAsync_ClienteSinOperaciones_EliminaCorrectamente()
        {
            var repo = new InMemoryGestionClientesRepository();
            var uow = new InMemoryUnitOfWork();
            var useCase = new EliminarClienteUseCase(repo, uow);

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

            await useCase.EliminarAsync(cliente.ClienteId, new FakeUserContext());

            var eliminado = await repo.GetByIdAsync(cliente.ClienteId);
            Assert.That(eliminado, Is.Null);
        }

        [Test]
        public void EliminarAsync_SinPermiso_LanzaExcepcion()
        {
            var repo = new InMemoryGestionClientesRepository();
            var uow = new InMemoryUnitOfWork();
            var useCase = new EliminarClienteUseCase(repo, uow);

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
                await useCase.EliminarAsync(cliente.ClienteId, new FakeUserSinPermiso()));
        }

        [Test]
        public void EliminarAsync_ClienteNoExiste_LanzaExcepcion()
        {
            var repo = new InMemoryGestionClientesRepository();
            var uow = new InMemoryUnitOfWork();
            var useCase = new EliminarClienteUseCase(repo, uow);

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await useCase.EliminarAsync(Guid.NewGuid(), new FakeUserContext()));
        }

        [Test]
        public void EliminarAsync_ClienteConOperacionesCriticas_LanzaExcepcion()
        {
            var repo = new InMemoryGestionClientesRepository();
            var uow = new InMemoryUnitOfWork();
            var useCase = new EliminarClienteUseCase(repo, uow);

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
            // Simula una operación crítica para retención
            var operacionCritica = new GestionClientesBC.Domain.Entities.OperacionCliente(
                Guid.NewGuid(),
                TipoOperacion.BoletaEmitida, // Usa el valor correcto de tu enum
                new MontoOperacion(100),
                new ReferenciaId("REF123"),
                DateTime.UtcNow
            )
            {
                EstaPendiente = false,
                EsCriticaParaRetencion = true
            };

            var lista = cliente.GetType().GetField("_operaciones", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(cliente) as System.Collections.IList;
            lista?.Add(operacionCritica);

            repo.AddAsync(cliente).Wait();

            Assert.ThrowsAsync<ClienteConOperacionesRegistradasException>(async () =>
                await useCase.EliminarAsync(cliente.ClienteId, new FakeUserContext()));
        }
    }
}