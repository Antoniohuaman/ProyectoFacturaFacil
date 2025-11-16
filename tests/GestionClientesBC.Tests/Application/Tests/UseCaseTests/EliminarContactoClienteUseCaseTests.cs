using GestionClientesBC.Application.Interfaces; // IUnitOfWork
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GestionClientesBC.Application.Clientes.Contactos.Eliminar;
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.Entities;
using GestionClientesBC.Domain.Repositories;
using GestionClientesBC.Domain.ValueObjects;
using Moq;
using NUnit.Framework;
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace GestionClientesBC.Tests.Application.Clientes.Contactos
{
    [TestFixture]
    public class EliminarContactoClienteUseCaseTests
    {
        private static EmpresaId EmpresaDemo() => EmpresaId.From("EMPRESA-TEST-001");
        private static DocumentoIdentidad Ruc(string value) => DocumentoIdentidad.Crear(TipoDocumento.Ruc, value);
        private static SharedKernel.ValueObjects.RazonSocial RS(string value) => SharedKernel.ValueObjects.RazonSocial.Crear(value);
        private static SharedKernel.ValueObjects.NombrePersona NP(string nombres, string apellidos) => SharedKernel.ValueObjects.NombrePersona.Crear(nombres, apellidos);

        private static EliminarContactoClienteUseCase Sut(
            out Mock<IClienteRepository> repo,
            out Mock<IUnitOfWork> uow,
            out Mock<ITenantContext> tenant)
        {
            repo = new Mock<IClienteRepository>(MockBehavior.Strict);
            uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaDemo());
            uow.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            return new EliminarContactoClienteUseCase(repo.Object, uow.Object, tenant.Object);
        }

        private static Cliente ClienteRucBase()
        {
            return new Cliente(
                clienteId: Guid.NewGuid(),
                empresaId: EmpresaDemo(),
                documento: Ruc("20661287099"),
                razonSocial: RS("ACME S.A.C."),
                nombres: null,
                correo: SharedKernel.ValueObjects.Email.Create("ventas@acme.com"),
                telefono: SharedKernel.ValueObjects.Telefono.FromTexto("+51 999 888 777"),
                domicilioFiscal: null,
                tipoCliente: TipoCliente.ClienteProveedor,
                rolCliente: RolCliente.Mayorista,
                estado: EstadoCliente.Habilitado);
        }

        [Test]
        public async Task EliminarContacto_Exitoso_RegistraEvento()
        {
            var sut = Sut(out var repo, out var uow, out _);
            var cliente = ClienteRucBase();

            var contacto = new ContactoCliente(
                contactoId: Guid.NewGuid(),
                nombreContacto: NP("Ana", "Perez"),
                documentoIdentidad: DocumentoIdentidad.Crear(TipoDocumento.Dni, "12345678"),
                emails: new List<SharedKernel.ValueObjects.Email> { SharedKernel.ValueObjects.Email.Create("ana@acme.com") },
                telefonos: new List<SharedKernel.ValueObjects.Telefono> { SharedKernel.ValueObjects.Telefono.FromTexto("+51 999 111 222") },
                direccion: "Calle 1");

            cliente.AgregarContacto(contacto);

            repo.Setup(r => r.GetByIdAsync(cliente.EmpresaId, cliente.ClienteId)).ReturnsAsync(cliente);
            repo.Setup(r => r.UpdateAsync(cliente, It.IsAny<int>())).Returns(Task.CompletedTask);

            var input = new EliminarContactoClienteInputDto
            {
                ClienteId = cliente.ClienteId,
                ContactoId = contacto.ContactoId
            };

            var output = await sut.Handle(input);

            Assert.That(cliente.Contactos.Count, Is.EqualTo(0));
            Assert.That(output.TotalContactos, Is.EqualTo(0));
            Assert.That(output.ContactoId, Is.EqualTo(contacto.ContactoId));
            Assert.That(output.FechaEventoUtc, Is.Not.Null);

            repo.Verify(r => r.UpdateAsync(cliente, It.IsAny<int>()), Times.Once);
            uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void EliminarContacto_NoExiste_Lanza_Regla()
        {
            var sut = Sut(out var repo, out var uow, out _);
            var cliente = ClienteRucBase();

            repo.Setup(r => r.GetByIdAsync(cliente.EmpresaId, cliente.ClienteId)).ReturnsAsync(cliente);

            var input = new EliminarContactoClienteInputDto
            {
                ClienteId = cliente.ClienteId,
                ContactoId = Guid.NewGuid()
            };

            Assert.That(async () => await sut.Handle(input),
                Throws.TypeOf<BusinessRuleException>());

            repo.Verify(r => r.UpdateAsync(It.IsAny<Cliente>(), It.IsAny<int>()), Times.Never);
            uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public void EliminarContacto_ClienteNoEncontrado_Lanza_NotFound()
        {
            var sut = Sut(out var repo, out _, out _);
            repo.Setup(r => r.GetByIdAsync(It.IsAny<EmpresaId>(), It.IsAny<Guid>())).ReturnsAsync((Cliente?)null);

            var input = new EliminarContactoClienteInputDto
            {
                ClienteId = Guid.NewGuid(),
                ContactoId = Guid.NewGuid()
            };

            Assert.That(async () => await sut.Handle(input), Throws.TypeOf<NotFoundException>());
        }
    }
}
