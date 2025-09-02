using System;
using NUnit.Framework;
using ComprobantesElectronicosBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace ComprobantesElectronicosBC.Tests.UnitTests.ValueObjects
{
    public class ClienteSnapshotTests
    {
        // ===== Helpers (ajusta si tus VOs tienen otra firma) =====
        private static DocumentoIdentidad Doc(string schemeId, string numero)
        {
            var tipo = schemeId switch
            {
                "6" => TipoDocumento.Ruc,
                "1" => TipoDocumento.Dni,
                "4" => TipoDocumento.CarnetExtranjeria,
                "7" => TipoDocumento.Pasaporte,
                "A" => TipoDocumento.CedulaDiplomatica,
                "B" => TipoDocumento.DocIdentidadPaisResidenciaNoDomiciliado,
                "C" => TipoDocumento.TinPersonaNatural,
                "D" => TipoDocumento.InPersonaJuridica,
                _   => throw new ArgumentException($"Tipo de documento no soportado: {schemeId}")
            };
            return DocumentoIdentidad.Crear(tipo, numero);
        }

        private static DomicilioFiscal Domicilio(
            string linea1 = "Av. Los Olivos 123",
            string ubigeo = "150101",
            string distrito = "LIMA",
            string provincia = "LIMA",
            string departamento = "LIMA")
            => DomicilioFiscal.FromPeru(
                linea: linea1,
                ubigeo: ubigeo,
                departamento: departamento,
                provincia: provincia,
                distrito: distrito,
                addressTypeCode: "0000"
            );

        private static Email Mail(string s) => Email.Create(s);
        // =========================================================

        private static EmpresaId Empresa() => EmpresaId.From("EMPRESA-TEST");
        private static TenantId Tenant() => TenantId.FromString("11111111-1111-1111-1111-111111111111");

        [Test]
        public void Create_RucConDireccionYEmail_NormalizaNombre_YExponeUbl()
        {
            var documento = Doc("6", "20100070970"); // RUC válido
            var domicilio = Domicilio();
            var email     = Mail("facturacion@cliente.com");

            var snap = ClienteSnapshot.Create(
                empresaId: Empresa(),
                tenantId: Tenant(),
                documento: documento,
                nombre: "  CLIENTE   S.A.C. ",
                domicilio: domicilio,
                email: email
            );

            // Normalización de nombre
            Assert.That(snap.Nombre, Is.EqualTo("CLIENTE S.A.C."));

            // Asignación de VOs
            Assert.That(snap.Documento, Is.SameAs(documento));
            Assert.That(snap.Domicilio, Is.SameAs(domicilio));
            Assert.That(snap.Email, Is.SameAs(email));

            // Conveniencia
            Assert.That(snap.EsRuc, Is.True);

            // UBL helpers
            Assert.That(snap.UblCompanyId_SchemeId, Is.EqualTo("6"));
            Assert.That(snap.UblCompanyId_Value,    Is.EqualTo("20100070970"));
            Assert.That(snap.UblRegistrationName,   Is.EqualTo("CLIENTE S.A.C."));

            // Multiempresa/multitenant
            Assert.That(snap.EmpresaId, Is.EqualTo(Empresa()));
            Assert.That(snap.TenantId, Is.EqualTo(Tenant()));
        }

        [Test]
        public void Create_ObligatoriosConDni_SinDireccionNiEmail_OK()
        {
            var documento = Doc("1", "12345678"); // DNI válido
            var snap = ClienteSnapshot.Create(Empresa(), Tenant(), documento, " Juan   Pérez ");

            Assert.That(snap.Nombre, Is.EqualTo("Juan Pérez"));
            Assert.That(snap.Domicilio, Is.Null);
            Assert.That(snap.Email, Is.Null);
            Assert.That(snap.EsRuc, Is.False);

            Assert.That(snap.UblCompanyId_SchemeId, Is.EqualTo("1"));
            Assert.That(snap.UblCompanyId_Value,    Is.EqualTo("12345678"));
            Assert.That(snap.EmpresaId, Is.EqualTo(Empresa()));
            Assert.That(snap.TenantId, Is.EqualTo(Tenant()));
        }

        [Test]
        public void Create_DocumentoNull_Lanza()
        {
            Assert.That(
                () => ClienteSnapshot.Create(Empresa(), Tenant(), documento: null!, nombre: "ACME"),
                Throws.TypeOf<System.ArgumentNullException>());
        }

        [Test]
        public void Create_NombreVacioOLlenoDeEspacios_Lanza()
        {
            var doc = Doc("6", "20100070970");

            Assert.That(() => ClienteSnapshot.Create(Empresa(), Tenant(), doc, ""),      Throws.TypeOf<System.ArgumentException>());
            Assert.That(() => ClienteSnapshot.Create(Empresa(), Tenant(), doc, "   "),   Throws.TypeOf<System.ArgumentException>());
        }

        [Test]
        public void Withers_CreanNuevaInstancia_YActualizanCampos()
        {
            var doc  = Doc("6", "20100070970");
            var dom1 = Domicilio();
            var dom2 = Domicilio(linea1: "Jr. Nueva 456");
            var c1   = ClienteSnapshot.Create(Empresa(), Tenant(), doc, "CLIENTE S.A.C.", dom1);

            var c2 = c1.ConDomicilio(dom2);
            Assert.That(c2, Is.Not.SameAs(c1));
            Assert.That(c2.Domicilio, Is.SameAs(dom2));

            var c3 = c2.ConEmail(Mail("ventas@cliente.com"));
            Assert.That(c3.Email!.Value, Is.EqualTo("ventas@cliente.com"));

            var c4 = c3.ConNombre("  NUEVO    NOMBRE  ");
            Assert.That(c4.Nombre, Is.EqualTo("NUEVO NOMBRE"));

            var c5 = c4.ConEmpresaTenant(EmpresaId.From("EMPRESA-2"), TenantId.FromString("22222222-2222-2222-2222-222222222222"));
            Assert.That(c5.EmpresaId, Is.EqualTo(EmpresaId.From("EMPRESA-2")));
            Assert.That(c5.TenantId, Is.EqualTo(TenantId.FromString("22222222-2222-2222-2222-222222222222")));
        }

        [Test]
        public void ToString_IncluyeDocumentoYNombre()
        {
            var snap = ClienteSnapshot.Create(Empresa(), Tenant(), Doc("6", "20100070970"), "CLIENTE S.A.C.");
            var s = snap.ToString();

            // Nuevo formato esperado: "RUC 20100070970 - CLIENTE S.A.C."
            Assert.That(s, Does.Contain("RUC 20100070970"));
            Assert.That(s, Does.Contain("CLIENTE S.A.C."));
        }
    }
}
