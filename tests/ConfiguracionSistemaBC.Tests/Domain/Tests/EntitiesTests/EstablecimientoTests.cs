using System;
using NUnit.Framework;
using ConfiguracionSistemaBC.Domain.Entities;
using SharedKernel.ValueObjects;                   // DireccionPostal, Telefono (ajusta si están en otro namespace)
using ConfiguracionSistemaBC.Domain.ValueObjects; // EmailEmpresa (ajusta si aplica)

namespace ConfiguracionSistemaBC.Tests.Domain.Entities
{
    [TestFixture]
    public class EstablecimientoTests
    {
        // --- Helpers / Fábricas de VOs (ajusta a tus APIs reales) ---
        private static DireccionPostal Dir() =>
            DireccionPostal.From("PE", "Av. Siempre Viva 742", "150101", "Lima", "Lima", "15000", null, null, null);

        private static Telefono Tel() =>
            Telefono.FromTexto("+51 999888777");

        private static EmailEmpresa Mail(string v = "ventas@miempresa.com") =>
            EmailEmpresa.From(v);

        // ----------------- Creación -----------------

        [Test]
        public void Ctor_con_Id_vacio_genera_nuevo_Id()
        {
            var e = new Establecimiento(Guid.Empty, "Tienda Miraflores", "SUC01", Dir(), Tel(), Mail());

            Assert.That(e.Id, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void Ctor_con_Id_explicito_lo_respeta()
        {
            var id = Guid.NewGuid();

            var e = new Establecimiento(id, "Tienda Centro", "CTR01", Dir(), Tel(), null);

            Assert.That(e.Id, Is.EqualTo(id));
        }

        [Test]
        public void Ctor_asigna_propiedades_correctamente()
        {
            var id = Guid.NewGuid();
            var dir = Dir();
            var tel = Tel();
            var mail = Mail("ventas@acme.com");

            var e = new Establecimiento(id, "Local Online", "WEB01", dir, tel, mail);

            Assert.That(e.Nombre, Is.EqualTo("Local Online"));
            Assert.That(e.Codigo, Is.EqualTo("WEB01"));
            Assert.That(e.Direccion, Is.EqualTo(dir));
            Assert.That(e.Telefono, Is.EqualTo(tel));
            Assert.That(e.Email, Is.EqualTo(mail));
        }

        [Test]
        public void Ctor_permite_Email_nulo()
        {
            var e = new Establecimiento(Guid.NewGuid(), "Outlet", "OUT01", Dir(), Tel(), null);

            Assert.That(e.Email, Is.Null);
        }

        // ----------------- Validaciones: ctor -----------------

        [Test]
        public void Ctor_con_nombre_vacio_lanza_ArgumentException_con_paramName()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                _ = new Establecimiento(Guid.NewGuid(), "   ", "COD1", Dir(), Tel(), Mail()));

            Assert.That(ex!.ParamName, Is.EqualTo("nombre"));
            Assert.That(ex.Message, Does.Contain("obligatorio"));
        }

        [Test]
        public void Ctor_con_codigo_vacio_lanza_ArgumentException_con_paramName()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                _ = new Establecimiento(Guid.NewGuid(), "Almacén", "   ", Dir(), Tel(), Mail()));

            Assert.That(ex!.ParamName, Is.EqualTo("codigo"));
            Assert.That(ex.Message, Does.Contain("obligatorio"));
        }

        [Test]
        public void Ctor_con_direccion_nula_lanza_ArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
                _ = new Establecimiento(Guid.NewGuid(), "Depósito", "DEP01", null!, Tel(), Mail()));

            Assert.That(ex!.ParamName, Is.EqualTo("direccion"));
        }

        [Test]
        public void Ctor_con_telefono_nulo_lanza_ArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
                _ = new Establecimiento(Guid.NewGuid(), "Depósito", "DEP01", Dir(), null!, Mail()));

            Assert.That(ex!.ParamName, Is.EqualTo("telefono"));
        }

        // ----------------- ActualizarDatos -----------------

        [Test]
        public void ActualizarDatos_actualiza_todos_los_campos_incluido_email_nulo()
        {
            var e = new Establecimiento(Guid.NewGuid(), "Local A", "A01", Dir(), Tel(), Mail("a@acme.com"));

            var nuevaDir = Dir(); // si tu VO es inmutable, crea uno nuevo con otros datos si quieres
            var nuevoTel = Tel();
            e.ActualizarDatos("Local B", "B02", nuevaDir, nuevoTel, null);

            Assert.That(e.Nombre, Is.EqualTo("Local B"));
            Assert.That(e.Codigo, Is.EqualTo("B02"));
            Assert.That(e.Direccion, Is.EqualTo(nuevaDir));
            Assert.That(e.Telefono, Is.EqualTo(nuevoTel));
            Assert.That(e.Email, Is.Null);
        }

        [Test]
        public void ActualizarDatos_con_nombre_vacio_lanza_ArgumentException_y_no_toca_propiedades()
        {
            var dirOriginal = Dir();
            var telOriginal = Tel();
            var mailOriginal = Mail("ventas@acme.com");
            var e = new Establecimiento(Guid.NewGuid(), "Local A", "A01", dirOriginal, telOriginal, mailOriginal);

            var dirNueva = Dir();
            var telNuevo = Tel();

            var ex = Assert.Throws<ArgumentException>(() =>
                e.ActualizarDatos("  ", "B02", dirNueva, telNuevo, null));

            Assert.That(ex!.ParamName, Is.EqualTo("nombre"));

            // propiedades no cambiaron
            Assert.That(e.Nombre, Is.EqualTo("Local A"));
            Assert.That(e.Codigo, Is.EqualTo("A01"));
            Assert.That(e.Direccion, Is.EqualTo(dirOriginal));
            Assert.That(e.Telefono, Is.EqualTo(telOriginal));
            Assert.That(e.Email, Is.EqualTo(mailOriginal));
        }

        [Test]
        public void ActualizarDatos_con_codigo_vacio_lanza_ArgumentException()
        {
            var e = new Establecimiento(Guid.NewGuid(), "Local A", "A01", Dir(), Tel(), Mail("ventas@acme.com"));

            var ex = Assert.Throws<ArgumentException>(() =>
                e.ActualizarDatos("Local A2", "   ", Dir(), Tel(), Mail("ventas2@acme.com")));

            Assert.That(ex!.ParamName, Is.EqualTo("codigo"));
        }

        [Test]
        public void ActualizarDatos_con_direccion_nula_lanza_ArgumentNullException()
        {
            var e = new Establecimiento(Guid.NewGuid(), "Local A", "A01", Dir(), Tel(), Mail());

            var ex = Assert.Throws<ArgumentNullException>(() =>
                e.ActualizarDatos("Local B", "B01", null!, Tel(), Mail()));

            Assert.That(ex!.ParamName, Is.EqualTo("direccion"));
        }

        [Test]
        public void ActualizarDatos_con_telefono_nulo_lanza_ArgumentNullException()
        {
            var e = new Establecimiento(Guid.NewGuid(), "Local A", "A01", Dir(), Tel(), Mail());

            var ex = Assert.Throws<ArgumentNullException>(() =>
                e.ActualizarDatos("Local B", "B01", Dir(), null!, Mail()));

            Assert.That(ex!.ParamName, Is.EqualTo("telefono"));
        }
    }
}
