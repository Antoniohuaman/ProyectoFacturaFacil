using GestionClientesBC.Application.Interfaces; // IUnitOfWork
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GestionClientesBC.Application.Clientes.Deshabilitar;
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
    public class DeshabilitarClienteUseCaseTests
    {
        private static EmpresaId EmpresaDemo() => EmpresaId.From("EMPRESA-TEST-001");
        private static EmpresaId OtraEmpresa()  => EmpresaId.From("EMPRESA-OTRA");

        private static DocumentoIdentidad RUC(string v) => DocumentoIdentidad.Crear(TipoDocumento.Ruc, v);
        private static DocumentoIdentidad DNI(string v) => DocumentoIdentidad.Crear(TipoDocumento.Dni, v);
        private static SharedKernel.ValueObjects.RazonSocial RS(string v) => SharedKernel.ValueObjects.RazonSocial.Crear(v);
        private static SharedKernel.ValueObjects.NombrePersona NP(string nombre, string apellidos) => SharedKernel.ValueObjects.NombrePersona.Crear(nombre, apellidos);

        private static DeshabilitarClienteUseCase SUT(
            out Mock<IClienteRepository> repo,
            out Mock<IUnitOfWork> uow,
            out Mock<ITenantContext> tenant)
        {
            repo = new Mock<IClienteRepository>(MockBehavior.Strict);
            uow  = new Mock<IUnitOfWork>(MockBehavior.Strict);
            tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaDemo());
            uow.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            return new DeshabilitarClienteUseCase(repo.Object, uow.Object, tenant.Object);
        }

        private static Cliente ClienteRucHabilitado()
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

    private static Cliente ClienteDniDeshabilitado()
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
        estado: EstadoCliente.Deshabilitado);
        }

        [Test]
        public async Task Deshabilitar_Exitoso_RegistraEvento_Y_Persistencia()
        {
            var sut = SUT(out var repo, out var uow, out var tenant);
            var existente = ClienteRucHabilitado();

            var fechaUtc = new DateTime(2025, 01, 02, 03, 04, 05, DateTimeKind.Utc);
            var motivo = "Solicitado por el usuario";

            repo.Setup(r => r.GetByIdAsync(EmpresaDemo(), existente.ClienteId)).ReturnsAsync(existente);
            repo.Setup(r => r.UpdateAsync(existente, It.IsAny<int>())).Returns(Task.CompletedTask);

            var input = new DeshabilitarClienteInputDto
            {
                ClienteId = existente.ClienteId,
                Motivo = motivo,
                FechaDeshabilitacion = fechaUtc
            };

            var eventosAntes = existente.DomainEvents.Count;
            var outDto = await sut.Handle(input);

            // Estado interno
            Assert.That(existente.Estado, Is.EqualTo(EstadoCliente.Deshabilitado));
            Assert.That(existente.MotivoDeshabilitacion, Is.EqualTo(motivo));
            Assert.That(existente.FechaDeshabilitacion, Is.EqualTo(fechaUtc));
            Assert.That(existente.DomainEvents.Count, Is.GreaterThan(eventosAntes));
            Assert.That(existente.DomainEvents.Any(e => e is GestionClientesBC.Domain.Events.ClienteDeshabilitado), Is.True);

            // Salida
            Assert.That(outDto.Deshabilitado, Is.True);
            Assert.That(outDto.EmpresaId, Is.EqualTo(EmpresaDemo().Value));
            Assert.That(outDto.EstadoCodigo, Is.EqualTo(EstadoCliente.Deshabilitado.Codigo)); // "DES"
            Assert.That(outDto.FechaDeshabilitacionUtc, Is.EqualTo(fechaUtc));
            Assert.That(outDto.MotivoDeshabilitacion, Is.EqualTo(motivo));
            Assert.That(outDto.TipoDocumento, Is.EqualTo(TipoDocumento.Ruc.ToString()));
            Assert.That(outDto.NumeroDocumento, Is.EqualTo("20661287099"));

            // Persistencia
            repo.Verify(r => r.UpdateAsync(existente, It.IsAny<int>()), Times.Once);
            uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void Deshabilitar_YaInhabilitado_NoFalla_Y_EmiteEvento()
        {
            var sut = SUT(out var repo, out var uow, out var tenant);
            var existente = ClienteDniDeshabilitado();

            repo.Setup(r => r.GetByIdAsync(EmpresaDemo(), existente.ClienteId)).ReturnsAsync(existente);
            repo.Setup(r => r.UpdateAsync(existente, It.IsAny<int>())).Returns(Task.CompletedTask);

            var input = new DeshabilitarClienteInputDto
            {
                ClienteId = existente.ClienteId,
                Motivo = "Sin condición",
                FechaDeshabilitacion = new DateTime(2025, 02, 03, 04, 05, 06, DateTimeKind.Utc)
            };

            Assert.DoesNotThrowAsync(async () => await sut.Handle(input));

            Assert.That(existente.Estado, Is.EqualTo(EstadoCliente.Deshabilitado));
            Assert.That(existente.DomainEvents.Any(e => e is GestionClientesBC.Domain.Events.ClienteDeshabilitado), Is.True);

            repo.Verify(r => r.UpdateAsync(existente, It.IsAny<int>()), Times.Once);
            uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void Deshabilitar_De_Otra_Empresa_Lanza_NotFound()
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
                estado: EstadoCliente.Habilitado);

            repo.Setup(r => r.GetByIdAsync(EmpresaDemo(), otro.ClienteId)).ReturnsAsync((Cliente?)null);
            repo.Setup(r => r.GetByIdAsync(OtraEmpresa(), otro.ClienteId)).ReturnsAsync(otro);

            var input = new DeshabilitarClienteInputDto
            {
                ClienteId = otro.ClienteId,
                Motivo = "Fuera de tenant",
                FechaDeshabilitacion = DateTime.UtcNow
            };

            Assert.That(async () => await sut.Handle(input),
                Throws.TypeOf<NotFoundException>());
        }

        [Test]
        public void Deshabilitar_NoExiste_Lanza_NotFound()
        {
            var sut = SUT(out var repo, out var uow, out var tenant);

            repo.Setup(r => r.GetByIdAsync(It.IsAny<EmpresaId>(), It.IsAny<Guid>())).ReturnsAsync((Cliente?)null);

            var input = new DeshabilitarClienteInputDto
            {
                ClienteId = Guid.NewGuid(),
                Motivo = "N/A",
                FechaDeshabilitacion = DateTime.UtcNow
            };

            Assert.That(async () => await sut.Handle(input),
                Throws.TypeOf<NotFoundException>());
        }

        [Test]
        public async Task Deshabilitar_Fecha_Unspecified_Se_Normaliza_A_Utc()
        {
            var sut = SUT(out var repo, out var uow, out var tenant);
            var existente = ClienteRucHabilitado();

            var fechaUnspec = new DateTime(2025, 03, 04, 10, 00, 00, DateTimeKind.Unspecified);

            repo.Setup(r => r.GetByIdAsync(EmpresaDemo(), existente.ClienteId)).ReturnsAsync(existente);
            repo.Setup(r => r.UpdateAsync(existente, It.IsAny<int>())).Returns(Task.CompletedTask);

            var input = new DeshabilitarClienteInputDto
            {
                ClienteId = existente.ClienteId,
                Motivo = "Normalización",
                FechaDeshabilitacion = fechaUnspec
            };

            var outDto = await sut.Handle(input);

            Assert.That(existente.FechaDeshabilitacion.HasValue, Is.True);
            Assert.That(outDto.FechaDeshabilitacionUtc.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(existente.FechaDeshabilitacion!.Value.Kind, Is.EqualTo(DateTimeKind.Utc));

            repo.Verify(r => r.UpdateAsync(existente, It.IsAny<int>()), Times.Once);
            uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void Deshabilitar_ClienteId_Vacio_Lanza_Regla()
        {
            var sut = SUT(out var repo, out var uow, out var tenant);

            var input = new DeshabilitarClienteInputDto
            {
                ClienteId = Guid.Empty
            };

            Assert.That(async () => await sut.Handle(input),
                Throws.TypeOf<BusinessRuleException>()
                      .With.Message.Contains("ClienteId no puede ser vacío"));
        }
    }
}
