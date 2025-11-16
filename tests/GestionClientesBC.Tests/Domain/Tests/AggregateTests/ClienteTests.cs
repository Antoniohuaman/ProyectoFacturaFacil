using System;
using System.Linq;
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.Events;
using GestionClientesBC.Domain.ValueObjects;
using NUnit.Framework;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace GestionClientesBC.Tests.Domain.AggregateTests
{
    [TestFixture]
    public class ClienteTests
    {
        // ----------------- Helpers -----------------

        private static EmpresaId CrearEmpresaId()
            => EmpresaId.From(Guid.NewGuid().ToString()); // Ajusta aquí si tu VO se crea distinto

        private static DocumentoIdentidad CrearRuc(string numero = "10000000006")
            => DocumentoIdentidad.Crear(TipoDocumento.Ruc, numero);

        private static DocumentoIdentidad CrearDni(string numero = "12345678")
            => DocumentoIdentidad.Crear(TipoDocumento.Dni, numero);

        private static RazonSocial CrearRazon(string valor = "MI EMPRESA SAC")
            => RazonSocial.Crear(valor);

        private static NombrePersona CrearNombrePersona(
            string nombres = "Juan",
            string apellidos = "Pérez López")
            => NombrePersona.Crear(nombres, apellidos);

        private static Cliente CrearClienteRucBasico()
        {
            var clienteId = Guid.NewGuid();
            var empresaId = CrearEmpresaId();
            var doc = CrearRuc();
            var razon = CrearRazon();

            return new Cliente(
                clienteId,
                empresaId,
                doc,
                razon,
                nombres: null,
                correo: null,
                telefono: null,
                domicilioFiscal: null,
                tipoCliente: TipoCliente.Cliente,
                rolCliente: null,
                estado: EstadoCliente.Habilitado);
        }

        private static Cliente CrearClienteDniBasico()
        {
            var clienteId = Guid.NewGuid();
            var empresaId = CrearEmpresaId();
            var doc = CrearDni();
            var nombres = CrearNombrePersona();

            return new Cliente(
                clienteId,
                empresaId,
                doc,
                razonSocial: null,
                nombres: nombres,
                correo: null,
                telefono: null,
                domicilioFiscal: null,
                tipoCliente: TipoCliente.Cliente,
                rolCliente: null,
                estado: EstadoCliente.Habilitado);
        }

        // ----------------- Tests ctor -----------------

        [Test]
        public void Ctor_RucConRazonSocial_CreaClienteHabilitadoYDisparaEvento()
        {
            var clienteId = Guid.NewGuid();
            var empresaId = CrearEmpresaId();
            var doc = CrearRuc();
            var razon = CrearRazon();

            var cliente = new Cliente(
                clienteId,
                empresaId,
                doc,
                razon,
                nombres: null);

            Assert.That(cliente.ClienteId, Is.EqualTo(clienteId));
            Assert.That(cliente.EmpresaId, Is.EqualTo(empresaId));
            Assert.That(cliente.Documento, Is.EqualTo(doc));
            Assert.That(cliente.RazonSocial, Is.EqualTo(razon));
            Assert.That(cliente.Nombres, Is.Null);
            Assert.That(cliente.Estado, Is.EqualTo(EstadoCliente.Habilitado));
            Assert.That(cliente.FechaRegistro, Is.Not.EqualTo(default(DateTime)));
            Assert.That(cliente.Version, Is.EqualTo(0));

            var evt = cliente.DomainEvents.OfType<ClienteCreado>().SingleOrDefault();
            Assert.That(evt, Is.Not.Null);
            Assert.That(evt!.ClienteId, Is.EqualTo(clienteId));
            Assert.That(evt.EmpresaId, Is.EqualTo(empresaId));
            Assert.That(evt.NumeroDocumento, Is.EqualTo(doc.Numero));
        }

        [Test]
        public void Ctor_RucSinRazonSocial_LanzaArgumentNullException()
        {
            var clienteId = Guid.NewGuid();
            var empresaId = CrearEmpresaId();
            var doc = CrearRuc();

            Assert.That(
                () => new Cliente(
                    clienteId,
                    empresaId,
                    doc,
                    razonSocial: null,
                    nombres: null),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Property("ParamName").EqualTo("razonSocial"));
        }

        [Test]
        public void Ctor_DniSinNombres_LanzaArgumentNullException()
        {
            var clienteId = Guid.NewGuid();
            var empresaId = CrearEmpresaId();
            var doc = CrearDni();

            Assert.That(
                () => new Cliente(
                    clienteId,
                    empresaId,
                    doc,
                    razonSocial: null,
                    nombres: null),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Property("ParamName").EqualTo("nombres"));
        }

        [Test]
        public void Ctor_DniConNombres_CreaClienteCorrectamente()
        {
            var clienteId = Guid.NewGuid();
            var empresaId = CrearEmpresaId();
            var doc = CrearDni();
            var nombres = CrearNombrePersona();

            var cliente = new Cliente(
                clienteId,
                empresaId,
                doc,
                razonSocial: null,
                nombres: nombres);

            Assert.That(cliente.Documento.Tipo, Is.EqualTo(TipoDocumento.Dni));
            Assert.That(cliente.Nombres!.Completo, Is.EqualTo(nombres.Completo));
            Assert.That(cliente.RazonSocial, Is.Null);
            Assert.That(cliente.Estado, Is.EqualTo(EstadoCliente.Habilitado));
        }

        // ----------------- Tests de comportamiento -----------------

        [Test]
        public void Habilitar_CuandoYaEstaHabilitado_LanzaBusinessRuleException()
        {
            var cliente = CrearClienteRucBasico();

            Assert.That(
                () => cliente.Habilitar(),
                Throws.TypeOf<BusinessRuleException>()
                    .With.Message.Contains("ya está habilitado"));
        }

        [Test]
        public void Deshabilitar_CambiaEstado_RegistraMotivoYEvento()
        {
            var cliente = CrearClienteRucBasico();
            var fecha = new DateTime(2025, 1, 1, 10, 30, 0, DateTimeKind.Local);
            var motivo = "Morosidad";

            cliente.Deshabilitar(motivo, fecha);

            Assert.That(cliente.Estado, Is.EqualTo(EstadoCliente.Deshabilitado));
            Assert.That(cliente.MotivoDeshabilitacion, Is.EqualTo(motivo));
            Assert.That(cliente.FechaDeshabilitacion, Is.Not.Null);
            Assert.That(cliente.FechaUltimaModificacion, Is.Not.Null);
            Assert.That(cliente.Version, Is.GreaterThan(0));

            var evt = cliente.DomainEvents.OfType<ClienteDeshabilitado>().SingleOrDefault();
            Assert.That(evt, Is.Not.Null);
            Assert.That(evt!.Motivo, Is.EqualTo(motivo));
        }

        [Test]
        public void ActualizarNombre_ParaRuc_ActualizaRazonSocial()
        {
            var cliente = CrearClienteRucBasico();
            var nuevaRazon = RazonSocial.Crear("NUEVA EMPRESA SAC");

            cliente.ActualizarNombre(nuevaRazon);

            Assert.That(cliente.RazonSocial, Is.EqualTo(nuevaRazon));
            Assert.That(cliente.Nombres, Is.Null);
            Assert.That(cliente.FechaUltimaModificacion, Is.Not.Null);
            Assert.That(cliente.Version, Is.GreaterThan(0));
        }

        [Test]
        public void ActualizarNombre_ParaDni_ActualizaNombrePersona()
        {
            var cliente = CrearClienteDniBasico();
            var nuevoNombre = NombrePersona.Crear("Ana María", "García Torres");

            cliente.ActualizarNombre(nuevoNombre);

            Assert.That(cliente.Nombres, Is.EqualTo(nuevoNombre));
            Assert.That(cliente.RazonSocial, Is.Null);
            Assert.That(cliente.FechaUltimaModificacion, Is.Not.Null);
            Assert.That(cliente.Version, Is.GreaterThan(0));
        }

        [Test]
        public void ActualizarDocumentoIdentidad_CambiarADniSinNombres_LanzaBusinessRuleException()
        {
            // Cliente inicial RUC con razón social
            var clienteId = Guid.NewGuid();
            var empresaId = CrearEmpresaId();
            var docRuc = CrearRuc();
            var razon = CrearRazon();

            var cliente = new Cliente(
                clienteId,
                empresaId,
                docRuc,
                razon,
                nombres: null);

            // Forzamos a que Nombres esté vacío para provocar la regla
            cliente.ActualizarNombre(razon); // sigue siendo PJ, no cambia nada en nombres

            var nuevoDoc = CrearDni("12345678");

            Assert.That(
                () => cliente.ActualizarDocumentoIdentidad(nuevoDoc),
                Throws.TypeOf<BusinessRuleException>()
                    .With.Message.Contains("nombres válidos"));
        }

        [Test]
        public void RegistrarModificacion_AgregaEventoClienteActualizado_EIncrementeVersion()
        {
            var cliente = CrearClienteRucBasico();
            var cambios = new System.Collections.Generic.Dictionary<string, (object? anterior, object? nuevo)>
            {
                { "Correo", (null, "nuevo@correo.com") }
            };

            cliente.RegistrarModificacion(cambios);

            var evt = cliente.DomainEvents.OfType<ClienteActualizado>().SingleOrDefault();
            Assert.That(evt, Is.Not.Null);
            Assert.That(cliente.Version, Is.GreaterThan(0));
        }

        [Test]
        public void EliminarCliente_RegistraEventoClienteEliminado_EIncrementeVersion()
        {
            var cliente = CrearClienteRucBasico();

            cliente.EliminarCliente();

            var evt = cliente.DomainEvents.OfType<ClienteEliminado>().SingleOrDefault();
            Assert.That(evt, Is.Not.Null);
            Assert.That(evt!.ClienteId, Is.EqualTo(cliente.ClienteId));
            Assert.That(cliente.Version, Is.GreaterThan(0));
        }
    }
}
