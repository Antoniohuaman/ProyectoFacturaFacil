using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GestionClientesBC.Application.Clientes.Habilitar;
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.Repositories;
using GestionClientesBC.Domain.ValueObjects;
using GestionClientesBC.Domain.Events;
using Moq;
using NUnit.Framework;
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace GestionClientesBC.Tests.Application.Clientes
{
    [TestFixture]
    public class HabilitarClienteUseCaseTests
    {
        private static EmpresaId EmpresaDemo() => EmpresaId.From("EMPRESA-TEST-001");
        private static EmpresaId OtraEmpresa()  => EmpresaId.From("EMPRESA-OTRA");

        private static DocumentoIdentidad RUC(string v) => DocumentoIdentidad.Crear(TipoDocumento.Ruc, v);
        private static DocumentoIdentidad DNI(string v) => DocumentoIdentidad.Crear(TipoDocumento.Dni, v);
        private static SharedKernel.ValueObjects.RazonSocial RS(string v) => SharedKernel.ValueObjects.RazonSocial.Crear(v);
        private static SharedKernel.ValueObjects.NombrePersona NP(string nombre, string apellidos) => SharedKernel.ValueObjects.NombrePersona.Crear(nombre, apellidos);

        private static HabilitarClienteUseCase SUT(
            out Mock<IClienteRepository> repo,
            out Mock<IUnitOfWork> uow,
            out Mock<ITenantContext> tenant)
        {
            repo = new Mock<IClienteRepository>(MockBehavior.Strict);
            uow  = new Mock<IUnitOfWork>(MockBehavior.Strict);
            tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaDemo());
            uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            return new HabilitarClienteUseCase(repo.Object, uow.Object, tenant.Object);
        }

        private static Cliente ClienteRucInhabilitado()
        {
            // Cliente inicialmente INHABILITADO
            var c = new Cliente(
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
                estado: EstadoCliente.Inhabilitado);

            // (Opcional) ya tenía una deshabilitación previa
            c.Deshabilitar("Mora", new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc));
            return c;
        }

        private static Cliente ClienteDniHabilitado()
        {
            // Cliente ya HABILITADO
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
        public async Task Habilitar_Cliente_Inhabilitado_Exitoso_RegistraEvento()
        {
            var sut = SUT(out var repo, out var uow, out var tenant);
            var existente = ClienteRucInhabilitado();

            repo.Setup(r => r.GetByIdAsync(EmpresaDemo(), existente.ClienteId)).ReturnsAsync(existente);
            repo.Setup(r => r.UpdateAsync(existente)).Returns(Task.CompletedTask);

            var input = new HabilitarClienteInputDto { ClienteId = existente.ClienteId };
            var beforeEvents = existente.DomainEvents.Count;

            var outDto = await sut.Handle(input);

            // Estado en agregado
            Assert.That(existente.Estado, Is.EqualTo(EstadoCliente.Habilitado));
            Assert.That(existente.DomainEvents.Count, Is.GreaterThan(beforeEvents));
            Assert.That(existente.DomainEvents.OfType<ClienteHabilitado>().Any(), Is.True);

            // Salida
            Assert.That(outDto.Habilitado, Is.True);
            Assert.That(outDto.EmpresaId, Is.EqualTo(EmpresaDemo().Value));
            Assert.That(outDto.EstadoCodigo, Is.EqualTo(EstadoCliente.Habilitado.Codigo)); // "HAB"
            Assert.That(outDto.TipoDocumento, Is.EqualTo(TipoDocumento.Ruc.ToString()));
            Assert.That(outDto.NumeroDocumento, Is.EqualTo("20661287099"));
            Assert.That(outDto.FechaHabilitacionUtc.Kind, Is.EqualTo(DateTimeKind.Utc));

            // Persistencia
            repo.Verify(r => r.UpdateAsync(existente), Times.Once);
            uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void Habilitar_Cliente_YaHabilitado_Lanza_Regla()
        {
            var sut = SUT(out var repo, out var uow, out var tenant);
            var existente = ClienteDniHabilitado();

            repo.Setup(r => r.GetByIdAsync(EmpresaDemo(), existente.ClienteId)).ReturnsAsync(existente);

            var input = new HabilitarClienteInputDto { ClienteId = existente.ClienteId };

            Assert.That(async () => await sut.Handle(input),
                Throws.TypeOf<BusinessRuleException>()
                      .With.Message.Contains("ya está habilitado"));

            // No se persiste
            repo.Verify(r => r.UpdateAsync(It.IsAny<Cliente>()), Times.Never);
            uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public void Habilitar_De_Otra_Empresa_Lanza_NotFound()
        {
            var sut = SUT(out var repo, out var uow, out var tenant);

            var otro = new Cliente(
                clienteId: Guid.NewGuid(),
                empresaId: OtraEmpresa(),
                documento: RUC("20600893409"),
                razonSocial: RS("OTRA S.A."),
                nombres: null,
                correo: null,
                telefono: null,
                domicilioFiscal: null,
                tipoCliente: TipoCliente.Cliente,
                rolCliente: null,
                estado: EstadoCliente.Inhabilitado);

            repo.Setup(r => r.GetByIdAsync(OtraEmpresa(), otro.ClienteId)).ReturnsAsync(otro);
            // Setup para simular que el cliente no existe en la empresa del tenant
            repo.Setup(r => r.GetByIdAsync(EmpresaDemo(), otro.ClienteId)).ReturnsAsync((Cliente?)null);

            var input = new HabilitarClienteInputDto { ClienteId = otro.ClienteId };

            Assert.That(async () => await sut.Handle(input),
                Throws.TypeOf<NotFoundException>());
        }

        [Test]
        public void Habilitar_NoExiste_Lanza_NotFound()
        {
            var sut = SUT(out var repo, out var uow, out var tenant);

            repo.Setup(r => r.GetByIdAsync(It.IsAny<EmpresaId>(), It.IsAny<Guid>())).ReturnsAsync((Cliente?)null);

            var input = new HabilitarClienteInputDto { ClienteId = Guid.NewGuid() };

            Assert.That(async () => await sut.Handle(input),
                Throws.TypeOf<NotFoundException>());
        }

        [Test]
        public void Habilitar_ClienteId_Vacio_Lanza_Regla()
        {
            var sut = SUT(out var repo, out var uow, out var tenant);

            var input = new HabilitarClienteInputDto { ClienteId = Guid.Empty };

            Assert.That(async () => await sut.Handle(input),
                Throws.TypeOf<BusinessRuleException>()
                      .With.Message.Contains("ClienteId no puede ser vacío"));
        }
    }
}
