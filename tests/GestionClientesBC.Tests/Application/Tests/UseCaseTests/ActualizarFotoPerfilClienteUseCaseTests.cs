using GestionClientesBC.Application.Interfaces; // IUnitOfWork
using System;
using System.Threading;
using System.Threading.Tasks;
using GestionClientesBC.Application.Clientes.FotoPerfil.Actualizar;
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.Repositories;
using GestionClientesBC.Domain.ValueObjects;
using Moq;
using NUnit.Framework;
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace GestionClientesBC.Tests.Application.Clientes.FotoPerfil
{
    [TestFixture]
    public class ActualizarFotoPerfilClienteUseCaseTests
    {
        private static EmpresaId EmpresaDemo() => EmpresaId.From("EMPRESA-TEST-001");
        private static DocumentoIdentidad Ruc(string value) => DocumentoIdentidad.Crear(TipoDocumento.Ruc, value);
        private static SharedKernel.ValueObjects.RazonSocial RS(string value) => SharedKernel.ValueObjects.RazonSocial.Crear(value);

        private static ActualizarFotoPerfilClienteUseCase Sut(
            out Mock<IClienteRepository> repo,
            out Mock<IUnitOfWork> uow,
            out Mock<ITenantContext> tenant)
        {
            repo = new Mock<IClienteRepository>(MockBehavior.Strict);
            uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaDemo());
            uow.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            return new ActualizarFotoPerfilClienteUseCase(repo.Object, uow.Object, tenant.Object);
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
        public async Task ActualizarFotoPerfil_Exitoso_MapeaSalida()
        {
            var sut = Sut(out var repo, out var uow, out _);
            var cliente = ClienteRucBase();

            repo.Setup(r => r.GetByIdAsync(cliente.EmpresaId, cliente.ClienteId)).ReturnsAsync(cliente);
            repo.Setup(r => r.UpdateAsync(cliente, It.IsAny<int>())).Returns(Task.CompletedTask);

            var input = new ActualizarFotoPerfilClienteInputDto
            {
                ClienteId = cliente.ClienteId,
                NombreArchivo = "avatar.png",
                UrlPublica = "https://cdn.test/avatar.png"
            };

            var output = await sut.Handle(input);

            Assert.That(cliente.FotoPerfil, Is.Not.Null);
            Assert.That(cliente.FotoPerfil!.NombreArchivo, Is.EqualTo("avatar.png"));
            Assert.That(cliente.FotoPerfil.UrlPublica, Is.EqualTo("https://cdn.test/avatar.png"));

            Assert.That(output.ClienteId, Is.EqualTo(cliente.ClienteId));
            Assert.That(output.EmpresaId, Is.EqualTo(EmpresaDemo().Value));
            Assert.That(output.TieneFoto, Is.True);
            Assert.That(output.NombreArchivo, Is.EqualTo("avatar.png"));
            Assert.That(output.UrlPublica, Is.EqualTo("https://cdn.test/avatar.png"));

            repo.Verify(r => r.UpdateAsync(cliente, It.IsAny<int>()), Times.Once);
            uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task ActualizarFotoPerfil_SinDatos_RemueveFoto()
        {
            var sut = Sut(out var repo, out var uow, out _);
            var cliente = ClienteRucBase();
            cliente.ActualizarFotoPerfil(FotoPerfilCliente.Create("old.png", "https://cdn.test/old.png"));

            repo.Setup(r => r.GetByIdAsync(cliente.EmpresaId, cliente.ClienteId)).ReturnsAsync(cliente);
            repo.Setup(r => r.UpdateAsync(cliente, It.IsAny<int>())).Returns(Task.CompletedTask);

            var input = new ActualizarFotoPerfilClienteInputDto
            {
                ClienteId = cliente.ClienteId,
                NombreArchivo = null,
                UrlPublica = null
            };

            var output = await sut.Handle(input);

            Assert.That(cliente.FotoPerfil, Is.Null);
            Assert.That(output.TieneFoto, Is.False);
            Assert.That(output.NombreArchivo, Is.Null);
            Assert.That(output.UrlPublica, Is.Null);

            repo.Verify(r => r.UpdateAsync(cliente, It.IsAny<int>()), Times.Once);
            uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void ActualizarFotoPerfil_ClienteNoEncontrado_Lanza_NotFound()
        {
            var sut = Sut(out var repo, out _, out _);
            repo.Setup(r => r.GetByIdAsync(It.IsAny<EmpresaId>(), It.IsAny<Guid>())).ReturnsAsync((Cliente?)null);

            var input = new ActualizarFotoPerfilClienteInputDto
            {
                ClienteId = Guid.NewGuid(),
                NombreArchivo = "avatar.png"
            };

            Assert.That(async () => await sut.Handle(input), Throws.TypeOf<NotFoundException>());
        }
    }
}
