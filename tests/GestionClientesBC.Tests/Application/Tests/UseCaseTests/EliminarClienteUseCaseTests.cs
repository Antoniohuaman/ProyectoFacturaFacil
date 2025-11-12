using GestionClientesBC.Application.Interfaces; // IUnitOfWork
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GestionClientesBC.Application.Clientes.Eliminar;
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.Repositories;
using GestionClientesBC.Domain.ValueObjects;
using Moq;
using NUnit.Framework;
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace GestionClientesBC.Tests.Application.Clientes
{
    [TestFixture]
    public class EliminarClienteUseCaseTests
    {
        private static EmpresaId EmpresaDemo() => EmpresaId.From("EMPRESA-TEST-001");
        private static DocumentoIdentidad RUC(string v) => DocumentoIdentidad.Crear(TipoDocumento.Ruc, v);
        private static DocumentoIdentidad DNI(string v) => DocumentoIdentidad.Crear(TipoDocumento.Dni, v);
        private static SharedKernel.ValueObjects.RazonSocial RS(string s) => SharedKernel.ValueObjects.RazonSocial.Crear(s);
    private static SharedKernel.ValueObjects.NombrePersona NP(string nombre, string apellidos) => SharedKernel.ValueObjects.NombrePersona.Crear(nombre, apellidos);

        private static EliminarClienteUseCase SUT(
            out Mock<IClienteRepository> repo,
            out Mock<IUnitOfWork> uow,
            out Mock<ITenantContext> tenant)
        {
            repo = new Mock<IClienteRepository>(MockBehavior.Strict);
            uow  = new Mock<IUnitOfWork>(MockBehavior.Strict);
            tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaDemo());
            uow.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            return new EliminarClienteUseCase(repo.Object, uow.Object, tenant.Object);
        }

        private static Cliente NuevoClienteRuc()
        {
            return new Cliente(
                clienteId: Guid.NewGuid(),
                empresaId: EmpresaDemo(),
                documento: RUC("20661287099"),
                razonSocial: RS("ACME S.A.C."),
                nombres: null,
                correo: null,
                telefono: null,
                domicilioFiscal: null,
                tipoCliente: TipoCliente.Cliente,
                rolCliente: null,
                estado: EstadoCliente.Habilitado);
        }

        private static Cliente NuevoClienteDni()
        {
            return new Cliente(
                clienteId: Guid.NewGuid(),
                empresaId: EmpresaDemo(),
                documento: DNI("12345678"),
                razonSocial: null,
                nombres: NP("Juan", "Pérez"),
                correo: null,
                telefono: null,
                domicilioFiscal: null,
                tipoCliente: TipoCliente.Cliente,
                rolCliente: null,
                estado: EstadoCliente.Habilitado);
        }

        [Test]
        public async Task Eliminar_Cliente_RUC_Exitoso_RegistraEvento_Y_Elimina()
        {
            var sut = SUT(out var repo, out var uow, out var tenant);
            var existente = NuevoClienteRuc();

            repo.Setup(r => r.GetByIdAsync(EmpresaDemo(), existente.ClienteId)).ReturnsAsync(existente);
            repo.Setup(r => r.DeleteAsync(EmpresaDemo(), existente.ClienteId)).Returns(Task.CompletedTask);

            var input = new EliminarClienteInputDto { ClienteId = existente.ClienteId };

            var beforeEvents = existente.DomainEvents.Count;
            var result = await sut.Handle(input);

            // Verificaciones de salida
            Assert.That(result.Eliminado, Is.True);
            Assert.That(result.ClienteId, Is.EqualTo(existente.ClienteId));
            Assert.That(result.EmpresaId, Is.EqualTo(EmpresaDemo().Value));
            Assert.That(result.TipoDocumento, Is.EqualTo(TipoDocumento.Ruc.ToString()));
            Assert.That(result.NumeroDocumento, Is.EqualTo("20661287099"));
            Assert.That(result.FechaEliminacionUtc, Is.Not.EqualTo(default(DateTime)));

            // Verificación de que se registró el evento de eliminación
            Assert.That(existente.DomainEvents.Count, Is.GreaterThan(beforeEvents));
            Assert.That(existente.DomainEvents.Any(e => e.GetType().Name == "ClienteEliminado"), Is.True);

            repo.Verify(r => r.DeleteAsync(EmpresaDemo(), existente.ClienteId), Times.Once);
            uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Eliminar_Cliente_DNI_Exitoso_Sin_Restricciones()
        {
            var sut = SUT(out var repo, out var uow, out var tenant);
            var existente = NuevoClienteDni();

            repo.Setup(r => r.GetByIdAsync(EmpresaDemo(), existente.ClienteId)).ReturnsAsync(existente);
            repo.Setup(r => r.DeleteAsync(EmpresaDemo(), existente.ClienteId)).Returns(Task.CompletedTask);

            var input = new EliminarClienteInputDto { ClienteId = existente.ClienteId };
            var result = await sut.Handle(input);

            Assert.That(result.Eliminado, Is.True);
            Assert.That(result.TipoDocumento, Is.EqualTo(TipoDocumento.Dni.ToString()));
            Assert.That(result.NumeroDocumento, Is.EqualTo("12345678"));

            repo.Verify(r => r.DeleteAsync(EmpresaDemo(), existente.ClienteId), Times.Once);
            uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void Eliminar_Cliente_NoExiste_Lanza_NotFound()
        {
            var sut = SUT(out var repo, out var uow, out var tenant);

            repo.Setup(r => r.GetByIdAsync(EmpresaDemo(), It.IsAny<Guid>())).ReturnsAsync((Cliente?)null);

            var input = new EliminarClienteInputDto { ClienteId = Guid.NewGuid() };

            Assert.That(async () => await sut.Handle(input),
                Throws.TypeOf<NotFoundException>());
        }

        [Test]
        public void Eliminar_Cliente_De_Otra_Empresa_Lanza_NotFound()
        {
            var sut = SUT(out var repo, out var uow, out var tenant);

            // Cliente con empresa distinta
            var otroTenant = EmpresaId.From("EMPRESA-DISTINTA");
            var clienteOtraEmpresa = new Cliente(
                clienteId: Guid.NewGuid(),
                empresaId: otroTenant,
                documento: RUC("20600893409"),
                razonSocial: RS("OTRA S.A."),
                nombres: null,
                correo: null,
                telefono: null,
                domicilioFiscal: null,
                tipoCliente: TipoCliente.Cliente,
                rolCliente: null,
                estado: EstadoCliente.Habilitado);

            repo.Setup(r => r.GetByIdAsync(EmpresaDemo(), clienteOtraEmpresa.ClienteId)).ReturnsAsync(clienteOtraEmpresa);
            repo.Setup(r => r.DeleteAsync(EmpresaDemo(), clienteOtraEmpresa.ClienteId)).Returns(Task.CompletedTask);

            var input = new EliminarClienteInputDto { ClienteId = clienteOtraEmpresa.ClienteId };

            Assert.That(async () => await sut.Handle(input),
                Throws.TypeOf<NotFoundException>());
        }

        [Test]
        public void Eliminar_No_Llama_Update_Solo_Delete()
        {
            var sut = SUT(out var repo, out var uow, out var tenant);
            var existente = NuevoClienteRuc();

            repo.Setup(r => r.GetByIdAsync(EmpresaDemo(), existente.ClienteId)).ReturnsAsync(existente);
            repo.Setup(r => r.DeleteAsync(EmpresaDemo(), existente.ClienteId)).Returns(Task.CompletedTask);

            var input = new EliminarClienteInputDto { ClienteId = existente.ClienteId };
            Assert.DoesNotThrowAsync(async () => await sut.Handle(input));

            // Asegura que no se intentó un Update
            repo.Verify(r => r.UpdateAsync(It.IsAny<Cliente>(), It.IsAny<int>()), Times.Never);
            repo.Verify(r => r.DeleteAsync(EmpresaDemo(), existente.ClienteId), Times.Once);
        }
    }
}
