using System;
using SharedKernel.Exceptions;
using NUnit.Framework;
using ConfiguracionSistemaBC.Domain.Entities;
using ConfiguracionSistemaBC.Domain.ValueObjects; // EstablecimientoId, EmpresaId, EmailEmpresa
using SharedKernel.ValueObjects;                  // DireccionPostal, Telefono

namespace ConfiguracionSistemaBC.Tests.Domain.Entities
{
    [TestFixture]
    public class EstablecimientoTests
    {
        // -------- Helpers de creación de VOs (ajusta a tus APIs reales) --------
    // Helper: acepta solo números de hasta 6 caracteres y los convierte a Guid válido
    private static EstablecimientoId EstId(string v = "001")
    {
        if (string.IsNullOrWhiteSpace(v) || v.Length > 6 || !int.TryParse(v, out _))
            throw new ArgumentException("El código de establecimiento debe ser numérico y hasta 6 dígitos", nameof(v));
        // Convierte el número a Guid: rellena con ceros a la izquierda y lo inserta en el último bloque
        var guidStr = $"00000000-0000-0000-0000-000000{v.PadLeft(6, '0')}";
        return EstablecimientoId.From(Guid.Parse(guidStr));
    }
    private static EmpresaId EmpId(string v = "22222222-2222-2222-2222-222222222222") => EmpresaId.From(v);

        private static DomicilioFiscal Dir() =>
            DomicilioFiscal.FromPeru(
                "Av. Siempre Viva 742",
                ubigeo: "150101",
                departamento: "Lima",
                provincia: "Lima",
                distrito: "Lima"
            );

        private static Telefono Tel() =>
            Telefono.FromTexto("+51 999888777");

        private static Email Mail(string v = "ventas@acme.com") =>
            Email.Create(v);

        // -------------------- Creación --------------------

        [Test]
        public void Ctor_asigna_propiedades_correctamente()
        {
            var id  = EstId("123456");
            var emp = EmpId("EMP01");
            var dir = Dir();
            var tel = Tel();
            var mail = Mail("ventas@miempresa.com");

            var e = new Establecimiento(id, emp, "Tienda Miraflores", "SUC01", dir, tel, mail);

            Assert.That(e.Id, Is.EqualTo(id));
            Assert.That(e.EmpresaId, Is.EqualTo(emp));
            Assert.That(e.Nombre, Is.EqualTo("Tienda Miraflores"));
            Assert.That(e.Codigo, Is.EqualTo("SUC01"));
            Assert.That(e.Direccion, Is.EqualTo(dir));
            Assert.That(e.Telefono, Is.EqualTo(tel));
            Assert.That(e.Email, Is.EqualTo(mail));
        }

        [Test]
        public void Ctor_permite_email_nulo()
        {
            var e = new Establecimiento(EstId(), EmpId(), "Outlet", "OUT01", Dir(), Tel(), null);

            Assert.That(e.Email, Is.Null);
        }

        // -------------------- Validaciones en ctor --------------------

        [Test]
        public void Ctor_con_nombre_vacio_lanza_BusinessRuleException()
        {
            var ex = Assert.Throws<BusinessRuleException>(() =>
                _ = new Establecimiento(EstId(), EmpId(), "   ", "COD1", Dir(), Tel(), Mail()));
            Assert.That(ex!.Message!, Does.Contain("El nombre es obligatorio"));
        }

        [Test]
        public void Ctor_con_codigo_vacio_lanza_BusinessRuleException()
        {
            var ex = Assert.Throws<BusinessRuleException>(() =>
                _ = new Establecimiento(EstId(), EmpId(), "Local", "   ", Dir(), Tel(), Mail()));
            Assert.That(ex!.Message!, Does.Contain("El código es obligatorio"));
        }

        [Test]
        public void Ctor_con_id_nulo_lanza_ArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
                _ = new Establecimiento(null!, EmpId(), "Local", "COD1", Dir(), Tel(), Mail()));

