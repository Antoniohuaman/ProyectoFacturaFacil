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
    public class DeshabilitarClienteUseCaseTests
    {
        private class FakeUserContext : IUserContext
        {
            public bool HasPermission(string permission) => permission == "DeshabilitarCliente";
        }

        [Test]
        public async Task DeshabilitarAsync_ClienteActivo_SinPendientes_Deshabilita()
        {
            var repo = new InMemoryGestionClientesRepository();
            var uow = new InMemoryUnitOfWork();
            var useCase = new DeshabilitarClienteUseCase(repo, uow);

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

            await useCase.DeshabilitarAsync(cliente.ClienteId, "Dejó de operar", new FakeUserContext());

            var actualizado = await repo.GetByIdAsync(cliente.ClienteId);
            Assert.That(actualizado, Is.Not.Null);
            Assert.That(actualizado.Estado, Is.EqualTo(EstadoCliente.Inactivo));
            Assert.That(actualizado.MotivoDeshabilitacion, Is.EqualTo("Dejó de operar"));
            Assert.That(actualizado.DomainEvents.Any(e => e.GetType().Name == "ClienteDeshabilitado"), Is.True);
        }

        [Test]
        public void DeshabilitarAsync_ClienteYaInactivo_LanzaExcepcion()
        {
            var repo = new InMemoryGestionClientesRepository();
            var uow = new InMemoryUnitOfWork();
            var useCase = new DeshabilitarClienteUseCase(repo, uow);

            var cliente = new GestionClientesBC.Domain.Aggregates.Cliente(
                Guid.NewGuid(),
                new DocumentoIdentidad(TipoDocumento.DNI, "12345678"),
                "Cliente Test",
                "cliente@mail.com",
                "999999999",
                "Calle 1",
                TipoCliente.Minorista,
                EstadoCliente.Inactivo
            );
            repo.AddAsync(cliente).Wait();

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await useCase.DeshabilitarAsync(cliente.ClienteId, null, new FakeUserContext()));
        }

        [Test]
        public void DeshabilitarAsync_ClienteConPendientes_LanzaExcepcion()
        {
            // Simula un cliente con operaciones pendientes
            // Debes adaptar esto según tu modelo de OperacionCliente
        }
    }
}