using System;
using System.Linq;
using System.Threading.Tasks;
using GestionClientesBC.Application.DTOs;
using GestionClientesBC.Application.UseCases;
using GestionClientesBC.Adapters.Output.Persistence.InMemory;
using GestionClientesBC.Domain.ValueObjects;
using GestionClientesBC.Application.Interfaces;
using NUnit.Framework;

namespace GestionClientesBC.Tests.UnitTests.UseCases
{
    public class EditarClienteUseCaseTests
    {
        private class FakeUserContext : IUserContext
        {
            public bool HasPermission(string permission) => permission == "EditarCliente";
        }

        [Test]
        public async Task EditarAsync_DatosValidos_ModificaCliente()
        {
            var repo = new InMemoryGestionClientesRepository();
            var uow = new InMemoryUnitOfWork();
            var useCase = new EditarClienteUseCase(repo, uow);

            // Pre-crea cliente
            var cliente = new GestionClientesBC.Domain.Aggregates.Cliente(
                Guid.NewGuid(),
                new DocumentoIdentidad(TipoDocumento.DNI, "12345678"),
                "Cliente Original",
                "original@mail.com",
                "999999999",
                "Calle 1",
                TipoCliente.Minorista,
                EstadoCliente.Activo
            );
            await repo.AddAsync(cliente);

            var fechaAntes = cliente.FechaRegistro;

            var dto = new EditarClienteDto
            {
                ClienteId = cliente.ClienteId,
                Correo = "nuevo@mail.com",
                DireccionPostal = "Nueva Calle 2",
                TipoCliente = "Mayorista"
            };

            await useCase.EditarAsync(dto, new FakeUserContext());

            var actualizado = await repo.GetByIdAsync(cliente.ClienteId);
            Assert.That(actualizado, Is.Not.Null);
            Assert.That(actualizado.Correo, Is.EqualTo("nuevo@mail.com"));
            Assert.That(actualizado.DireccionPostal, Is.EqualTo("Nueva Calle 2"));
            Assert.That(actualizado.TipoCliente, Is.EqualTo(TipoCliente.Mayorista));
            Assert.That(actualizado.RazonSocialONombres, Is.EqualTo("Cliente Original")); // No cambió
            Assert.That(actualizado.Celular, Is.EqualTo("999999999")); // No cambió
            Assert.That(actualizado.FechaRegistro, Is.GreaterThan(fechaAntes)); // Fecha actualizada

            // Opcional: verifica que se registró el evento de modificación
            Assert.That(actualizado.DomainEvents.Any(e => e.GetType().Name == "ClienteModificado"), Is.True);
        }
    }
}