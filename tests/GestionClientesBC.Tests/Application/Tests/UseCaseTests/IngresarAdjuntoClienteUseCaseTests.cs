using GestionClientesBC.Application.Interfaces; // IUnitOfWork
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GestionClientesBC.Application.Clientes.Adjuntos.Ingresar;
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.Entities;
using GestionClientesBC.Domain.Events;
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
    public class IngresarAdjuntoClienteUseCaseTests
    {
        private static EmpresaId EmpresaDemo() => EmpresaId.From("EMPRESA-TEST-001");
        private static EmpresaId OtraEmpresa()  => EmpresaId.From("EMPRESA-OTRA");

        private static DocumentoIdentidad RUC(string v) => DocumentoIdentidad.Crear(TipoDocumento.Ruc, v);
        private static DocumentoIdentidad DNI(string v) => DocumentoIdentidad.Crear(TipoDocumento.Dni, v);
        private static SharedKernel.ValueObjects.RazonSocial RS(string v) => SharedKernel.ValueObjects.RazonSocial.Crear(v);
    private static SharedKernel.ValueObjects.NombrePersona NP(string nombre, string apellidos) => SharedKernel.ValueObjects.NombrePersona.Crear(nombre, apellidos);

        private static IngresarAdjuntoClienteUseCase SUT(
            out Mock<IClienteRepository> repo,
            out Mock<IUnitOfWork> uow,
            out Mock<ITenantContext> tenant)
        {
            repo = new Mock<IClienteRepository>(MockBehavior.Strict);
            uow  = new Mock<IUnitOfWork>(MockBehavior.Strict);
            tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaDemo());
            uow.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            return new IngresarAdjuntoClienteUseCase(repo.Object, uow.Object, tenant.Object);
        }

        private static Cliente ClienteRuc()
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

        private static Cliente ClienteDniOtraEmpresa()
        {
            return new Cliente(
                clienteId: Guid.NewGuid(),
                empresaId: OtraEmpresa(),
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
        public async Task IngresarAdjunto_Exitoso_Agrega_Adjunto_Y_EmiteEvento()
        {
            // Arrange
            var sut = SUT(out var repo, out var uow, out var tenant);
            var cliente = ClienteRuc();

            repo.Setup(r => r.GetByIdAsync(EmpresaDemo(), cliente.ClienteId)).ReturnsAsync(cliente);
            repo.Setup(r => r.UpdateAsync(cliente, It.IsAny<int>())).Returns(Task.CompletedTask);

            var fecha = new DateTime(2025, 01, 02, 03, 04, 05, DateTimeKind.Utc);
            var input = new IngresarAdjuntoClienteInputDto
            {
                ClienteId = cliente.ClienteId,
                NombreArchivo = "contrato.pdf",
                Ruta = "/files/contratos/contrato.pdf",
                Comentario = "Contrato firmado",
                FechaSubida = fecha
            };

            var adjuntosAntes = cliente.Adjuntos.Count;

            // Act
            var outDto = await sut.Handle(input);

            // Assert (estado en agregado)
            Assert.That(cliente.Adjuntos.Count, Is.EqualTo(adjuntosAntes + 1));
            var adj = cliente.Adjuntos.Last();
            Assert.That(adj.NombreArchivo, Is.EqualTo("contrato.pdf"));
            Assert.That(adj.Ruta, Is.EqualTo("/files/contratos/contrato.pdf"));
            Assert.That(adj.Comentario, Is.EqualTo("Contrato firmado"));
            Assert.That(adj.FechaSubida, Is.EqualTo(fecha));

            // Evento
            Assert.That(cliente.DomainEvents.OfType<AdjuntoAgregado>().Any(), Is.True);

            // Output
            Assert.That(outDto.ClienteId, Is.EqualTo(cliente.ClienteId));
            Assert.That(outDto.EmpresaId, Is.EqualTo(EmpresaDemo().Value));
            Assert.That(outDto.AdjuntoId, Is.EqualTo(adj.AdjuntoId));
            Assert.That(outDto.NombreArchivo, Is.EqualTo(adj.NombreArchivo));
            Assert.That(outDto.Ruta, Is.EqualTo(adj.Ruta));
            Assert.That(outDto.Comentario, Is.EqualTo(adj.Comentario));
            Assert.That(outDto.FechaSubidaUtc, Is.EqualTo(adj.FechaSubida));
            Assert.That(outDto.TotalAdjuntosCliente, Is.EqualTo(cliente.Adjuntos.Count));
            Assert.That(outDto.FechaEventoUtc.HasValue, Is.True);
            Assert.That(outDto.FechaEventoUtc!.Value.Kind, Is.EqualTo(DateTimeKind.Utc));

            // Persistencia
            repo.Verify(r => r.UpdateAsync(cliente, It.IsAny<int>()), Times.Once);
            uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task IngresarAdjunto_Sin_AdjuntoId_Genera_Nuevo_Id()
        {
            var sut = SUT(out var repo, out var uow, out var tenant);
            var cliente = ClienteRuc();

            repo.Setup(r => r.GetByIdAsync(EmpresaDemo(), cliente.ClienteId)).ReturnsAsync(cliente);
            repo.Setup(r => r.UpdateAsync(cliente, It.IsAny<int>())).Returns(Task.CompletedTask);

            var input = new IngresarAdjuntoClienteInputDto
            {
                ClienteId = cliente.ClienteId,
                AdjuntoId = null, // que lo genere
                NombreArchivo = "foto.png",
                Ruta = "/files/fotos/foto.png",
                Comentario = null,
                FechaSubida = null // que use UtcNow
            };

            var outDto = await sut.Handle(input);

            Assert.That(outDto.AdjuntoId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(cliente.Adjuntos.Any(a => a.AdjuntoId == outDto.AdjuntoId), Is.True);
            Assert.That(outDto.FechaSubidaUtc.Kind, Is.EqualTo(DateTimeKind.Utc));
        }

        [Test]
        public async Task IngresarAdjunto_Fecha_Unspecified_Se_Normaliza_A_Utc()
        {
            var sut = SUT(out var repo, out var uow, out var tenant);
            var cliente = ClienteRuc();

            repo.Setup(r => r.GetByIdAsync(EmpresaDemo(), cliente.ClienteId)).ReturnsAsync(cliente);
            repo.Setup(r => r.UpdateAsync(cliente, It.IsAny<int>())).Returns(Task.CompletedTask);

            var fechaUnspec = new DateTime(2025, 04, 05, 06, 07, 08, DateTimeKind.Unspecified);

            var outDto = await sut.Handle(new IngresarAdjuntoClienteInputDto
            {
                ClienteId = cliente.ClienteId,
                NombreArchivo = "doc.txt",
                Ruta = "/files/documents/doc.txt",
                FechaSubida = fechaUnspec
            });

            Assert.That(outDto.FechaSubidaUtc.Kind, Is.EqualTo(DateTimeKind.Utc));
            var adj = cliente.Adjuntos.Last();
            Assert.That(adj.FechaSubida.Kind, Is.EqualTo(DateTimeKind.Utc));
        }

        [Test]
        public void IngresarAdjunto_Cliente_De_Otra_Empresa_Lanza_NotFound()
        {
            var sut = SUT(out var repo, out var uow, out var tenant);
            var cliente = ClienteDniOtraEmpresa();

            repo.Setup(r => r.GetByIdAsync(OtraEmpresa(), cliente.ClienteId)).ReturnsAsync(cliente);
            // Setup para cuando se busca con la empresa activa y el cliente de otra empresa (debe devolver null)
            repo.Setup(r => r.GetByIdAsync(EmpresaDemo(), cliente.ClienteId)).ReturnsAsync((Cliente?)null);

            var input = new IngresarAdjuntoClienteInputDto
            {
                ClienteId = cliente.ClienteId,
                NombreArchivo = "doc.pdf",
                Ruta = "/files/doc.pdf"
            };

            Assert.That(async () => await sut.Handle(input),
                Throws.TypeOf<NotFoundException>());
        }

        [Test]
        public void IngresarAdjunto_Cliente_No_Existe_Lanza_NotFound()
        {
            var sut = SUT(out var repo, out var uow, out var tenant);

            repo.Setup(r => r.GetByIdAsync(It.IsAny<EmpresaId>(), It.IsAny<Guid>())).ReturnsAsync((Cliente?)null);

            var input = new IngresarAdjuntoClienteInputDto
            {
                ClienteId = Guid.NewGuid(),
                NombreArchivo = "a.pdf",
                Ruta = "/a.pdf"
            };

            Assert.That(async () => await sut.Handle(input),
                Throws.TypeOf<NotFoundException>());
        }

        [Test]
        public void IngresarAdjunto_ClienteId_Vacio_Lanza_Regla()
        {
            var sut = SUT(out var repo, out var uow, out var tenant);

            var input = new IngresarAdjuntoClienteInputDto
            {
                ClienteId = Guid.Empty,
                NombreArchivo = "a.pdf",
                Ruta = "/a.pdf"
            };

            Assert.That(async () => await sut.Handle(input),
                Throws.TypeOf<BusinessRuleException>()
                    .With.Message.Contains("ClienteId no puede ser vacío"));
        }

        [Test]
        public void IngresarAdjunto_Sin_NombreArchivo_Lanza_Regla()
        {
            var sut = SUT(out var repo, out var uow, out var tenant);
            var cliente = ClienteRuc();
            repo.Setup(r => r.GetByIdAsync(EmpresaDemo(), cliente.ClienteId)).ReturnsAsync(cliente);

            var input = new IngresarAdjuntoClienteInputDto
            {
                ClienteId = cliente.ClienteId,
                NombreArchivo = "  ",
                Ruta = "/a.pdf"
            };

            Assert.That(async () => await sut.Handle(input),
                Throws.TypeOf<BusinessRuleException>()
                    .With.Message.Contains("NombreArchivo es obligatorio"));
        }

        [Test]
        public void IngresarAdjunto_Sin_Ruta_Lanza_Regla()
        {
            var sut = SUT(out var repo, out var uow, out var tenant);
            var cliente = ClienteRuc();
            repo.Setup(r => r.GetByIdAsync(EmpresaDemo(), cliente.ClienteId)).ReturnsAsync(cliente);

            var input = new IngresarAdjuntoClienteInputDto
            {
                ClienteId = cliente.ClienteId,
                NombreArchivo = "a.pdf",
                Ruta = " "
            };

            Assert.That(async () => await sut.Handle(input),
                Throws.TypeOf<BusinessRuleException>()
                    .With.Message.Contains("Ruta es obligatoria"));
        }
    }
}
