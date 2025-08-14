// tests/ComprobantesElectronicosBC.Tests/UnitTests/ValueObjects/ClienteSnapshotTests.cs
using NUnit.Framework;
using ComprobantesElectronicosBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace ComprobantesElectronicosBC.Tests.UnitTests.ValueObjects
{
    public class ClienteSnapshotTests
    {
        // ===== Helpers (ajusta si tus VOs tienen otra firma) =====
        private static DocumentoIdentidad Doc(string tipo, string numero)
            => DocumentoIdentidad.Create(tipo, numero);

        private static DireccionPostal Dir(
            string linea1 = "Av. Los Olivos 123",
            string ubigeo = "150101",
            string distrito = "LIMA",
            string provincia = "LIMA",
            string departamento = "LIMA")
            => DireccionPostal.Create(ubigeo, linea1, departamento, provincia, distrito);

        private static Email Mail(string s) => Email.Create(s);
        // =========================================================

        [Test]
        public void Create_RucConDireccionYEmail_NormalizaNombre_YExponeUbl()
        {
            var documento = Doc("6", "20100070970"); // RUC válido
            var direccion = Dir();
            var email     = Mail("facturacion@cliente.com");

            var snap = ClienteSnapshot.Create(
                documento: documento,
                nombre: "  CLIENTE   S.A.C. ",
                direccion: direccion,
                email: email
            );

            // Normalización de nombre
            Assert.That(snap.Nombre, Is.EqualTo("CLIENTE S.A.C."));

            // Asignación de VOs
            Assert.That(snap.Documento, Is.SameAs(documento));
            Assert.That(snap.Direccion, Is.SameAs(direccion));
            Assert.That(snap.Email, Is.SameAs(email));

            // Conveniencia
            Assert.That(snap.EsRuc, Is.True);

            // UBL helpers
            Assert.That(snap.UblCompanyId_SchemeId, Is.EqualTo("6"));
            Assert.That(snap.UblCompanyId_Value,    Is.EqualTo("20100070970"));
            Assert.That(snap.UblRegistrationName,   Is.EqualTo("CLIENTE S.A.C."));
        }

        [Test]
        public void Create_ObligatoriosConDni_SinDireccionNiEmail_OK()
        {
            var documento = Doc("1", "12345678"); // DNI válido
            var snap = ClienteSnapshot.Create(documento, " Juan   Pérez ");

            Assert.That(snap.Nombre, Is.EqualTo("Juan Pérez"));
            Assert.That(snap.Direccion, Is.Null);
            Assert.That(snap.Email, Is.Null);
            Assert.That(snap.EsRuc, Is.False);

            Assert.That(snap.UblCompanyId_SchemeId, Is.EqualTo("1"));
            Assert.That(snap.UblCompanyId_Value,    Is.EqualTo("12345678"));
        }

        [Test]
        public void Create_DocumentoNull_Lanza()
        {
            Assert.That(
                () => ClienteSnapshot.Create(documento: null!, nombre: "ACME"),
                Throws.TypeOf<System.ArgumentNullException>());
        }

        [Test]
        public void Create_NombreVacioOLlenoDeEspacios_Lanza()
        {
            var doc = Doc("6", "20100070970");

            Assert.That(() => ClienteSnapshot.Create(doc, ""),      Throws.TypeOf<System.ArgumentException>());
            Assert.That(() => ClienteSnapshot.Create(doc, "   "),   Throws.TypeOf<System.ArgumentException>());
        }

        [Test]
        public void Withers_CreanNuevaInstancia_YActualizanCampos()
        {
            var doc  = Doc("6", "20100070970");
            var dir1 = Dir();
            var dir2 = Dir(linea1: "Jr. Nueva 456");
            var c1   = ClienteSnapshot.Create(doc, "CLIENTE S.A.C.", dir1);

            var c2 = c1.ConDireccion(dir2);
            Assert.That(c2, Is.Not.SameAs(c1));
            Assert.That(c2.Direccion, Is.SameAs(dir2));

            var c3 = c2.ConEmail(Mail("ventas@cliente.com"));
            Assert.That(c3.Email!.Value, Is.EqualTo("ventas@cliente.com"));

            var c4 = c3.ConNombre("  NUEVO    NOMBRE  ");
            Assert.That(c4.Nombre, Is.EqualTo("NUEVO NOMBRE"));
        }

        [Test]
        public void ToString_IncluyeDocumentoYNombre()
        {
            var snap = ClienteSnapshot.Create(Doc("6", "20100070970"), "CLIENTE S.A.C.");
            var s = snap.ToString();

            Assert.That(s, Does.Contain("6-20100070970"));
            Assert.That(s, Does.Contain("CLIENTE S.A.C."));
        }
    }
}