            Assert.That(ex!.ParamName!, Is.EqualTo("id"));
        }

        [Test]
        public void Ctor_con_empresaId_nulo_lanza_ArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
                _ = new Establecimiento(EstId(), null!, "Local", "COD1", Dir(), Tel(), Mail()));

            Assert.That(ex!.ParamName!, Is.EqualTo("empresaId"));
        }

        [Test]
        public void Ctor_con_direccion_nula_lanza_ArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
                _ = new Establecimiento(EstId(), EmpId(), "Local", "COD1", null!, Tel(), Mail()));

            Assert.That(ex!.ParamName!, Is.EqualTo("direccion"));
        }

        [Test]
        public void Ctor_con_telefono_nulo_lanza_ArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
                _ = new Establecimiento(EstId(), EmpId(), "Local", "COD1", Dir(), null!, Mail()));

            Assert.That(ex!.ParamName!, Is.EqualTo("telefono"));
        }

        // -------------------- ActualizarDatos --------------------

        [Test]
        public void ActualizarDatos_actualiza_todos_los_campos_incluido_email_a_null()
        {
            var e = new Establecimiento(EstId(), EmpId(), "Local A", "A01", Dir(), Tel(), Mail("a@acme.com"));

            var nuevaDir = Dir();
            var nuevoTel = Tel();

            e.ActualizarDatos("Local B", "B02", nuevaDir, nuevoTel, null);

            Assert.That(e.Nombre, Is.EqualTo("Local B"));
            Assert.That(e.Codigo, Is.EqualTo("B02"));
            Assert.That(e.Direccion, Is.EqualTo(nuevaDir));
            Assert.That(e.Telefono, Is.EqualTo(nuevoTel));
            Assert.That(e.Email, Is.Null);
        }

        [Test]
        public void ActualizarDatos_con_nombre_vacio_lanza_BusinessRuleException_y_no_cambia_estado_actual()
        {
            var dirOriginal = Dir();
            var telOriginal = Tel();
            var mailOriginal = Mail();
            var e = new Establecimiento(EstId(), EmpId(), "Local A", "A01", dirOriginal, telOriginal, mailOriginal);
            var ex = Assert.Throws<BusinessRuleException>(() =>
                e.ActualizarDatos("  ", "B02", Dir(), Tel(), null));
            Assert.That(ex!.Message!, Does.Contain("El nombre es obligatorio"));
            // No cambió nada
            Assert.That(e.Nombre, Is.EqualTo("Local A"));
            Assert.That(e.Codigo, Is.EqualTo("A01"));
            Assert.That(e.Direccion, Is.EqualTo(dirOriginal));
            Assert.That(e.Telefono, Is.EqualTo(telOriginal));
            Assert.That(e.Email, Is.EqualTo(mailOriginal));
        }

        [Test]
        public void ActualizarDatos_con_codigo_vacio_lanza_BusinessRuleException()
        {
            var e = new Establecimiento(EstId(), EmpId(), "Local A", "A01", Dir(), Tel(), Mail());
            var ex = Assert.Throws<BusinessRuleException>(() =>
                e.ActualizarDatos("Local B", "   ", Dir(), Tel(), Mail()));
            Assert.That(ex!.Message!, Does.Contain("El código es obligatorio"));
        }

        [Test]
        public void ActualizarDatos_con_direccion_nula_lanza()
        {
            var e = new Establecimiento(EstId(), EmpId(), "Local A", "A01", Dir(), Tel(), Mail());

            var ex = Assert.Throws<ArgumentNullException>(() =>
                e.ActualizarDatos("Local B", "B01", null!, Tel(), Mail()));

            Assert.That(ex!.ParamName!, Is.EqualTo("direccion"));
        }

        [Test]
        public void ActualizarDatos_con_telefono_nulo_lanza()
        {
            var e = new Establecimiento(EstId(), EmpId(), "Local A", "A01", Dir(), Tel(), Mail());

            var ex = Assert.Throws<ArgumentNullException>(() =>
                e.ActualizarDatos("Local B", "B01", Dir(), null!, Mail()));

            Assert.That(ex!.ParamName!, Is.EqualTo("telefono"));
        }
    }
}
