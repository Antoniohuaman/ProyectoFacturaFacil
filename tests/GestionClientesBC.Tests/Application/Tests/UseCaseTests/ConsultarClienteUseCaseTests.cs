using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GestionClientesBC.Application.Clientes.Consultar;
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.Entities;
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
    public class ConsultarClienteUseCaseTests
    {
        private static EmpresaId EmpresaDemo() => EmpresaId.From("EMPRESA-TEST-001");
        private static EmpresaId OtraEmpresa() => EmpresaId.From("EMPRESA-OTRA");
        private static DocumentoIdentidad RUC(string v) => DocumentoIdentidad.Crear(TipoDocumento.Ruc, v);
        private static DocumentoIdentidad DNI(string v) => DocumentoIdentidad.Crear(TipoDocumento.Dni, v);
        private static SharedKernel.ValueObjects.RazonSocial RS(string s) => SharedKernel.ValueObjects.RazonSocial.Crear(s);
    // Ahora NP recibe nombre y apellidos separados
    private static SharedKernel.ValueObjects.NombrePersona NP(string nombre, string apellidos) => SharedKernel.ValueObjects.NombrePersona.Crear(nombre, apellidos);
        private static SharedKernel.ValueObjects.Email Mail(string s) => SharedKernel.ValueObjects.Email.Create(s);

        private static ConsultarClienteUseCase SUT(
            out Mock<IClienteRepository> repo,
            out Mock<ITenantContext> tenant)
        {
            repo = new Mock<IClienteRepository>(MockBehavior.Strict);
            tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaDemo());
            return new ConsultarClienteUseCase(repo.Object, tenant.Object);
        }

        private static Cliente ClienteRucBase()
        {
            var c = new Cliente(
                clienteId: Guid.NewGuid(),
                empresaId: EmpresaDemo(),
                documento: RUC("20661287099"),
                razonSocial: RS("ACME S.A.C."),
                nombres: null,
                correo: Mail("ventas@acme.com"),
                telefono: Telefono.FromTexto("999 111 222"),
                domicilioFiscal: DomicilioFiscal.FromPeru(
                    linea: "Av. Siempre Viva 742",
                    ubigeo: "150101",
                    departamento: "Lima",
                    provincia: "Lima",
                    distrito: "Lima",
                    addressTypeCode: "0000"),
                tipoCliente: TipoCliente.ClienteProveedor,
                rolCliente: RolCliente.Mayorista,
                estado: EstadoCliente.Habilitado);

            // Contacto
            var contacto = new ContactoCliente(
                contactoId: Guid.NewGuid(),
                nombreContacto: NP("María", "Gomez"),
                documentoIdentidad: DNI("87654321"),
                emails: new List<SharedKernel.ValueObjects.Email> { Mail("maria@acme.com") },
                telefonos: new List<SharedKernel.ValueObjects.Telefono> { Telefono.FromTexto("+51 988 777 666") },
                direccion: "Jr. Los Laureles 123");

            c.AgregarContacto(contacto);

            // Adjunto
            c.AgregarAdjunto(new AdjuntoCliente(
                adjuntoId: Guid.NewGuid(),
                nombreArchivo: "contrato.pdf",
                ruta: "/files/contrato.pdf",
                fechaSubida: DateTime.UtcNow,
                comentario: "Contrato firmado"));

            return c;
        }

        private static Cliente ClienteDniBase()
        {
            var c = new Cliente(
                clienteId: Guid.NewGuid(),
                empresaId: EmpresaDemo(),
                documento: DNI("12345678"),
                razonSocial: null,
                nombres: NP("Juan", "Pérez"),
                correo: Mail("juan@correo.com"),
                telefono: Telefono.FromTexto("900 000 000"),
                domicilioFiscal: null,
                tipoCliente: TipoCliente.Cliente,
                rolCliente: RolCliente.SinDefinir,
                estado: EstadoCliente.Inhabilitado);
            return c;
        }

        [Test]
        public async Task Consultar_PorId_RUC_Incluye_Contactos_Adjuntos()
        {
            var sut = SUT(out var repo, out var tenant);

            var existente = ClienteRucBase();
            repo.Setup(r => r.GetByIdAsync(EmpresaDemo(), existente.ClienteId)).ReturnsAsync(existente);

            var input = new ConsultarClienteInputDto
            {
                ClienteId = existente.ClienteId,
                IncluirAdjuntos = true,
                IncluirContactos = true
            };

            var outDto = await sut.Handle(input);

            Assert.That(outDto.ClienteId, Is.EqualTo(existente.ClienteId));
            Assert.That(outDto.EmpresaId, Is.EqualTo(EmpresaDemo().Value));
            Assert.That(outDto.TipoDocumento, Is.EqualTo(TipoDocumento.Ruc.ToString()));
            Assert.That(outDto.NumeroDocumento, Is.EqualTo("20661287099"));
            Assert.That(outDto.RazonSocial, Is.EqualTo("ACME S.A.C."));
            Assert.That(outDto.Nombres, Is.Null);
            Assert.That(outDto.Correo, Is.EqualTo("ventas@acme.com"));
            Assert.That(outDto.Telefonos, Does.Contain("999").Or.Contain("+51"));
            Assert.That(outDto.TipoClienteCodigo, Is.EqualTo(TipoCliente.ClienteProveedor.Codigo));
            Assert.That(outDto.RolClienteCodigo, Is.EqualTo(RolCliente.Mayorista.Codigo));
            Assert.That(outDto.EstadoCodigo, Is.EqualTo(EstadoCliente.Habilitado.Codigo));
            Assert.That(outDto.DomicilioFiscalResumen, Does.Contain("Av. Siempre Viva 742").And.Contain("Lima"));
            Assert.That(outDto.Contactos.Length, Is.EqualTo(1));
            Assert.That(outDto.Contactos[0].NombreContacto, Is.EqualTo("María Gomez"));
            Assert.That(outDto.Contactos[0].DocumentoIdentidad, Does.StartWith("DNI"));
            Assert.That(outDto.Adjuntos.Length, Is.EqualTo(1));
            Assert.That(outDto.Adjuntos[0].NombreArchivo, Is.EqualTo("contrato.pdf"));
        }

        [Test]
        public async Task Consultar_PorDocumento_DNI_Exitoso()
        {
            var sut = SUT(out var repo, out var tenant);

            var a = ClienteRucBase();
            var b = ClienteDniBase(); // <- el que debe devolver
            var otros = new List<Cliente> { a, b };

            // SearchAsync por número; luego el use case filtra por empresa + tipo + número
            repo.Setup(r => r.SearchAsync(EmpresaDemo(), "12345678", null, null)).ReturnsAsync(otros);

            var input = new ConsultarClienteInputDto
            {
                TipoDocumento = TipoDocumento.Dni,
                NumeroDocumento = "12345678",
                IncluirContactos = false,
                IncluirAdjuntos = false
            };

            var outDto = await sut.Handle(input);

            Assert.That(outDto.ClienteId, Is.EqualTo(b.ClienteId));
            Assert.That(outDto.TipoDocumento, Is.EqualTo(TipoDocumento.Dni.ToString()));
            Assert.That(outDto.NumeroDocumento, Is.EqualTo("12345678"));
            Assert.That(outDto.Nombres, Is.EqualTo("Juan Pérez"));
            Assert.That(outDto.Contactos, Is.Empty);
            Assert.That(outDto.Adjuntos, Is.Empty);
        }

        [Test]
        public void Consultar_No_Envio_Id_Ni_Documento_Lanza_Regla()
        {
            var sut = SUT(out var repo, out var tenant);

            var input = new ConsultarClienteInputDto();
            Assert.That(async () => await sut.Handle(input),
                Throws.TypeOf<BusinessRuleException>()
                      .With.Message.Contains("ClienteId")
                      .And.Message.Contains("TipoDocumento"));
        }

        [Test]
        public void Consultar_PorId_De_Otra_Empresa_Lanza_NotFound()
        {
            var sut = SUT(out var repo, out var tenant);

            var clienteOtra = new Cliente(
                Guid.NewGuid(),
                OtraEmpresa(),
                RUC("20661287099"), // RUC válido
                RS("OTRA S.A."),
                null,
                null,
                null,
                null,
                TipoCliente.Cliente,
                null,
                EstadoCliente.Habilitado);

            repo.Setup(r => r.GetByIdAsync(EmpresaDemo(), clienteOtra.ClienteId)).ReturnsAsync(clienteOtra);

            var input = new ConsultarClienteInputDto { ClienteId = clienteOtra.ClienteId };

            Assert.That(async () => await sut.Handle(input),
                Throws.TypeOf<NotFoundException>());
        }

        [Test]
        public void Consultar_PorDocumento_NoExiste_Lanza_NotFound()
        {
            var sut = SUT(out var repo, out var tenant);

            repo.Setup(r => r.SearchAsync(EmpresaDemo(), "00000000", null, null)).ReturnsAsync(new List<Cliente>());

            var input = new ConsultarClienteInputDto { TipoDocumento = TipoDocumento.Dni, NumeroDocumento = "00000000" };

            Assert.That(async () => await sut.Handle(input),
                Throws.TypeOf<NotFoundException>());
        }
    }
}
