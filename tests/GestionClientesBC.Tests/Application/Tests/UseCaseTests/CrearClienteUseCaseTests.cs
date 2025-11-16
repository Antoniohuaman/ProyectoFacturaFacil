using GestionClientesBC.Application.Interfaces; // IUnitOfWork
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GestionClientesBC.Application.Clientes.Crear;
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
    public class CrearClienteUseCaseTests
    {
        private static EmpresaId EmpresaDemo() => EmpresaId.From("EMPRESA-TEST-001");

        private static DocumentoIdentidad Ruc(string n) => DocumentoIdentidad.Crear(TipoDocumento.Ruc, n);
        private static DocumentoIdentidad Dni(string n) => DocumentoIdentidad.Crear(TipoDocumento.Dni, n);
        private static SharedKernel.ValueObjects.RazonSocial RS(string s) => SharedKernel.ValueObjects.RazonSocial.Crear(s);

        // UseCase factory
        private static CrearClienteUseCase BuildSut(
            out Mock<IClienteRepository> repo,
            out Mock<IUnitOfWork> uow,
            out Mock<ITenantContext> tenant)
        {
            repo = new Mock<IClienteRepository>(MockBehavior.Strict);
            uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaDemo());

            uow.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            return new CrearClienteUseCase(repo.Object, uow.Object, tenant.Object);
        }

        [Test]
        public async Task Crear_RUC_Minimo_Obligatorio_Ok()
        {
            var sut = BuildSut(out var repo, out var uow, out var tenant);

            // No hay duplicados
            repo.Setup(r => r.SearchAsync(EmpresaDemo(), "20661287099", null, null))
                .ReturnsAsync(Array.Empty<Cliente>());

            // Se espera que se agregue y se guarde
            repo.Setup(r => r.AddAsync(It.IsAny<Cliente>())).Returns(Task.CompletedTask);

            var input = new CrearClienteInputDto
            {
                TipoDocumento = TipoDocumento.Ruc,
                NumeroDocumento = "20661287099",
                RazonSocial = "ACME S.A.C."
                // el resto opcional
            };

            var result = await sut.Handle(input);

            Assert.That(result.ClienteId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(result.EmpresaId, Is.EqualTo(EmpresaDemo().Value));
            Assert.That(result.TipoDocumento, Is.EqualTo(TipoDocumento.Ruc.ToString()));
            Assert.That(result.NumeroDocumento, Is.EqualTo("20661287099"));
            Assert.That(result.RazonSocial, Is.EqualTo("ACME S.A.C."));
            Assert.That(result.Nombres, Is.EqualTo(string.Empty));
            Assert.That(result.Estado, Is.EqualTo(EstadoCliente.Habilitado.Nombre));

            repo.Verify(r => r.AddAsync(It.IsAny<Cliente>()), Times.Once);
            uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Crear_DNI_Minimo_Obligatorio_Ok()
        {
            var sut = BuildSut(out var repo, out var uow, out var tenant);

            // No hay duplicados
            repo.Setup(r => r.SearchAsync(EmpresaDemo(), "12345678", null, null))
                .ReturnsAsync(Array.Empty<Cliente>());

            // Persistencia
            repo.Setup(r => r.AddAsync(It.IsAny<Cliente>())).Returns(Task.CompletedTask);

            var input = new CrearClienteInputDto
            {
                TipoDocumento = TipoDocumento.Dni,
                NumeroDocumento = "12345678",
                Nombres = "Juan",
                Apellidos = "Pérez"
            };

            var result = await sut.Handle(input);

            Assert.That(result.TipoDocumento, Is.EqualTo(TipoDocumento.Dni.ToString()));
            Assert.That(result.NumeroDocumento, Is.EqualTo("12345678"));
            Assert.That(result.RazonSocial, Is.Null);
            // Nombres llega del aggregate -> .Completo; como no conocemos implementación,
            // verificamos que simplemente no sea null (el SUT no lo manipula).
            Assert.That(result.Nombres, Is.Not.Null);

            repo.Verify(r => r.AddAsync(It.IsAny<Cliente>()), Times.Once);
            uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Crear_DNI_SoloNombresCompletos_Ok()
        {
            var sut = BuildSut(out var repo, out var uow, out var tenant);

            repo.Setup(r => r.SearchAsync(EmpresaDemo(), "87654321", null, null))
                .ReturnsAsync(Array.Empty<Cliente>());
            repo.Setup(r => r.AddAsync(It.IsAny<Cliente>())).Returns(Task.CompletedTask);

            var input = new CrearClienteInputDto
            {
                TipoDocumento = TipoDocumento.Dni,
                NumeroDocumento = "87654321",
                NombresCompletos = "Juan Carlos Pérez López"
            };

            var result = await sut.Handle(input);

            Assert.That(result.Nombres, Is.EqualTo("Juan Carlos Pérez López"));

            repo.Verify(r => r.AddAsync(It.IsAny<Cliente>()), Times.Once);
            uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void Crear_RUC_Sin_RazonSocial_Lanza()
        {
            var sut = BuildSut(out var repo, out _, out _);

            var input = new CrearClienteInputDto
            {
                TipoDocumento = TipoDocumento.Ruc,
                NumeroDocumento = "20661287099",
                RazonSocial = null
            };

            Assert.That(async () => await sut.Handle(input),
                Throws.TypeOf<BusinessRuleException>()
                      .With.Message.Contains("razón social es obligatoria"));
        }

        [Test]
        public void Crear_DNI_Sin_Nombres_Lanza()
        {
            var sut = BuildSut(out var repo, out _, out _);

            var input = new CrearClienteInputDto
            {
                TipoDocumento = TipoDocumento.Dni,
                NumeroDocumento = "12345678",
                NombresCompletos = null
            };

            Assert.That(async () => await sut.Handle(input),
                Throws.TypeOf<BusinessRuleException>()
                      .With.Message.Contains("Los nombres son obligatorios"));
        }

        [Test]
        public void Crear_Duplicado_PorEmpresaYDocumento_Lanza()
        {
            var sut = BuildSut(out var repo, out _, out var tenant);

            // Cliente existente con mismo doc y misma empresa
            var existente = new Cliente(
                Guid.NewGuid(),
                empresaId: tenant.Object.EmpresaId,
                documento: Ruc("20661287099"),
                razonSocial: RS("ACME S.A.C."),
                nombres: null);

            repo.Setup(r => r.SearchAsync(EmpresaDemo(), "20661287099", null, null))
                .ReturnsAsync(new[] { existente });

            var input = new CrearClienteInputDto
            {
                TipoDocumento = TipoDocumento.Ruc,
                NumeroDocumento = "20661287099",
                RazonSocial = "OTRA S.A."
            };

            Assert.That(async () => await sut.Handle(input),
                Throws.TypeOf<BusinessRuleException>()
                     .With.Message.Contains("Ya existe un cliente con el mismo documento"));
        }

        [Test]
        public async Task Crear_Con_Extras_Opcionales_Ok()
        {
            var sut = BuildSut(out var repo, out var uow, out var tenant);

            repo.Setup(r => r.SearchAsync(EmpresaDemo(), "20661287099", null, null))
                .ReturnsAsync(Array.Empty<Cliente>());

            repo.Setup(r => r.AddAsync(It.IsAny<Cliente>())).Returns(Task.CompletedTask);

            var input = new CrearClienteInputDto
            {
                TipoDocumento = TipoDocumento.Ruc,
                NumeroDocumento = "20661287099",
                RazonSocial = "FOO S.A.C.",
                Correo = "ventas@foo.com",
                Telefonos = "999 888 777 / (01) 234 5678",
                NombreComercial = "Mi Cliente Top",
                PaginaWeb = "https://foo.com",
                Observaciones = "Cliente preferente",
                FotoPerfilNombreArchivo = "logo.png",
                FotoPerfilUrl = "https://cdn.foo.com/logo.png",
                PaisCodigoIso = "PE",
                DireccionLinea = "Av. Siempre Viva 742",
                Ubigeo = "150101",
                Departamento = "Lima",
                Provincia = "Lima",
                Distrito = "Lima",
                AddressTypeCode = "0000",
                TipoClienteCodigo = "CP",
                RolClienteCodigo = "MAY"
            };

            var result = await sut.Handle(input);

            Assert.That(result.EmpresaId, Is.EqualTo(EmpresaDemo().Value));
            Assert.That(result.RazonSocial, Is.EqualTo("FOO S.A.C."));
            Assert.That(result.Estado, Is.EqualTo(EstadoCliente.Habilitado.Nombre));
            Assert.That(result.NombreComercial, Is.EqualTo("Mi Cliente Top"));
            Assert.That(result.PaginaWeb, Is.EqualTo("https://foo.com"));
            Assert.That(result.Observaciones, Is.EqualTo("Cliente preferente"));
            Assert.That(result.FotoPerfilNombreArchivo, Is.EqualTo("logo.png"));
            Assert.That(result.FotoPerfilUrl, Is.EqualTo("https://cdn.foo.com/logo.png"));

            repo.Verify(r => r.AddAsync(It.IsAny<Cliente>()), Times.Once);
            uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
