using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using GestionClientesBC.Domain.Entities;
using NUnit.Framework;
using SharedKernel.ValueObjects;

namespace GestionClientesBC.Tests.Domain.Entities
{
    [TestFixture]
    public class ContactoClienteTests
    {
        // ----------------- Helpers -----------------

        private static NombrePersona AnyNombrePersona()
        {
            return NombrePersona.Crear("Juan", "Pérez");
        }

        private static DocumentoIdentidad Dni(string numero) =>
            DocumentoIdentidad.Crear(TipoDocumento.Dni, numero);

        private static DocumentoIdentidad Ruc(string numero) =>
            DocumentoIdentidad.Crear(TipoDocumento.Ruc, numero);

        private static Email Mail(string v) => Email.Create(v);

        private static Telefono Fono(string v) => Telefono.FromTexto(v);

        // ----------------- Tests de construcción -----------------

        [Test]
        public void Ctor_Valido_InicializaPropiedades_Basicas()
        {
            var id = Guid.NewGuid();
            var nombre = AnyNombrePersona();
            var dni = Dni("12345678");
            var emails = new List<Email> { Mail("a@acme.com"), Mail("b@acme.com") };
            var telefonos = new List<Telefono> { Fono("999 111 222"), Fono("(01) 444 5555") };
            var direccion = "Av. Siempre Viva 742";

            var c = new ContactoCliente(id, nombre, dni, emails, telefonos, direccion);

            Assert.That(c.ContactoId, Is.EqualTo(id));
            Assert.That(c.NombreContacto, Is.Not.Null);
            Assert.That(c.DocumentoIdentidad, Is.Not.Null);
            Assert.That(c.DocumentoIdentidad!.Tipo, Is.EqualTo(TipoDocumento.Dni));
            Assert.That(c.Emails, Has.Count.EqualTo(2));
            Assert.That(c.Telefonos, Has.Count.EqualTo(2));
            Assert.That(c.Direccion, Is.EqualTo(direccion));
            Assert.That(c.FechaCreacion, Is.Not.EqualTo(default(DateTime)));
            Assert.That(c.FechaModificacion, Is.Null);
        }

        [Test]
        public void Ctor_IdVacio_Lanza()
        {
            var nombre = AnyNombrePersona();
            Assert.That(
                () => new ContactoCliente(Guid.Empty, nombre),
                Throws.Exception.TypeOf<ArgumentException>()
                    .With.Message.Contains("El Id no puede ser vacío."));
        }

        [Test]
        public void Ctor_NombreNull_Lanza()
        {
            Assert.That(
                () => new ContactoCliente(Guid.NewGuid(), null!),
                Throws.Exception.TypeOf<ArgumentNullException>()
                    .With.Message.Contains("El nombre de la persona de contacto es obligatorio."));
        }

        [Test]
        public void Ctor_Documento_NoEsDni_Lanza()
        {
            var nombre = AnyNombrePersona();
            var ruc = Ruc("20661287099"); // válido según tu VO
            Assert.That(
                () => new ContactoCliente(Guid.NewGuid(), nombre, ruc),
                Throws.Exception.TypeOf<ArgumentException>()
                    .With.Message.Contains("Solo se permite DNI como documento de contacto secundario."));
        }

        [Test]
        public void Ctor_DocumentoNull_EsPermitido()
        {
            var c = new ContactoCliente(Guid.NewGuid(), AnyNombrePersona(), documentoIdentidad: null);
            Assert.That(c.DocumentoIdentidad, Is.Null);
        }

        [Test]
        public void Ctor_EmailsTelefonosDireccion_Nulos_InicializaColeccionesVacias_Y_DireccionNull()
        {
            var c = new ContactoCliente(Guid.NewGuid(), AnyNombrePersona(), null, null, null, "   ");
            Assert.That(c.Emails, Is.Not.Null);
            Assert.That(c.Emails, Has.Count.EqualTo(0));
            Assert.That(c.Telefonos, Is.Not.Null);
            Assert.That(c.Telefonos, Has.Count.EqualTo(0));
            Assert.That(c.Direccion, Is.Null);
        }

        // ----------------- Tests de actualización -----------------

        [Test]
        public void ActualizarDatos_ReemplazaListasYDireccion_YActualizaTimestamp()
        {
            var c = new ContactoCliente(
                Guid.NewGuid(),
                AnyNombrePersona(),
                Dni("12345678"),
                new List<Email> { Mail("old@acme.com") },
                new List<Telefono> { Fono("900 000 000") },
                "Calle 1");

            var prevCreated = c.FechaCreacion;
            var emailsNew = new List<Email> { Mail("nuevo1@acme.com"), Mail("nuevo2@acme.com") };
            var telefonosNew = new List<Telefono> { Fono("(01) 234 5678") };
            var direccionNew = "Av. Progreso 123";

            c.ActualizarDatos(emailsNew, telefonosNew, direccionNew);

            Assert.That(c.Emails, Is.SameAs(emailsNew));      // reemplazo, no append
            Assert.That(c.Telefonos, Is.SameAs(telefonosNew));// reemplazo, no append
            Assert.That(c.Direccion, Is.EqualTo(direccionNew));
            Assert.That(c.FechaModificacion, Is.Not.Null);
            Assert.That(c.FechaCreacion, Is.EqualTo(prevCreated)); // creación inmutable
        }

        [Test]
        public void ActualizarDatos_Nulos_LimpiaListasYDireccion()
        {
            var c = new ContactoCliente(
                Guid.NewGuid(),
                AnyNombrePersona(),
                Dni("12345678"),
                new List<Email> { Mail("old@acme.com") },
                new List<Telefono> { Fono("900 000 000") },
                "Calle 1");

            c.ActualizarDatos(emails: null, telefonos: null, direccion: "   ");

            Assert.That(c.Emails, Has.Count.EqualTo(0));
            Assert.That(c.Telefonos, Has.Count.EqualTo(0));
            Assert.That(c.Direccion, Is.Null);
            Assert.That(c.FechaModificacion, Is.Not.Null);
        }
    }
}
