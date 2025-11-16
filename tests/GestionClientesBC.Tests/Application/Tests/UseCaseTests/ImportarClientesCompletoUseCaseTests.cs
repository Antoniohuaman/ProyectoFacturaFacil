using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GestionClientesBC.Application.Clientes.Importar.Basico;
using GestionClientesBC.Application.Clientes.Importar.Completo;
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
    public class ImportarClientesCompletoUseCaseTests
    {
        [Test]
        public async Task Handle_CreaClienteConCamposCompletos()
        {
            var empresaId = EmpresaId.From("EMPRESA-IMPORT-C");
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

            var sut = new ImportarClientesCompletoUseCase(repo.Object, uow.Object, tenant.Object);

            var fila = new ImportarClientesCompletoFilaDto
            {
                TipoDocumento = TipoDocumento.Ruc.ToString(),
                NumeroDocumento = "20661287099",
                RazonSocial = "Servicios Demo SAC",
                Correo = "servicios@demo.com",
                Telefonos = "+51 955 111 333",
                NombreComercial = "Servicios Demo",
                PaginaWeb = "https://servicios.demo",
                Observaciones = "Cliente corporativo",
                PaisCodigoIso = "PE",
                DireccionLinea = "Av. Los Álamos 123",
                Ubigeo = "150101",
                Departamento = "Lima",
                Provincia = "Lima",
                Distrito = "Lima",
                AddressTypeCode = "0000",
                TipoClienteCodigo = "CP",
                RolClienteCodigo = RolCliente.Mayorista.Codigo,
                FotoPerfilNombreArchivo = "demo.png",
                FotoPerfilUrl = "https://cdn.demo.com/demo.png"
            };

            var input = new ImportarClientesCompletoInputDto
            {
                Filas = new[] { fila }
            };

            var resultado = await sut.Handle(input);

            Assert.That(resultado.Nuevos, Is.EqualTo(1));
            Assert.That(resultado.Actualizados, Is.Zero);
            Assert.That(agregado, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(agregado!.NombreComercial!.ParaMostrar, Is.EqualTo("Servicios Demo"));
                Assert.That(agregado.PaginaWeb!.Valor, Is.EqualTo("https://servicios.demo"));
                Assert.That(agregado.Observaciones!.Valor, Is.EqualTo("Cliente corporativo"));
                Assert.That(agregado.DomicilioFiscal!.Linea, Is.EqualTo("Av. Los Álamos 123"));
                Assert.That(agregado.TipoCliente!.Codigo, Is.EqualTo("CP"));
                Assert.That(agregado.RolCliente!.Codigo, Is.EqualTo(RolCliente.Mayorista.Codigo));
                Assert.That(agregado.FotoPerfil!.NombreArchivo, Is.EqualTo("demo.png"));
            });

            repo.Verify(r => r.AddAsync(It.IsAny<Cliente>()), Times.Once);
            uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_CuandoExisteCliente_ActualizaCamposExtendidos()
        {
            var empresaId = EmpresaId.From("EMPRESA-IMPORT-C");
            var repo = new Mock<IClienteRepository>(MockBehavior.Strict);
            var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            var clienteExistente = CrearClienteRucBasico(empresaId, "20600893409", "Industria Inicial SAC");

            tenant.SetupGet(t => t.EmpresaId).Returns(empresaId);
            repo.Setup(r => r.SearchAsync(empresaId, clienteExistente.Documento.Numero, It.IsAny<int?>(), It.IsAny<int?>()))
                .ReturnsAsync(new List<Cliente> { clienteExistente });
            repo.Setup(r => r.UpdateAsync(clienteExistente, It.IsAny<int>()))
                .Returns(Task.CompletedTask);
            uow.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var sut = new ImportarClientesCompletoUseCase(repo.Object, uow.Object, tenant.Object);

            var fila = new ImportarClientesCompletoFilaDto
            {
                TipoDocumento = TipoDocumento.Ruc.ToString(),
                NumeroDocumento = clienteExistente.Documento.Numero,
                RazonSocial = "Industria Inicial SAC",
                Correo = "nuevocontacto@industria.com",
                Telefonos = "+51 922 100 200",
                NombreComercial = "Industria Renovada",
                PaginaWeb = "https://industria.renovada",
                Observaciones = "Observaciones nuevas",
                PaisCodigoIso = "PE",
                DireccionLinea = "Av. Reformada 456",
                Ubigeo = "150102",
                Departamento = "Lima",
                Provincia = "Lima",
                Distrito = "Miraflores",
                AddressTypeCode = "0001",
                TipoClienteCodigo = "P",
                RolClienteCodigo = RolCliente.Minorista.Codigo,
                FotoPerfilNombreArchivo = "nueva.png",
                FotoPerfilUrl = "https://cdn.demo.com/nueva.png"
            };

            var resultado = await sut.Handle(new ImportarClientesCompletoInputDto { Filas = new[] { fila } });

            Assert.That(resultado.Nuevos, Is.Zero);
            Assert.That(resultado.Actualizados, Is.EqualTo(1));
            Assert.Multiple(() =>
            {
                Assert.That(clienteExistente.NombreComercial!.ParaMostrar, Is.EqualTo("Industria Renovada"));
                Assert.That(clienteExistente.PaginaWeb!.Valor, Is.EqualTo("https://industria.renovada"));
                Assert.That(clienteExistente.Observaciones!.Valor, Is.EqualTo("Observaciones nuevas"));
                Assert.That(clienteExistente.DomicilioFiscal!.Linea, Is.EqualTo("Av. Reformada 456"));
                Assert.That(clienteExistente.TipoCliente!.Codigo, Is.EqualTo("P"));
                Assert.That(clienteExistente.RolCliente!.Codigo, Is.EqualTo(RolCliente.Minorista.Codigo));
                Assert.That(clienteExistente.Correo!.Value, Is.EqualTo("nuevocontacto@industria.com"));
                Assert.That(clienteExistente.Telefono!.UnirParaMostrar(), Is.EqualTo(Telefono.FromTexto("+51 922 100 200").UnirParaMostrar()));
                Assert.That(clienteExistente.FotoPerfil!.NombreArchivo, Is.EqualTo("nueva.png"));
            });

            repo.Verify(r => r.UpdateAsync(clienteExistente, It.IsAny<int>()), Times.Once);
            repo.Verify(r => r.AddAsync(It.IsAny<Cliente>()), Times.Never);
            uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_ActualizaSoloTelefonos_CuandoNoHayCorreoEnFila()
        {
            var empresaId = EmpresaId.From("EMPRESA-IMPORT-C");
            var repo = new Mock<IClienteRepository>(MockBehavior.Strict);
            var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            var clienteExistente = CrearClienteRucBasico(empresaId, "20661287099", "Cliente Telefono SAC");

            tenant.SetupGet(t => t.EmpresaId).Returns(empresaId);
            repo.Setup(r => r.SearchAsync(empresaId, clienteExistente.Documento.Numero, It.IsAny<int?>(), It.IsAny<int?>()))
                .ReturnsAsync(new List<Cliente> { clienteExistente });
            repo.Setup(r => r.UpdateAsync(clienteExistente, It.IsAny<int>()))
                .Returns(Task.CompletedTask);
            uow.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var sut = new ImportarClientesCompletoUseCase(repo.Object, uow.Object, tenant.Object);

            var fila = new ImportarClientesCompletoFilaDto
            {
                TipoDocumento = TipoDocumento.Ruc.ToString(),
                NumeroDocumento = clienteExistente.Documento.Numero,
                RazonSocial = "Cliente Telefono SAC",
                Correo = null,
                Telefonos = "+51 955 444 999"
            };

            var resultado = await sut.Handle(new ImportarClientesCompletoInputDto { Filas = new[] { fila } });

            Assert.That(resultado.Actualizados, Is.EqualTo(1));
            Assert.That(clienteExistente.Telefono!.UnirParaMostrar(), Is.EqualTo(Telefono.FromTexto("+51 955 444 999").UnirParaMostrar()));

            repo.Verify(r => r.UpdateAsync(clienteExistente, It.IsAny<int>()), Times.Once);
            uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void Handle_SinEmpresaActual_LanzaRegla()
        {
            var repo = new Mock<IClienteRepository>(MockBehavior.Strict);
            var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            tenant.SetupGet(t => t.EmpresaId).Returns((EmpresaId)null!);

            var sut = new ImportarClientesCompletoUseCase(repo.Object, uow.Object, tenant.Object);

            var input = new ImportarClientesCompletoInputDto
            {
                Filas = new[]
                {
                    new ImportarClientesCompletoFilaDto
                    {
                        TipoDocumento = TipoDocumento.Ruc.ToString(),
                        NumeroDocumento = "20600893409",
                        RazonSocial = "Cliente Completo"
                    }
                }
            };

            Assert.That(async () => await sut.Handle(input),
                Throws.TypeOf<BusinessRuleException>()
                    .With.Message.Contains("Empresa"));
        }

        private static Cliente CrearClienteRucBasico(EmpresaId empresaId, string numeroRuc, string razon)
        {
            return new Cliente(
                Guid.NewGuid(),
                empresaId,
                DocumentoIdentidad.Crear(TipoDocumento.Ruc, numeroRuc),
                RazonSocial.Crear(razon),
                nombres: null,
                correo: Email.Create("contacto@original.com"),
                telefono: Telefono.FromTexto("+51 900 111 222"),
                domicilioFiscal: null,
                tipoCliente: TipoCliente.Cliente,
                rolCliente: null,
                estado: EstadoCliente.Habilitado);
        }
    }
}
