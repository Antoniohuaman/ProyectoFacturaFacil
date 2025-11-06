using GestionClientesBC.Application.Interfaces; // IUnitOfWork
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GestionClientesBC.Application.Clientes.Contactos.Agregar;
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

namespace GestionClientesBC.Tests.Application.Clientes.Contactos
{
    [TestFixture]
    public class AgregarContactoClienteUseCaseTests
    {
        private static EmpresaId EmpresaDemo() => EmpresaId.From("EMPRESA-TEST-001");
        private static EmpresaId OtraEmpresa()  => EmpresaId.From("EMPRESA-OTRA");

        private static DocumentoIdentidad RUC(string v) => DocumentoIdentidad.Crear(TipoDocumento.Ruc, v);
        private static DocumentoIdentidad DNI(string v) => DocumentoIdentidad.Crear(TipoDocumento.Dni, v);
        private static SharedKernel.ValueObjects.RazonSocial RS(string v) => SharedKernel.ValueObjects.RazonSocial.Crear(v);
        // Helper para crear NombrePersona con nombre y apellidos explícitos
        private static SharedKernel.ValueObjects.NombrePersona NP(string nombre, string apellidos)
        {
            return SharedKernel.ValueObjects.NombrePersona.Crear(nombre, apellidos);
        }

        private static AgregarContactoClienteUseCase SUT(
            out Mock<IClienteRepository> repo,
            out Mock<IUnitOfWork> uow,
            out Mock<ITenantContext> tenant)
        {
            repo = new Mock<IClienteRepository>(MockBehavior.Strict);
            uow  = new Mock<IUnitOfWork>(MockBehavior.Strict);
            tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaDemo());
            uow.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            return new AgregarContactoClienteUseCase(repo.Object, uow.Object, tenant.Object);
        }

        private static Cliente ClienteRucBase()
        {
            return new Cliente(
                clienteId: Guid.NewGuid(),
                empresaId: EmpresaDemo(),
                documento: RUC("20661287099"),
                razonSocial: RS("ACME S.A.C."),
                nombres: null,
                correo: SharedKernel.ValueObjects.Email.Create("ventas@acme.com"),
                telefono: SharedKernel.ValueObjects.Telefono.FromTexto("999 111 222"),
                domicilioFiscal: null,
                tipoCliente: TipoCliente.ClienteProveedor,
                rolCliente: RolCliente.Mayorista,
                estado: EstadoCliente.Habilitado);
        }

