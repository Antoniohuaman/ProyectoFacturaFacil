using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GestionClientesBC.Application.Clientes.Exportar.Basico;
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.ValueObjects;
using GestionClientesBC.Domain.Repositories;
using Moq;
using NUnit.Framework;
using SharedKernel.Application.Interfaces;
using SharedKernel.ValueObjects;

namespace GestionClientesBC.Tests.Application.Clientes.Exportar
{
    [TestFixture]
    public class ExportarClientesBasicoUseCaseTests
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
            "Telefonos"
        };

        [Test]
        public async Task Handle_RetornaCabecerasYFilasOrdenadasPorNombre()
        {
            var empresaId = EmpresaId.From("EMPRESA-TEST-BC");
            var repo = new Mock<IClienteRepository>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);

            var clienteRuc = CrearClienteRuc(empresaId, "20661287099", "Zeta SAC");
            var clienteDni = CrearClienteDni(empresaId, "Ana", "Solis");

            var clientes = new List<Cliente> { clienteRuc, clienteDni };

            tenant.SetupGet(t => t.EmpresaId).Returns(empresaId);
            repo.Setup(r => r.GetAllAsync(empresaId, It.IsAny<int?>(), It.IsAny<int?>()))
                .ReturnsAsync(clientes);

            var sut = new ExportarClientesBasicoUseCase(repo.Object, tenant.Object);

            var resultado = await sut.Handle();

            Assert.That(resultado.Cabeceras, Is.EqualTo(CabecerasEsperadas));
            Assert.That(resultado.Filas.Count, Is.EqualTo(2));

            var primeraFila = resultado.Filas[0]; // Debe ser la persona natural (Ana) por orden
            Assert.Multiple(() =>
            {
                Assert.That(primeraFila[0], Is.EqualTo(TipoDocumento.Dni.ToString()));
                Assert.That(primeraFila[1], Is.EqualTo(clienteDni.Documento.Numero));
                Assert.That(primeraFila[2], Is.Empty);
                Assert.That(primeraFila[3], Is.EqualTo("Ana"));
                Assert.That(primeraFila[4], Is.EqualTo("Solis"));
                Assert.That(primeraFila[5], Is.EqualTo(clienteDni.Nombres!.Completo));
                Assert.That(primeraFila[6], Is.EqualTo(clienteDni.Correo!.Value));
                Assert.That(primeraFila[7], Is.EqualTo(clienteDni.Telefono!.UnirParaMostrar()));
            });

            var segundaFila = resultado.Filas[1];
            Assert.Multiple(() =>
            {
                Assert.That(segundaFila[0], Is.EqualTo(TipoDocumento.Ruc.ToString()));
                Assert.That(segundaFila[1], Is.EqualTo(clienteRuc.Documento.Numero));
                Assert.That(segundaFila[2], Is.EqualTo(clienteRuc.RazonSocial!.Valor));
                Assert.That(segundaFila[3], Is.Empty);
                Assert.That(segundaFila[4], Is.Empty);
                Assert.That(segundaFila[5], Is.Empty);
            });

            repo.Verify(r => r.GetAllAsync(empresaId, It.IsAny<int?>(), It.IsAny<int?>()), Times.Once);
            tenant.VerifyGet(t => t.EmpresaId, Times.Once);
        }

        private static Cliente CrearClienteRuc(EmpresaId empresaId, string numeroRuc, string razon)
        {
            return new Cliente(
                Guid.NewGuid(),
                empresaId,
                DocumentoIdentidad.Crear(TipoDocumento.Ruc, numeroRuc),
                RazonSocial.Crear(razon),
                nombres: null,
                correo: Email.Create("ventas@zeta.com"),
                telefono: Telefono.FromTexto("+51 999 123 456"),
                domicilioFiscal: null,
                tipoCliente: TipoCliente.Cliente,
                rolCliente: null,
                estado: EstadoCliente.Habilitado);
        }

        private static Cliente CrearClienteDni(EmpresaId empresaId, string nombres, string apellidos)
        {
            var nombrePersona = NombrePersona.Crear(nombres, apellidos);

            return new Cliente(
                Guid.NewGuid(),
                empresaId,
                DocumentoIdentidad.Crear(TipoDocumento.Dni, "12345678"),
                razonSocial: null,
                nombres: nombrePersona,
                correo: Email.Create("ana@demo.com"),
                telefono: Telefono.FromTexto("+51 988 111 222"),
                domicilioFiscal: null,
                tipoCliente: TipoCliente.Cliente,
                rolCliente: null,
                estado: EstadoCliente.Habilitado);
        }
    }
}
