using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ComprobantesElectronicosBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace ComprobantesElectronicosBC.Tests.ValueObjects
{
    [TestFixture]
    public class EmisorSnapshotTests
    {
        // --------- Helpers sin APIs obsoletas ---------

        private static T MakeId<T>(string seed)
        {
            var t = typeof(T);

            // Factorías estáticas comunes (string)
            var m = t.GetMethod("From",   BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null)
                  ?? t.GetMethod("Parse",  BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null)
                  ?? t.GetMethod("Create", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
            if (m != null) return (T)m.Invoke(null, new object[] { seed.Trim() })!;

            // Ctor(string)
            var ctorStr = t.GetConstructor(new[] { typeof(string) });
            if (ctorStr != null) return (T)ctorStr.Invoke(new object[] { seed.Trim() });

            // Factorías/ctor Guid (por si tus IDs usan Guid internamente)
            var g = Guid.NewGuid();
            m = t.GetMethod("From", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Guid) }, null);
            if (m != null) return (T)m.Invoke(null, new object[] { g })!;
            var ctorGuid = t.GetConstructor(new[] { typeof(Guid) });
            if (ctorGuid != null) return (T)ctorGuid.Invoke(new object[] { g });

            // New()/NewId()
            m = t.GetMethod("New", BindingFlags.Public | BindingFlags.Static)
              ?? t.GetMethod("NewId", BindingFlags.Public | BindingFlags.Static);
            if (m != null) return (T)m.Invoke(null, Array.Empty<object>())!;

            // Ctor() sin parámetros (para structs o clases con ctor vacío)
            var ctor0 = t.GetConstructor(Type.EmptyTypes);
            if (ctor0 != null) return (T)Activator.CreateInstance(t)!;

            Assert.Inconclusive(
                $"No hay factoría/ctor público para {t.Name}. " +
                $"Expón por favor Create/Parse/From(string) o equivalente para poder probarlo."
            );
            throw new InvalidOperationException(); // no llega
        }

    private static DomicilioFiscal MakeDireccion()
        {
            // Usar valores válidos para Perú
            return DomicilioFiscal.FromPeru(
                linea: "Av. Siempre Viva 123",
                ubigeo: "150101",
                departamento: "Lima",
                provincia: "Lima",
                distrito: "Cercado de Lima",
                addressTypeCode: "0000"
            );
        }

        private static Email? MakeEmail(string value)
        {
            var t = typeof(Email);
            var m = t.GetMethod("Parse",  BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null)
                  ?? t.GetMethod("Create", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
            if (m != null) return (Email)m.Invoke(null, new object[] { value })!;
            var ctor = t.GetConstructor(new[] { typeof(string) });
            return ctor != null ? (Email)ctor.Invoke(new object[] { value }) : null; // opcional
        }

        private static Telefono? MakeTelefono(string value)
        {
            var t = typeof(Telefono);
            var m = t.GetMethod("Parse",  BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null)
                  ?? t.GetMethod("Create", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
            if (m != null) return (Telefono)m.Invoke(null, new object[] { value })!;
            var ctor = t.GetConstructor(new[] { typeof(string) });
            if (ctor != null) return (Telefono)ctor.Invoke(new object[] { value });

            TestContext.WriteLine("Telefono VO no accesible públicamente; el test lo omitirá (prop opcional).");
            return null; // opcional en el snapshot
        }

        private static (EmpresaId empresa, TenantId tenant, EstablecimientoId estab) MakeIds()
        {
            var empresa = MakeId<EmpresaId>("empresa-001");
            var tenant  = MakeId<TenantId>("tenant-001");
            var estab   = MakeId<EstablecimientoId>("est-001");
            return (empresa, tenant, estab);
        }

    private static DomicilioFiscal AnyDireccion() => MakeDireccion();

        // ---------------- PRUEBAS ----------------

        [Test]
        public void Create_construye_y_normaliza_RUC_y_nombres()
        {
            var (empresa, tenant, estab) = MakeIds();
            var dir = AnyDireccion();

            var s = EmisorSnapshot.Create(
                empresa, tenant, estab,
                ruc: "  20-12345678-9  ",
                razonSocial: "   COMPAÑÍA    XYZ   ",
                domicilio: dir,
                nombreComercial: "   Tienda   XYZ   "
            );

            Assert.That(s.EmpresaId, Is.EqualTo(empresa));
            Assert.That(s.TenantId, Is.EqualTo(tenant));
            Assert.That(s.EstablecimientoId, Is.EqualTo(estab));
            Assert.That(s.Ruc, Is.EqualTo("20123456789"));
            Assert.That(s.RazonSocial, Is.EqualTo("COMPAÑÍA XYZ"));
            Assert.That(s.NombreComercial, Is.EqualTo("Tienda XYZ"));
            Assert.That(s.Domicilio, Is.Not.Null);
        }

        [Test]
        public void JsonConstructor_tambien_normaliza_y_asigna()
        {
            var (empresa, tenant, estab) = MakeIds();
            var dir = AnyDireccion();

            var s = new EmisorSnapshot(
                empresaId: empresa,
                tenantId: tenant,
                establecimientoId: estab,
                ruc: "20.123.456-78 9",
                razonSocial: "  EMPRESA    ABC   ",
                domicilio: dir,
                nombreComercial: "  Comercial  ABC   "
            );

            Assert.That(s.Ruc, Is.EqualTo("20123456789"));
            Assert.That(s.RazonSocial, Is.EqualTo("EMPRESA ABC"));
            Assert.That(s.NombreComercial, Is.EqualTo("Comercial ABC"));
            Assert.That(s.Email, Is.Null);
            Assert.That(s.Telefono, Is.Null);
        }

        [Test]
        public void Create_admite_Email_y_Telefono_si_existen_los_VOs()
        {
            var (empresa, tenant, estab) = MakeIds();
            var dir   = AnyDireccion();
            var email = MakeEmail("demo@empresa.com");
            var tel   = MakeTelefono("+51 999 888 777"); // puede ser null

            var s = EmisorSnapshot.Create(
                empresa, tenant, estab,
                ruc: "20123456789",
                razonSocial: "EMPRESA ABC SAC",
                domicilio: dir,
                nombreComercial: "ABC",
                email: email,
                telefono: tel
            );

            Assert.That(s.Email, Is.EqualTo(email));
            if (tel is not null) Assert.That(s.Telefono, Is.EqualTo(tel));
            else                 Assert.That(s.Telefono, Is.Null);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Create_falla_si_RUC_nulo_o_vacio(string? ruc)
        {
            var (empresa, tenant, estab) = MakeIds();
            var dir = AnyDireccion();

            Assert.That(
                () => EmisorSnapshot.Create(empresa, tenant, estab, ruc!, "EMPRESA", dir),
                Throws.TypeOf<ArgumentException>().With.Message.Contains("RUC es obligatorio"));
        }

        [TestCase("2012345678")]
        [TestCase("201234567890")]
        public void Create_falla_si_RUC_no_tiene_11_digitos(string ruc)
        {
            var (empresa, tenant, estab) = MakeIds();
            var dir = AnyDireccion();

            Assert.That(
                () => EmisorSnapshot.Create(empresa, tenant, estab, ruc, "EMPRESA", dir),
                Throws.TypeOf<ArgumentException>().With.Message.Contains("11 dígitos"));
        }

        [Test]
        public void Create_falla_si_prefijo_RUC_invalido()
        {
            var (empresa, tenant, estab) = MakeIds();
            var dir = AnyDireccion();

            Assert.That(
                () => EmisorSnapshot.Create(empresa, tenant, estab, "99123456789", "EMPRESA", dir),
                Throws.TypeOf<ArgumentException>().With.Message.Contains("Prefijo de RUC inválido"));
        }

        [Test]
        public void Create_normaliza_RUC_eliminando_no_digitos()
        {
            var (empresa, tenant, estab) = MakeIds();
            var dir = AnyDireccion();

            var s = EmisorSnapshot.Create(
                empresa, tenant, estab,
                "20.123.456-78 9", "EMPRESA", dir);

            Assert.That(s.Ruc, Is.EqualTo("20123456789"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Create_falla_si_RazonSocial_nula_o_vacia(string? razon)
        {
            var (empresa, tenant, estab) = MakeIds();
            var dir = AnyDireccion();

            Assert.That(
                () => EmisorSnapshot.Create(empresa, tenant, estab, "20123456789", razon!, dir),
                Throws.TypeOf<ArgumentException>().With.Message.Contains("obligatorio"));
        }

        [Test]
        public void Create_trimea_colapsa_y_trunca_NombreComercial()
        {
            var (empresa, tenant, estab) = MakeIds();
            var dir = AnyDireccion();

            var nombreLargo = new string('A', 200);
            var s = EmisorSnapshot.Create(
                empresa, tenant, estab,
                "20123456789", "EMPRESA", dir,
                nombreComercial: "   Tienda    XYZ   " + nombreLargo);

            Assert.That(s.NombreComercial, Is.Not.Null);
            Assert.That(s.NombreComercial!.StartsWith("Tienda XYZ"), Is.True);
            Assert.That(s.NombreComercial!.Length, Is.LessThanOrEqualTo(150));
        }

        [Test]
    public void Create_falla_si_Domicilio_es_null()
        {
            var (empresa, tenant, estab) = MakeIds();

            Assert.That(
                () => EmisorSnapshot.Create(empresa, tenant, estab, "20123456789", "EMPRESA", domicilio: null!),
                Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("domicilio"));
        }

        [Test]
        public void Withers_crean_nuevas_instancias_y_preservan_IDs()
        {
            var (empresa, tenant, estab) = MakeIds();
            var dir = AnyDireccion();

            var baseSnap = EmisorSnapshot.Create(
                empresa, tenant, estab,
                "20123456789", "EMPRESA XYZ", dir, "Comercial");

            var nuevoEmail = MakeEmail("contacto@xyz.com");
            var nuevoTel   = MakeTelefono("+51 900 000 000"); // puede ser null
                // Crear una dirección diferente cambiando algún campo
                var nuevaDir   = DomicilioFiscal.FromPeru(
                    linea: "Calle Falsa 456",
                    ubigeo: "150102",
                    departamento: "Lima",
                    provincia: "Lima",
                    distrito: "San Isidro",
                    addressTypeCode: "0001"
                );
            var nuevoNom   = "  NUEVO   NOMBRE  ";

            var conEmail     = baseSnap.ConEmail(nuevoEmail);
            var conDireccion = baseSnap.ConDomicilio(nuevaDir);
            var conNombre    = baseSnap.ConNombreComercial(nuevoNom);

            Assert.That(conEmail.EmpresaId, Is.EqualTo(baseSnap.EmpresaId));
            Assert.That(conDireccion.TenantId, Is.EqualTo(baseSnap.TenantId));
            Assert.That(conNombre.EstablecimientoId, Is.EqualTo(baseSnap.EstablecimientoId));

            Assert.That(conEmail.Email, Is.EqualTo(nuevoEmail));
            Assert.That(conDireccion.Domicilio, Is.EqualTo(nuevaDir));
            Assert.That(conNombre.NombreComercial, Is.EqualTo("NUEVO NOMBRE"));

            if (nuevoTel is not null)
            {
                var conTelefono = baseSnap.ConTelefono(nuevoTel);
                Assert.That(conTelefono.Telefono, Is.EqualTo(nuevoTel));
                Assert.That(conTelefono.EstablecimientoId, Is.EqualTo(baseSnap.EstablecimientoId));
                Assert.That(baseSnap.Telefono, Is.Not.EqualTo(nuevoTel));
            }

            Assert.That(baseSnap.Email, Is.Not.EqualTo(nuevoEmail));
            Assert.That(baseSnap.Domicilio, Is.Not.EqualTo(nuevaDir));
            Assert.That(baseSnap.NombreComercial, Is.Not.EqualTo("NUEVO NOMBRE"));
        }

        [Test]
    public void ConDomicilio_falla_si_nuevoDomicilio_es_null()
        {
            var (empresa, tenant, estab) = MakeIds();
            var dir = AnyDireccion();

            var baseSnap = EmisorSnapshot.Create(
                empresa, tenant, estab, "20123456789", "EMPRESA", dir);

            Assert.That(
                () => baseSnap.ConDomicilio(null!),
                Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("nuevoDomicilio"));
        }

        [Test]
        public void UBL_helpers_y_ToString_mapean_correctamente()
        {
            var (empresa, tenant, estab) = MakeIds();
            var dir = AnyDireccion();

            var s1 = EmisorSnapshot.Create(
                empresa, tenant, estab, "20123456789", "EMPRESA", dir);
            var s2 = EmisorSnapshot.Create(
                empresa, tenant, estab, "20123456789", "EMPRESA", dir, "Comercial");

            Assert.That(s1.UblCompanyId_SchemeId, Is.EqualTo(EmisorSnapshot.SunatDocTipoRuc));
            Assert.That(s1.UblCompanyId_Value,    Is.EqualTo("20123456789"));
            Assert.That(s1.UblRegistrationName,   Is.EqualTo("EMPRESA"));
            Assert.That(s1.UblCommercialName,     Is.Null);

            Assert.That(s1.ToString(), Is.EqualTo("20123456789 - EMPRESA"));
            Assert.That(s2.ToString(), Is.EqualTo("20123456789 - EMPRESA (\"Comercial\")"));
        }
    }
}