        [Test]
        public async Task AgregarContacto_Exitoso_RegistraEvento_Persistencia_Y_MapeaSalida()
        {
            // Arrange
            var sut = SUT(out var repo, out var uow, out var tenant);
            var cliente = ClienteRucBase();

            repo.Setup(r => r.GetByIdAsync(cliente.EmpresaId, cliente.ClienteId)).ReturnsAsync(cliente);
            repo.Setup(r => r.UpdateAsync(cliente)).Returns(Task.CompletedTask);

            var input = new AgregarContactoClienteInputDto
            {
                ClienteId = cliente.ClienteId,
                NombreContacto = "María Gomez Perez",
                TipoDocumentoContacto = TipoDocumento.Dni,
                NumeroDocumentoContacto = "87654321",
                Emails = new List<string> { "maria@acme.com", "maria@acme.com" }, // duplicado intencional
                Telefonos = new List<string> { "+51 988 777 666", "(01) 234-5678" },
                Direccion = "Jr. Los Laureles 123"
            };

            var eventosAntes = cliente.DomainEvents.Count;

            // Act
            var outDto = await sut.Handle(input);

            // Assert - agregado
            Assert.That(cliente.Contactos.Count, Is.EqualTo(1));
            var agregado = cliente.Contactos.Single();
            Assert.That(agregado.NombreContacto.Completo, Is.EqualTo("María Gomez Perez"));
            Assert.That(agregado.DocumentoIdentidad?.ToString(), Does.StartWith("DNI"));
            Assert.That(agregado.Emails.Count, Is.EqualTo(1)); // dedup
            Assert.That(agregado.Telefonos.Count, Is.EqualTo(2));
            Assert.That(agregado.Direccion, Is.EqualTo("Jr. Los Laureles 123"));

            // Evento
            Assert.That(cliente.DomainEvents.Count, Is.GreaterThan(eventosAntes));
            Assert.That(cliente.DomainEvents.OfType<ContactoAgregado>().Any(), Is.True);

            // Salida
            Assert.That(outDto.ClienteId, Is.EqualTo(cliente.ClienteId));
            Assert.That(outDto.EmpresaId, Is.EqualTo(EmpresaDemo().Value));
            Assert.That(outDto.ContactoId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(outDto.NombreContacto, Is.EqualTo("María Gomez Perez"));
            Assert.That(outDto.DocumentoIdentidad, Does.StartWith("DNI 87654321"));
            Assert.That(outDto.Emails, Is.EquivalentTo(new[] { "maria@acme.com" }));
            Assert.That(outDto.Telefonos.Length, Is.EqualTo(2));
            Assert.That(outDto.Direccion, Is.EqualTo("Jr. Los Laureles 123"));
            Assert.That(outDto.FechaEventoUtc.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(outDto.FechaCreacionUtc.Kind, Is.EqualTo(DateTimeKind.Utc));

            // Persistencia
            repo.Verify(r => r.UpdateAsync(cliente), Times.Once);
            uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void AgregarContacto_Duplicado_Lanza_BusinessRule_Y_NoPersiste()
        {
            // Arrange
            var sut = SUT(out var repo, out var uow, out var tenant);
            var cliente = ClienteRucBase();

            // Pre-cargar un contacto idéntico
            var contacto = new ContactoCliente(
                contactoId: Guid.NewGuid(),
                nombreContacto: NP("Ana", "Paredes Lopez"),
                documentoIdentidad: DNI("11112222"),
                emails: new List<SharedKernel.ValueObjects.Email> { SharedKernel.ValueObjects.Email.Create("ana@acme.com") },
                telefonos: new List<SharedKernel.ValueObjects.Telefono> { SharedKernel.ValueObjects.Telefono.FromTexto("999 000 111") },
                direccion: "Calle 1"
            );
            cliente.AgregarContacto(contacto);

            repo.Setup(r => r.GetByIdAsync(cliente.EmpresaId, cliente.ClienteId)).ReturnsAsync(cliente);

            var input = new AgregarContactoClienteInputDto
            {
                ClienteId = cliente.ClienteId,
                NombreContacto = "Ana Paredes Lopez",
                TipoDocumentoContacto = TipoDocumento.Dni,
                NumeroDocumentoContacto = "11112222",
                Emails = new List<string> { "ana@acme.com" },
                Telefonos = new List<string> { "999 000 111" },
                Direccion = "Calle 1"
            };

            // Act & Assert
            Assert.That(async () => await sut.Handle(input),
                Throws.TypeOf<BusinessRuleException>()
                      .With.Message.Contains("Ya existe un contacto igual"));

            repo.Verify(r => r.UpdateAsync(It.IsAny<Cliente>()), Times.Never);
            uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public void AgregarContacto_Cliente_De_Otra_Empresa_Lanza_NotFound()
        {
            var sut = SUT(out var repo, out var uow, out var tenant);

            var clienteOtra = new Cliente(
                clienteId: Guid.NewGuid(),
                empresaId: OtraEmpresa(),
                documento: RUC("20600893409"), // RUC válido
                razonSocial: RS("OTRA S.A."),
                nombres: null,
                correo: null,
                telefono: null,
                domicilioFiscal: null,
                tipoCliente: TipoCliente.Cliente,
                rolCliente: null,
                estado: EstadoCliente.Habilitado);

            // Setup para devolver null cuando se consulta con la empresa del tenant (EMPRESA-TEST-001)
            repo.Setup(r => r.GetByIdAsync(It.IsAny<EmpresaId>(), clienteOtra.ClienteId))
                .ReturnsAsync((EmpresaId eid, Guid cid) =>
                    eid.Value == clienteOtra.EmpresaId.Value ? clienteOtra : null);

            var input = new AgregarContactoClienteInputDto
            {
                ClienteId = clienteOtra.ClienteId,
                NombreContacto = "X Apellido Y",
                TipoDocumentoContacto = TipoDocumento.Dni,
                NumeroDocumentoContacto = "12345678"
            };

            Assert.That(async () => await sut.Handle(input),
                Throws.TypeOf<NotFoundException>());
        }

        [Test]
        public void AgregarContacto_TipoDocumento_NoDni_Lanza_Regla()
        {
            var sut = SUT(out var repo, out var uow, out var tenant);
            var cliente = ClienteRucBase();
            repo.Setup(r => r.GetByIdAsync(cliente.EmpresaId, cliente.ClienteId)).ReturnsAsync(cliente);

            var input = new AgregarContactoClienteInputDto
            {
                ClienteId = cliente.ClienteId,
                NombreContacto = "Carlos Apellido Perez",
                TipoDocumentoContacto = TipoDocumento.Ruc, // inválido para contacto
                NumeroDocumentoContacto = "20600011122"
            };

            Assert.That(async () => await sut.Handle(input),
                Throws.TypeOf<BusinessRuleException>()
                      .With.Message.Contains("solo admite DNI"));
        }

        [Test]
        public void AgregarContacto_Nombre_Vacio_Lanza_Regla()
        {
            var sut = SUT(out var repo, out var uow, out var tenant);
            var cliente = ClienteRucBase();
            repo.Setup(r => r.GetByIdAsync(cliente.EmpresaId, cliente.ClienteId)).ReturnsAsync(cliente);

            var input = new AgregarContactoClienteInputDto
            {
                ClienteId = cliente.ClienteId,
                NombreContacto = "   " // Este test debe seguir probando vacío
            };

            Assert.That(async () => await sut.Handle(input),
                Throws.TypeOf<BusinessRuleException>());
        }

        [Test]
        public void AgregarContacto_EmailInvalido_Lanza_Argument()
        {
            var sut = SUT(out var repo, out var uow, out var tenant);
            var cliente = ClienteRucBase();
            repo.Setup(r => r.GetByIdAsync(cliente.EmpresaId, cliente.ClienteId)).ReturnsAsync(cliente);

            var input = new AgregarContactoClienteInputDto
            {
                ClienteId = cliente.ClienteId,
                NombreContacto = "Luz Apellido Perez",
                Emails = new List<string> { "no_es_mail" }
            };

            Assert.That(async () => await sut.Handle(input),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void AgregarContacto_TelefonoInvalido_Lanza_Argument()
        {
            var sut = SUT(out var repo, out var uow, out var tenant);
            var cliente = ClienteRucBase();
            repo.Setup(r => r.GetByIdAsync(cliente.EmpresaId, cliente.ClienteId)).ReturnsAsync(cliente);

            var input = new AgregarContactoClienteInputDto
            {
                ClienteId = cliente.ClienteId,
                NombreContacto = "Luz Apellido Perez",
                Telefonos = new List<string> { "abc-xyz" } // inválido para VO Telefono
            };

            Assert.That(async () => await sut.Handle(input),
                Throws.Exception); // puede ser ArgumentOutOfRangeException/ArgumentException
        }
    }
}
