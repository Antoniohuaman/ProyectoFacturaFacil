using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GestionClientesBC.Application.Clientes.Importar.Basico;
using GestionClientesBC.Application.Interfaces;
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.Repositories;
using GestionClientesBC.Domain.ValueObjects;
using Moq;
using NUnit.Framework;
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace GestionClientesBC.Tests.Application.Clientes.Importar
{
    [TestFixture]
    public class ImportarClientesBasicoUseCaseTests
    {
        [Test]
        public async Task Handle_SinCoincidencias_CreaNuevoCliente()
        {
            var empresaId = EmpresaId.From("EMPRESA-IMPORT-B");
            var repo = new Mock<IClienteRepository>(MockBehavior.Strict);
            var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            tenant.SetupGet(t => t.EmpresaId).Returns(empresaId);
            repo.Setup(r => r.SearchAsync(empresaId, "20661287099", It.IsAny<int?>(), It.IsAny<int?>()))
                .ReturnsAsync(Array.Empty<Cliente>());

            Cliente? agregado = null;
            repo.Setup(r => r.AddAsync(It.IsAny<Cliente>()))
                .Callback<Cliente>(c => agregado = c)
                .Returns(Task.CompletedTask);

            uow.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var sut = new ImportarClientesBasicoUseCase(repo.Object, uow.Object, tenant.Object);

            var input = new ImportarClientesBasicoInputDto
            {
                Filas = new[]
                {
                    new ImportarClientesBasicoFilaDto
                    {
                        TipoDocumento = TipoDocumento.Ruc.ToString(),
                        NumeroDocumento = "20661287099",
                        RazonSocial = "Tech Demo SAC",
                        Correo = "ventas@techdemo.com",
                        Telefonos = "+51 999 888 777"
                    }
                }
            };

            var resultado = await sut.Handle(input);

            Assert.That(resultado.Nuevos, Is.EqualTo(1));
            Assert.That(resultado.Actualizados, Is.Zero);
            Assert.That(agregado, Is.Not.Null);
            Assert.That(agregado!.EmpresaId, Is.EqualTo(empresaId));
            Assert.That(agregado.Documento.Numero, Is.EqualTo("20661287099"));

            repo.Verify(r => r.AddAsync(It.IsAny<Cliente>()), Times.Once);
            repo.Verify(r => r.UpdateAsync(It.IsAny<Cliente>(), It.IsAny<int>()), Times.Never);
            uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_CuandoExisteCliente_ActualizaDatosContacto()
        {
            var empresaId = EmpresaId.From("EMPRESA-IMPORT-B");
            var repo = new Mock<IClienteRepository>(MockBehavior.Strict);
            var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            var clienteExistente = CrearClienteRuc(empresaId, "20600893409", "Demo SAC");

            tenant.SetupGet(t => t.EmpresaId).Returns(empresaId);
            repo.Setup(r => r.SearchAsync(empresaId, clienteExistente.Documento.Numero, It.IsAny<int?>(), It.IsAny<int?>()))
                .ReturnsAsync(new List<Cliente> { clienteExistente });
            repo.Setup(r => r.UpdateAsync(clienteExistente, It.IsAny<int>()))
                .Returns(Task.CompletedTask);
            uow.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var sut = new ImportarClientesBasicoUseCase(repo.Object, uow.Object, tenant.Object);

            var input = new ImportarClientesBasicoInputDto
            {
                Filas = new[]
                {
                    new ImportarClientesBasicoFilaDto
                    {
                        TipoDocumento = TipoDocumento.Ruc.ToString(),
                        NumeroDocumento = clienteExistente.Documento.Numero,
                        RazonSocial = "Demo SAC",
                        Correo = "contacto@demo.com",
                        Telefonos = "+51 900 000 111"
                    }
                }
            };

            var resultado = await sut.Handle(input);

            Assert.That(resultado.Nuevos, Is.Zero);
            Assert.That(resultado.Actualizados, Is.EqualTo(1));
            Assert.That(clienteExistente.Correo!.Value, Is.EqualTo("contacto@demo.com"));
            Assert.That(clienteExistente.Telefono!.UnirParaMostrar(), Is.EqualTo(Telefono.FromTexto("+51 900 000 111").UnirParaMostrar()));

            repo.Verify(r => r.UpdateAsync(clienteExistente, It.IsAny<int>()), Times.Once);
            repo.Verify(r => r.AddAsync(It.IsAny<Cliente>()), Times.Never);
            uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_CuandoFilaSoloTieneCorreo_ActualizaCorreo()
        {
            var empresaId = EmpresaId.From("EMPRESA-IMPORT-B");
            var repo = new Mock<IClienteRepository>(MockBehavior.Strict);
            var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            var clienteExistente = new Cliente(
                Guid.NewGuid(),
                empresaId,
                DocumentoIdentidad.Crear(TipoDocumento.Ruc, "20661287099"),
                RazonSocial.Crear("Cliente Solo Correo SAC"),
                nombres: null,
                correo: Email.Create("original@cliente.com"),
                telefono: null,
                domicilioFiscal: null,
                tipoCliente: TipoCliente.Cliente,
                rolCliente: null,
                estado: EstadoCliente.Habilitado);

            tenant.SetupGet(t => t.EmpresaId).Returns(empresaId);
            repo.Setup(r => r.SearchAsync(empresaId, clienteExistente.Documento.Numero, It.IsAny<int?>(), It.IsAny<int?>()))
                .ReturnsAsync(new List<Cliente> { clienteExistente });
            repo.Setup(r => r.UpdateAsync(clienteExistente, It.IsAny<int>()))
                .Returns(Task.CompletedTask);
            uow.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var sut = new ImportarClientesBasicoUseCase(repo.Object, uow.Object, tenant.Object);

            var input = new ImportarClientesBasicoInputDto
            {
                Filas = new[]
                {
                    new ImportarClientesBasicoFilaDto
                    {
                        TipoDocumento = TipoDocumento.Ruc.ToString(),
                        NumeroDocumento = clienteExistente.Documento.Numero,
                        RazonSocial = "Cliente Solo Correo SAC",
                        Correo = "actualizado@cliente.com",
                        Telefonos = null
                    }
                }
            };

            var resultado = await sut.Handle(input);

            Assert.That(resultado.Nuevos, Is.Zero);
            Assert.That(resultado.Actualizados, Is.EqualTo(1));
            Assert.That(clienteExistente.Correo!.Value, Is.EqualTo("actualizado@cliente.com"));
            Assert.That(clienteExistente.Telefono, Is.Null);

            repo.Verify(r => r.UpdateAsync(clienteExistente, It.IsAny<int>()), Times.Once);
            uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_IgnoraCoincidenciasDeOtroTenant()
        {
            var empresaId = EmpresaId.From("EMPRESA-IMPORT-B");
            var otraEmpresa = EmpresaId.From("EMPRESA-IMPORT-C");
            var repo = new Mock<IClienteRepository>(MockBehavior.Strict);
            var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            var clienteOtraEmpresa = new Cliente(
                Guid.NewGuid(),
                otraEmpresa,
                DocumentoIdentidad.Crear(TipoDocumento.Ruc, "20600893409"),
                RazonSocial.Crear("Cliente Otro Tenant"),
                nombres: null,
                correo: Email.Create("externo@cliente.com"),
                telefono: Telefono.FromTexto("+51 955 100 100"),
                domicilioFiscal: null,
                tipoCliente: TipoCliente.Cliente,
                rolCliente: null,
                estado: EstadoCliente.Habilitado);

            tenant.SetupGet(t => t.EmpresaId).Returns(empresaId);
            repo.Setup(r => r.SearchAsync(empresaId, clienteOtraEmpresa.Documento.Numero, It.IsAny<int?>(), It.IsAny<int?>()))
                .ReturnsAsync(new List<Cliente> { clienteOtraEmpresa });

            Cliente? agregado = null;
            repo.Setup(r => r.AddAsync(It.IsAny<Cliente>()))
                .Callback<Cliente>(c => agregado = c)
                .Returns(Task.CompletedTask);

            uow.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var sut = new ImportarClientesBasicoUseCase(repo.Object, uow.Object, tenant.Object);

            var input = new ImportarClientesBasicoInputDto
            {
                Filas = new[]
                {
                    new ImportarClientesBasicoFilaDto
                    {
                        TipoDocumento = TipoDocumento.Ruc.ToString(),
                        NumeroDocumento = clienteOtraEmpresa.Documento.Numero,
                        RazonSocial = "Cliente Otro Tenant",
                        Correo = "propio@tenant.com",
                        Telefonos = "+51 955 200 200"
                    }
                }
            };

            var resultado = await sut.Handle(input);

            Assert.That(resultado.Nuevos, Is.EqualTo(1));
            Assert.That(resultado.Actualizados, Is.Zero);
            Assert.That(agregado, Is.Not.Null);
            Assert.That(agregado!.EmpresaId, Is.EqualTo(empresaId));

            repo.Verify(r => r.AddAsync(It.IsAny<Cliente>()), Times.Once);
            repo.Verify(r => r.UpdateAsync(It.IsAny<Cliente>(), It.IsAny<int>()), Times.Never);
            uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void Handle_SinEmpresaActual_LanzaRegla()
        {
            var repo = new Mock<IClienteRepository>(MockBehavior.Strict);
            var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            tenant.SetupGet(t => t.EmpresaId).Returns((EmpresaId)null!);

            var sut = new ImportarClientesBasicoUseCase(repo.Object, uow.Object, tenant.Object);

            var input = new ImportarClientesBasicoInputDto
            {
                Filas = new[]
                {
                    new ImportarClientesBasicoFilaDto
                    {
                        TipoDocumento = TipoDocumento.Ruc.ToString(),
                        NumeroDocumento = "20661287099",
                        RazonSocial = "Cliente Demo"
                    }
                }
            };

            Assert.That(async () => await sut.Handle(input),
                Throws.TypeOf<BusinessRuleException>()
                    .With.Message.Contains("Empresa"));
        }

        private static Cliente CrearClienteRuc(EmpresaId empresaId, string numeroRuc, string razon)
        {
            return new Cliente(
                Guid.NewGuid(),
                empresaId,
                DocumentoIdentidad.Crear(TipoDocumento.Ruc, numeroRuc),
                RazonSocial.Crear(razon),
                nombres: null,
                correo: Email.Create("ventas@demo.com"),
                telefono: Telefono.FromTexto("+51 988 111 222"),
                domicilioFiscal: null,
                tipoCliente: TipoCliente.Cliente,
                rolCliente: null,
                estado: EstadoCliente.Habilitado);
        }
    }
}
