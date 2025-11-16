using GestionClientesBC.Application.Interfaces; // IUnitOfWork
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GestionClientesBC.Application.Clientes.Direccion.Actualizar;
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.Repositories;
using GestionClientesBC.Domain.Events;
using GestionClientesBC.Domain.ValueObjects;
using Moq;
using NUnit.Framework;
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace GestionClientesBC.Tests.Application.Clientes.Direccion
{
    [TestFixture]
    public class ActualizarDireccionClienteUseCaseTests
    {
        private static EmpresaId EmpresaDemo() => EmpresaId.From("EMPRESA-TEST-001");
        private static DocumentoIdentidad Ruc(string value) => DocumentoIdentidad.Crear(TipoDocumento.Ruc, value);
        private static SharedKernel.ValueObjects.RazonSocial RS(string value) => SharedKernel.ValueObjects.RazonSocial.Crear(value);

        private static ActualizarDireccionClienteUseCase Sut(
            out Mock<IClienteRepository> repo,
            out Mock<IUnitOfWork> uow,
            out Mock<ITenantContext> tenant)
        {
            repo = new Mock<IClienteRepository>(MockBehavior.Strict);
            uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaDemo());
            uow.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            return new ActualizarDireccionClienteUseCase(repo.Object, uow.Object, tenant.Object);
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
        public async Task ActualizarDireccion_Exitoso_PersistenciaYSalida()
        {
            var sut = Sut(out var repo, out var uow, out _);
            var cliente = ClienteRucBase();

            repo.Setup(r => r.GetByIdAsync(cliente.EmpresaId, cliente.ClienteId)).ReturnsAsync(cliente);
            repo.Setup(r => r.UpdateAsync(cliente, It.IsAny<int>())).Returns(Task.CompletedTask);

            var input = new ActualizarDireccionClienteInputDto
            {
                ClienteId = cliente.ClienteId,
                DireccionLinea = "  Av. Los Cedros 123  ",
                Ubigeo = "150101",
                Departamento = "Lima",
                Provincia = "Lima",
                Distrito = "Lima",
                AddressTypeCode = "0000"
            };

            var output = await sut.Handle(input);

            Assert.That(cliente.DomicilioFiscal, Is.Not.Null);
            Assert.That(cliente.DomicilioFiscal!.Linea, Is.EqualTo("Av. Los Cedros 123"));
            Assert.That(cliente.DomicilioFiscal.Ubigeo, Is.EqualTo("150101"));
            Assert.That(cliente.DomicilioFiscal.Distrito, Is.EqualTo("Lima"));
            Assert.That(cliente.DomicilioFiscal.AddressTypeCode, Is.EqualTo("0000"));

            Assert.That(output.ClienteId, Is.EqualTo(cliente.ClienteId));
            Assert.That(output.EmpresaId, Is.EqualTo(EmpresaDemo().Value));
            Assert.That(output.Ubigeo, Is.EqualTo("150101"));
            Assert.That(output.Version, Is.EqualTo(cliente.Version));
            Assert.That(output.DireccionFormateada, Does.Contain("Av. Los Cedros 123"));
            Assert.That(cliente.DomainEvents.OfType<DireccionClienteActualizada>().Any(), Is.True);

            repo.Verify(r => r.UpdateAsync(cliente, It.IsAny<int>()), Times.Once);
            uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void ActualizarDireccion_SinDatos_Lanza_Regla()
        {
            var sut = Sut(out var repo, out var uow, out _);
            var cliente = ClienteRucBase();
            repo.Setup(r => r.GetByIdAsync(cliente.EmpresaId, cliente.ClienteId)).ReturnsAsync(cliente);

            var input = new ActualizarDireccionClienteInputDto
            {
                ClienteId = cliente.ClienteId,
                PaisCodigoIso = "CL"
            };

            Assert.That(async () => await sut.Handle(input),
                Throws.TypeOf<BusinessRuleException>());

            repo.Verify(r => r.UpdateAsync(It.IsAny<Cliente>(), It.IsAny<int>()), Times.Never);
            uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public void ActualizarDireccion_ClienteDeOtraEmpresa_Lanza_NotFound()
        {
            var sut = Sut(out var repo, out _, out _);

            var clienteOtraEmpresa = new Cliente(
                Guid.NewGuid(),
                empresaId: EmpresaId.From("OTRA-EMP"),
                documento: Ruc("20600893409"),
                razonSocial: RS("OTRA"),
                nombres: null,
                correo: null,
                telefono: null,
                domicilioFiscal: null,
                tipoCliente: TipoCliente.Cliente,
                rolCliente: null,
                estado: EstadoCliente.Habilitado);

            repo.Setup(r => r.GetByIdAsync(It.IsAny<EmpresaId>(), clienteOtraEmpresa.ClienteId))
                .ReturnsAsync(clienteOtraEmpresa);

            var input = new ActualizarDireccionClienteInputDto
            {
                ClienteId = clienteOtraEmpresa.ClienteId,
                DireccionLinea = "Av. Uno",
                Ubigeo = "150101",
                Departamento = "Lima",
                Provincia = "Lima",
                Distrito = "Lima"
            };

            Assert.That(async () => await sut.Handle(input), Throws.TypeOf<NotFoundException>());
        }
    }
}
