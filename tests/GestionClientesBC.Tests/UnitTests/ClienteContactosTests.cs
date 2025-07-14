using System;
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.Entities;
using GestionClientesBC.Domain.ValueObjects;
using NUnit.Framework;

namespace GestionClientesBC.Tests.UnitTests
{
    public class ClienteContactosTests
    {
        private Cliente CrearCliente()
        {
            return new Cliente(
                Guid.NewGuid(),
                new DocumentoIdentidad(TipoDocumento.DNI, "12345678"),
                "Cliente Test",
                "cliente@mail.com",
                "999999999",
                "Calle 1",
                TipoCliente.Minorista,
                EstadoCliente.Activo
            );
        }

        [Test]
        public void AgregarContacto_Valido_SeAgrega()
        {
            var cliente = CrearCliente();
            var contacto = new ContactoCliente(Guid.NewGuid(), TipoContacto.EMAIL_SECUNDARIO, "otro@mail.com");

            cliente.AgregarContacto(contacto);

            Assert.That(cliente.Contactos, Has.Exactly(1).EqualTo(contacto));
        }

        [Test]
        public void AgregarContacto_Duplicado_LanzaExcepcion()
        {
            var cliente = CrearCliente();
            var contacto1 = new ContactoCliente(Guid.NewGuid(), TipoContacto.EMAIL_SECUNDARIO, "otro@mail.com");
            var contacto2 = new ContactoCliente(Guid.NewGuid(), TipoContacto.EMAIL_SECUNDARIO, "otro@mail.com");

            cliente.AgregarContacto(contacto1);
            Assert.Throws<InvalidOperationException>(() => cliente.AgregarContacto(contacto2));
        }

        [Test]
        public void EditarContacto_Existente_CambiaValor()
        {
            var cliente = CrearCliente();
            var contacto = new ContactoCliente(Guid.NewGuid(), TipoContacto.TELEFONO_SECUNDARIO, "999888777");
            cliente.AgregarContacto(contacto);

            cliente.EditarContacto(contacto.ContactoId, "111222333");

            Assert.That(contacto.Valor, Is.EqualTo("111222333"));
        }

        [Test]
        public void EditarContacto_NoExistente_LanzaExcepcion()
        {
            var cliente = CrearCliente();
            Assert.Throws<InvalidOperationException>(() => cliente.EditarContacto(Guid.NewGuid(), "nuevo"));
        }

        [Test]
        public void EliminarContacto_Existente_SeElimina()
        {
            var cliente = CrearCliente();
            var contacto = new ContactoCliente(Guid.NewGuid(), TipoContacto.DIRECCION, "Calle Nueva 123");
            cliente.AgregarContacto(contacto);

            cliente.EliminarContacto(contacto.ContactoId);

            Assert.That(cliente.Contactos, Is.Empty);
        }

        [Test]
        public void EliminarContacto_NoExistente_LanzaExcepcion()
        {
            var cliente = CrearCliente();
            Assert.Throws<InvalidOperationException>(() => cliente.EliminarContacto(Guid.NewGuid()));
        }
    }
}