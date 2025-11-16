using GestionClientesBC.Application.Interfaces; // IUnitOfWork
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GestionClientesBC.Application.Clientes.Editar;
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
    public class EditarClienteUseCaseTests
    {
        private static EmpresaId EmpresaDemo() => EmpresaId.From("EMPRESA-TEST-001");
        private static DocumentoIdentidad RUC(string v) => DocumentoIdentidad.Crear(TipoDocumento.Ruc, v);
        private static DocumentoIdentidad DNI(string v) => DocumentoIdentidad.Crear(TipoDocumento.Dni, v);
        private static SharedKernel.ValueObjects.RazonSocial RS(string s) => SharedKernel.ValueObjects.RazonSocial.Crear(s);
    private static SharedKernel.ValueObjects.NombrePersona NP(string nombre, string apellidos) => SharedKernel.ValueObjects.NombrePersona.Crear(nombre, apellidos);
        private static Email Mail(string v) => Email.Create(v);

        private static EditarClienteUseCase SUT(
            out Mock<IClienteRepository> repo,
            out Mock<IUnitOfWork> uow,
            out Mock<ITenantContext> tenant)
        {
            repo = new Mock<IClienteRepository>(MockBehavior.Strict);
            uow  = new Mock<IUnitOfWork>(MockBehavior.Strict);
            tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaDemo());
            uow.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            return new EditarClienteUseCase(repo.Object, uow.Object, tenant.Object);
        }

        private static Cliente ClienteRucBase(string ruc = "20661287099")
        {
            return new Cliente(
                clienteId: Guid.NewGuid(),
                empresaId: EmpresaDemo(),
                documento: RUC(ruc),
                razonSocial: RS("ACME S.A.C."),
                nombres: null,
                correo: Mail("ventas@acme.com"),
                telefono: Telefono.FromTexto("999 111 222"),
                domicilioFiscal: null,
                tipoCliente: TipoCliente.Cliente,
                rolCliente: RolCliente.SinDefinir,
                estado: EstadoCliente.Habilitado);
        }

        private static Cliente ClienteDniBase(string dni = "12345678")
        {
            return new Cliente(
                clienteId: Guid.NewGuid(),
                empresaId: EmpresaDemo(),
                documento: DNI(dni),
                razonSocial: null,
                nombres: NP("Juan", "Pérez"),
                correo: Mail("juan@x.com"),
                telefono: Telefono.FromTexto("900 000 000"),
                domicilioFiscal: null,
                tipoCliente: TipoCliente.Cliente,
                rolCliente: null,
                estado: EstadoCliente.Habilitado);
        }

        [Test]
        public async Task Editar_Ruc_Actualiza_Razon_Contacto_Direccion_TipoRol()
        {
            var sut = SUT(out var repo, out var uow, out var tenant);

            var existente = ClienteRucBase();
            repo.Setup(r => r.GetByIdAsync(EmpresaDemo(), existente.ClienteId)).ReturnsAsync(existente);

            // No se cambia documento, no se consulta duplicados
            repo.Setup(r => r.UpdateAsync(existente, It.IsAny<int>())).Returns(Task.CompletedTask);

            var input = new EditarClienteInputDto
            {
                ClienteId = existente.ClienteId,
                RazonSocial = "ACME PERU S.A.",
                Correo = "ventas@acmeperu.com",
                Telefonos = "912 345 678 / (01) 234 5678",
                // Dirección
                PaisCodigoIso = "PE",
                DireccionLinea = "Av. Siempre Viva 742",
                Ubigeo = "150101",
                Departamento = "Lima",
                Provincia = "Lima",
                Distrito = "Lima",
                AddressTypeCode = "0000",
                // Segmentación
                TipoClienteCodigo = "CP",
                RolClienteCodigo = "MAY"
            };

            var result = await sut.Handle(input);

            Assert.That(result.ClienteId, Is.EqualTo(existente.ClienteId));
            Assert.That(result.RazonSocial, Is.EqualTo("ACME PERU S.A."));
            Assert.That(result.Correo, Is.EqualTo("ventas@acmeperu.com"));
            Assert.That(result.Telefonos, Does.Contain("912").And.Contain("234"));
            Assert.That(result.TipoCliente, Is.EqualTo("CP"));
            Assert.That(result.RolCliente, Is.EqualTo("MAY"));

            repo.Verify(r => r.UpdateAsync(existente, It.IsAny<int>()), Times.Once);
            uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Editar_CambiarDocumento_RUCaRUC_ValidaDuplicado()
        {
            var sut = SUT(out var repo, out var uow, out var tenant);

            var existente = ClienteRucBase("20661287099");
            repo.Setup(r => r.GetByIdAsync(EmpresaDemo(), existente.ClienteId)).ReturnsAsync(existente);

            // Se consultará por número del nuevo doc
            repo.Setup(r => r.SearchAsync(EmpresaDemo(), "20239867198", null, null))
                .ReturnsAsync(new List<Cliente> { ClienteRucBase("20239867198") });

            var input = new EditarClienteInputDto
            {
                ClienteId = existente.ClienteId,
                TipoDocumento = TipoDocumento.Ruc,
                NumeroDocumento = "20239867198"
            };

            await Task.CompletedTask; // Para evitar warning CS1998
            Assert.That(async () => await sut.Handle(input),
                Throws.TypeOf<BusinessRuleException>()
                    .With.Message.Contains("Ya existe un cliente con el mismo documento"));
        }

        [Test]
        public void Editar_CambiarDocumento_DNIaRUC_Requiere_RazonSocial()
        {
            var sut = SUT(out var repo, out var uow, out var tenant);

            var existente = ClienteDniBase("12345678");
            repo.Setup(r => r.GetByIdAsync(EmpresaDemo(), existente.ClienteId)).ReturnsAsync(existente);
            // Setup faltante para SearchAsync:
            repo.Setup(r => r.SearchAsync(EmpresaDemo(), It.IsAny<string>(), null, null)).ReturnsAsync(new List<Cliente>());

            var input = new EditarClienteInputDto
            {
                ClienteId = existente.ClienteId,
                TipoDocumento = TipoDocumento.Ruc,
                NumeroDocumento = "20661287099"
                // no se envía RazonSocial
            };

            Assert.That(async () => await sut.Handle(input),
                Throws.TypeOf<BusinessRuleException>()
                    .With.Message.Contains("razón social"));
        }

        [Test]
        public async Task Editar_Dni_Actualiza_NombrePersona_DesdeCamposExplícitos()
        {
            var sut = SUT(out var repo, out var uow, out var tenant);

            var existente = ClienteDniBase("12345678");
            repo.Setup(r => r.GetByIdAsync(EmpresaDemo(), existente.ClienteId)).ReturnsAsync(existente);
            repo.Setup(r => r.UpdateAsync(existente, It.IsAny<int>())).Returns(Task.CompletedTask);

            var input = new EditarClienteInputDto
            {
                ClienteId = existente.ClienteId,
                Nombres = "Ana María",
                Apellidos = "García Torres"
            };

            var result = await sut.Handle(input);

            Assert.That(result.Nombres, Is.EqualTo("Ana María García Torres"));

            repo.Verify(r => r.UpdateAsync(existente, It.IsAny<int>()), Times.Once);
            uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Editar_Estado_Deshabilitar_y_Luego_Habilitar()
        {
            var sut = SUT(out var repo, out var uow, out var tenant);

            var existente = ClienteRucBase();
            repo.Setup(r => r.GetByIdAsync(EmpresaDemo(), existente.ClienteId)).ReturnsAsync(existente);
            repo.Setup(r => r.UpdateAsync(existente, It.IsAny<int>())).Returns(Task.CompletedTask);

            var deshabilitar = new EditarClienteInputDto
            {
                ClienteId = existente.ClienteId,
                TipoDocumento = TipoDocumento.Ruc,
                NumeroDocumento = "20661287099",
                Habilitado = false,
                MotivoDeshabilitacion = "Baja temporal"
            };

            var out1 = await sut.Handle(deshabilitar);
            Assert.That(out1.Estado, Is.EqualTo(EstadoCliente.Deshabilitado.Codigo));

            // Segunda llamada: habilitar
            repo.Invocations.Clear();
            uow.Invocations.Clear();
            repo.Setup(r => r.GetByIdAsync(EmpresaDemo(), existente.ClienteId)).ReturnsAsync(existente);
            repo.Setup(r => r.UpdateAsync(existente, It.IsAny<int>())).Returns(Task.CompletedTask);

            var habilitar = new EditarClienteInputDto
            {
                ClienteId = existente.ClienteId,
                Habilitado = true
            };

            var out2 = await sut.Handle(habilitar);
            Assert.That(out2.Estado, Is.EqualTo(EstadoCliente.Habilitado.Codigo));
        }

        [Test]
        public async Task Editar_SoloCorreo_SinTelefonoActual_ActualizaCorreo()
        {
            var sut = SUT(out var repo, out var uow, out var tenant);

            var existente = new Cliente(
                Guid.NewGuid(),
                EmpresaDemo(),
                RUC("20661287099"),
                RS("FOO S.A.C."),
                nombres: null,
                correo: null,
                telefono: null,
                domicilioFiscal: null,
                tipoCliente: TipoCliente.Cliente,
                rolCliente: null,
                estado: EstadoCliente.Habilitado);

            repo.Setup(r => r.GetByIdAsync(EmpresaDemo(), existente.ClienteId)).ReturnsAsync(existente);
            repo.Setup(r => r.UpdateAsync(existente, It.IsAny<int>())).Returns(Task.CompletedTask);
            uow.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var input = new EditarClienteInputDto
            {
                ClienteId = existente.ClienteId,
                Correo = "ventas@foo.com"
            };

            var output = await sut.Handle(input);

            Assert.That(output.Correo, Is.EqualTo("ventas@foo.com"));
            Assert.That(existente.Correo!.Value, Is.EqualTo("ventas@foo.com"));

            repo.Verify(r => r.UpdateAsync(existente, It.IsAny<int>()), Times.Once);
            uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Editar_Actualiza_MetadatosOpcionales()
        {
            var sut = SUT(out var repo, out var uow, out var tenant);

            var existente = ClienteRucBase();
            repo.Setup(r => r.GetByIdAsync(EmpresaDemo(), existente.ClienteId)).ReturnsAsync(existente);
            repo.Setup(r => r.UpdateAsync(existente, It.IsAny<int>())).Returns(Task.CompletedTask);

            var input = new EditarClienteInputDto
            {
                ClienteId = existente.ClienteId,
                NombreComercial = "Cliente Premium",
                PaginaWeb = "https://acme.pe",
                Observaciones = "VIP",
                FotoPerfilNombreArchivo = "logo.png",
                FotoPerfilUrl = "https://cdn.acme.pe/logo.png"
            };

            var result = await sut.Handle(input);

            Assert.That(result.NombreComercial, Is.EqualTo("Cliente Premium"));
            Assert.That(result.PaginaWeb, Is.EqualTo("https://acme.pe"));
            Assert.That(result.Observaciones, Is.EqualTo("VIP"));
            Assert.That(result.FotoPerfilNombreArchivo, Is.EqualTo("logo.png"));
            Assert.That(result.FotoPerfilUrl, Is.EqualTo("https://cdn.acme.pe/logo.png"));

            repo.Verify(r => r.UpdateAsync(existente, It.IsAny<int>()), Times.Once);
            uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void Editar_NoExisteCliente_LanzaNotFound()
        {
            var sut = SUT(out var repo, out var uow, out var tenant);

            repo.Setup(r => r.GetByIdAsync(EmpresaDemo(), It.IsAny<Guid>())).ReturnsAsync((Cliente?)null);

            var input = new EditarClienteInputDto { ClienteId = Guid.NewGuid() };

            Assert.That(async () => await sut.Handle(input),
                Throws.TypeOf<NotFoundException>());
        }
    }
}
