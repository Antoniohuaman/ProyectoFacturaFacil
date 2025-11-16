using GestionClientesBC.Application.Interfaces; // IUnitOfWork
using System;
using System.Threading;
using System.Threading.Tasks;
using GestionClientesBC.Application.Clientes.Adjuntos.Eliminar;
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.Entities;
using GestionClientesBC.Domain.Repositories;
using GestionClientesBC.Domain.ValueObjects;
using Moq;
using NUnit.Framework;
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace GestionClientesBC.Tests.Application.Clientes.Adjuntos
{
    [TestFixture]
    public class EliminarAdjuntoClienteUseCaseTests
    {
        private static EmpresaId EmpresaDemo() => EmpresaId.From("EMPRESA-TEST-001");
        private static DocumentoIdentidad Ruc(string value) => DocumentoIdentidad.Crear(TipoDocumento.Ruc, value);
        private static SharedKernel.ValueObjects.RazonSocial RS(string value) => SharedKernel.ValueObjects.RazonSocial.Crear(value);

        private static EliminarAdjuntoClienteUseCase Sut(
            out Mock<IClienteRepository> repo,
            out Mock<IUnitOfWork> uow,
            out Mock<ITenantContext> tenant)
        {
            repo = new Mock<IClienteRepository>(MockBehavior.Strict);
            uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaDemo());
            uow.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            return new EliminarAdjuntoClienteUseCase(repo.Object, uow.Object, tenant.Object);
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
        public async Task EliminarAdjunto_Exitoso_ReduceColeccion()
        {
            var sut = Sut(out var repo, out var uow, out _);
            var cliente = ClienteRucBase();
            var adjunto = new AdjuntoCliente(Guid.NewGuid(), "contrato.pdf", "/files/contrato.pdf", DateTime.UtcNow, "Contrato firmado");
            cliente.AgregarAdjunto(adjunto);

            repo.Setup(r => r.GetByIdAsync(cliente.EmpresaId, cliente.ClienteId)).ReturnsAsync(cliente);
            repo.Setup(r => r.UpdateAsync(cliente, It.IsAny<int>())).Returns(Task.CompletedTask);

            var input = new EliminarAdjuntoClienteInputDto
            {
                ClienteId = cliente.ClienteId,
                AdjuntoId = adjunto.AdjuntoId
            };

            var output = await sut.Handle(input);

            Assert.That(cliente.Adjuntos.Count, Is.EqualTo(0));
            Assert.That(output.TotalAdjuntos, Is.EqualTo(0));
            Assert.That(output.AdjuntoId, Is.EqualTo(adjunto.AdjuntoId));
            Assert.That(output.FechaEventoUtc, Is.Not.Null);

            repo.Verify(r => r.UpdateAsync(cliente, It.IsAny<int>()), Times.Once);
            uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void EliminarAdjunto_NoExiste_Lanza_NotFound()
        {
            var sut = Sut(out var repo, out var uow, out _);
            var cliente = ClienteRucBase();

            repo.Setup(r => r.GetByIdAsync(cliente.EmpresaId, cliente.ClienteId)).ReturnsAsync(cliente);

            var input = new EliminarAdjuntoClienteInputDto
            {
                ClienteId = cliente.ClienteId,
                AdjuntoId = Guid.NewGuid()
            };

            Assert.That(async () => await sut.Handle(input), Throws.TypeOf<NotFoundException>());

            repo.Verify(r => r.UpdateAsync(It.IsAny<Cliente>(), It.IsAny<int>()), Times.Never);
            uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public void EliminarAdjunto_ClienteNoEncontrado_Lanza_NotFound()
        {
            var sut = Sut(out var repo, out _, out _);
            repo.Setup(r => r.GetByIdAsync(It.IsAny<EmpresaId>(), It.IsAny<Guid>())).ReturnsAsync((Cliente?)null);

            var input = new EliminarAdjuntoClienteInputDto
            {
                ClienteId = Guid.NewGuid(),
                AdjuntoId = Guid.NewGuid()
            };

            Assert.That(async () => await sut.Handle(input), Throws.TypeOf<NotFoundException>());
        }
    }
}
