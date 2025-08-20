using System;
using System.Threading.Tasks;
using GestionClientesBC.Application.DTOs;
using GestionClientesBC.Application.UseCases;
using GestionClientesBC.Adapters.Output.Persistence.InMemory;
using NUnit.Framework;
using GestionClientesBC.Domain.ValueObjects;
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.Entities;
using SharedKernel.ValueObjects;

namespace GestionClientesBC.Tests.UnitTests
{
    public class ActualizarDatosContactoClienteUseCaseTests
    {
        [Test]
        public async Task HandleAsync_CuandoDatosValidos_ActualizaContactoYCommit()
        {
            // Arrange
            var repo = new InMemoryGestionClientesRepository();
            var uow = new InMemoryUnitOfWork();

            var direccionVO = DireccionPostal.FromPeru("Av. Siempre Viva 123");
            var cliente = new GestionClientesBC.Domain.Aggregates.Cliente(
                Guid.NewGuid(), // <-- Este es el clienteId
                new DocumentoIdentidad(TipoDocumento.DNI, "12345678"),
                "Juan Perez",
                "juan@correo.com",
                "999999999",
                direccionVO,
                TipoCliente.Mayorista,
                EstadoCliente.Activo
            );
            await repo.AddAsync(cliente);

            var useCase = new ActualizarDatosContactoClienteUseCase(repo, uow);

            var dto = new ActualizarDatosContactoClienteDto
        {
        ClienteId = cliente.ClienteId,
        NuevoCorreo = "nuevo@correo.com",
        NuevoCelular = "888888888",
        UsuarioId = "admin"
    };

// Act
        await useCase.HandleAsync(dto);

// Assert
        Assert.That(cliente.Correo, Is.EqualTo(dto.NuevoCorreo));
        Assert.That(cliente.Celular, Is.EqualTo(dto.NuevoCelular));
        Assert.That(uow.WasCommitted, Is.True);
        }
    }
}