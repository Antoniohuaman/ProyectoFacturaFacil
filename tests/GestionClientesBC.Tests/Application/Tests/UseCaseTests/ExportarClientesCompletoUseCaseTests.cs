using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GestionClientesBC.Application.Clientes.Exportar.Completo;
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.ValueObjects;
using GestionClientesBC.Domain.Repositories;
using Moq;
using NUnit.Framework;
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace GestionClientesBC.Tests.Application.Clientes.Exportar
{
    [TestFixture]
    public class ExportarClientesCompletoUseCaseTests
    {
        private static readonly string[] CabecerasEsperadas =
        {
            "TipoDocumento",
            "NumeroDocumento",
            "RazonSocial",
            "Nombres",
            "Apellidos",
            "NombresCompletos",
            "Correo",
            "Telefonos",
            "NombreComercial",
            "PaginaWeb",
            "Observaciones",
            "PaisCodigoIso",
            "DireccionLinea",
            "Ubigeo",
            "Departamento",
            "Provincia",
            "Distrito",
            "AddressTypeCode",
            "TipoClienteCodigo",
            "RolClienteCodigo",
            "FotoPerfilNombreArchivo",
            "FotoPerfilUrl"
        };

        [Test]
        public async Task Handle_IncluyeCamposExtendidosDeCadaCliente()
        {
            var empresaId = EmpresaId.From("EMPRESA-EXPORT-C");
            var repo = new Mock<IClienteRepository>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            var cliente = CrearClienteCompleto(empresaId);

            tenant.SetupGet(t => t.EmpresaId).Returns(empresaId);
            repo.Setup(r => r.GetAllAsync(empresaId, It.IsAny<int?>(), It.IsAny<int?>()))
                .ReturnsAsync(new List<Cliente> { cliente });

            var sut = new ExportarClientesCompletoUseCase(repo.Object, tenant.Object);

            var resultado = await sut.Handle();

            Assert.That(resultado.Cabeceras, Is.EqualTo(CabecerasEsperadas));
            Assert.That(resultado.Filas, Has.Count.EqualTo(1));

            var fila = resultado.Filas[0];
            Assert.Multiple(() =>
            {
                Assert.That(fila[0], Is.EqualTo(cliente.Documento.Tipo.ToString()));
                Assert.That(fila[2], Is.EqualTo(cliente.RazonSocial!.Valor));
                Assert.That(fila[8], Is.EqualTo(cliente.NombreComercial!.ParaMostrar));
                Assert.That(fila[9], Is.EqualTo(cliente.PaginaWeb!.Valor));
                Assert.That(fila[10], Is.EqualTo(cliente.Observaciones!.Valor));
                Assert.That(fila[11], Is.EqualTo(cliente.DomicilioFiscal!.PaisCodigoIso));
                Assert.That(fila[12], Is.EqualTo(cliente.DomicilioFiscal.Linea));
                Assert.That(fila[13], Is.EqualTo(cliente.DomicilioFiscal.Ubigeo));
                Assert.That(fila[18], Is.EqualTo(cliente.TipoCliente!.Codigo));
                Assert.That(fila[19], Is.EqualTo(cliente.RolCliente!.Codigo));
                Assert.That(fila[20], Is.EqualTo(cliente.FotoPerfil!.NombreArchivo));
                Assert.That(fila[21], Is.EqualTo(cliente.FotoPerfil.UrlPublica));
            });

            repo.Verify(r => r.GetAllAsync(empresaId, It.IsAny<int?>(), It.IsAny<int?>()), Times.Once);
            tenant.VerifyGet(t => t.EmpresaId, Times.Once);
        }

        [Test]
        public void Handle_SinEmpresaActual_LanzaRegla()
        {
            var repo = new Mock<IClienteRepository>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            tenant.SetupGet(t => t.EmpresaId).Returns((EmpresaId)null!);

            var sut = new ExportarClientesCompletoUseCase(repo.Object, tenant.Object);

            Assert.That(async () => await sut.Handle(),
                Throws.TypeOf<BusinessRuleException>()
                    .With.Message.Contains("Empresa"));
        }

        private static Cliente CrearClienteCompleto(EmpresaId empresaId)
        {
            return new Cliente(
                Guid.NewGuid(),
                empresaId,
                DocumentoIdentidad.Crear(TipoDocumento.Ruc, "20600893409"),
                RazonSocial.Crear("Industrias Demo SAC"),
                nombres: null,
                correo: Email.Create("contacto@demo.com"),
                telefono: Telefono.FromTexto("+51 955 444 333"),
                domicilioFiscal: DomicilioFiscal.FromPeru(
                    linea: "Av. Siempre Viva 742",
                    ubigeo: "150101",
                    departamento: "Lima",
                    provincia: "Lima",
                    distrito: "Lima",
                    addressTypeCode: "0000"),
                tipoCliente: TipoCliente.ClienteProveedor,
                rolCliente: RolCliente.Mayorista,
                estado: EstadoCliente.Habilitado,
                nombreComercial: NombreCliente.Crear("Industrias Demo"),
                paginaWeb: PaginaWebCliente.Create("https://demo.pe"),
                observaciones: ObservacionesCliente.Create("Cliente preferente"),
                fotoPerfil: FotoPerfilCliente.Create("demo.png", "https://cdn.demo.pe/demo.png"),
                datosSunat: null);
        }
    }
}
