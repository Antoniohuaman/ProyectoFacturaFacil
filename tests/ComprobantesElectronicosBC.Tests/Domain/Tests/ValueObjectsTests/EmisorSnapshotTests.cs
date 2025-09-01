// tests/ComprobantesElectronicosBC.Tests/UnitTests/ValueObjects/EmisorSnapshotTests.cs
using NUnit.Framework;
using ComprobantesElectronicosBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace ComprobantesElectronicosBC.Tests.UnitTests.ValueObjects
{
    public class EmisorSnapshotTests
    {
        // ======= Helpers (ajusta a la firma real de tus VOs) =======
        private static DireccionPostal Dir(
            string linea1 = "Av. Los Olivos 123",
            string ubigeo = "150101",
            string distrito = "LIMA",
            string provincia = "LIMA",
            string departamento = "LIMA")
        {
            return DireccionPostal.FromPeru(
                linea: linea1,
                ubigeo: ubigeo,
                departamento: departamento,
                provincia: provincia,
                distrito: distrito,
                addressTypeCode: "0000"
            );
        }

        private static Email Mail(string s) => Email.Create(s);

        // ===========================================================

        [Test]
        public void Create_Valido_NormalizaRucYNombres_YAsignaDireccion()
        {
            var dir = Dir();

            // RUC con separadores -> solo dígitos
            var telefono = Telefono.FromTexto("999-888-777");
            var emisor = EmisorSnapshot.Create(
                ruc: " 20-123456789 ",
                razonSocial: "  ACME   S.A.  ",
                direccion: dir,
                nombreComercial: "  tienda   ACME  ",
                email: Mail("facturacion@acme.com"),
                telefono: telefono
            );

            Assert.That(emisor.Ruc, Is.EqualTo("20123456789"));
            Assert.That(emisor.RazonSocial, Is.EqualTo("ACME S.A."));
            Assert.That(emisor.NombreComercial, Is.EqualTo("tienda ACME"));
            Assert.That(emisor.Direccion, Is.SameAs(dir));
            Assert.That(emisor.Email!.Value, Is.EqualTo("facturacion@acme.com"));
            Assert.That(emisor.Telefono, Is.Not.Null);
            Assert.That(emisor.Telefono, Is.SameAs(telefono));
            Assert.That(emisor.Telefono!.EsVacio, Is.False);
            Assert.That(emisor.Telefono!.Numeros[0].Canonico, Is.EqualTo("999888777"));

            // Helpers UBL
            Assert.That(emisor.UblCompanyId_SchemeId, Is.EqualTo("6"));
            Assert.That(emisor.UblCompanyId_Value, Is.EqualTo("20123456789"));
            Assert.That(emisor.UblRegistrationName, Is.EqualTo("ACME S.A."));
            Assert.That(emisor.UblCommercialName, Is.EqualTo("tienda ACME"));
        }

        [Test]
        public void Create_DireccionNull_Lanza()
        {
            Assert.That(
                () => EmisorSnapshot.Create("20123456789", "ACME S.A.", direccion: null!),
                Throws.TypeOf<System.ArgumentNullException>());
        }

        [Test]
        public void Create_RucConLongitudInvalida_Lanza()
        {
            var dir = Dir();

            Assert.That(
                () => EmisorSnapshot.Create("2012345678", "ACME", dir),       // 10 dígitos
                Throws.TypeOf<System.ArgumentException>());

            Assert.That(
                () => EmisorSnapshot.Create("201234567890", "ACME", dir),     // 12 dígitos
                Throws.TypeOf<System.ArgumentException>());
        }

        [Test]
        public void Create_RucConPrefijoInvalido_Lanza()
        {
            var dir = Dir();

            // Prefijo '23' no permitido (permitidos: 10/15/16/17/20)
            Assert.That(
                () => EmisorSnapshot.Create("23123456789", "ACME", dir),
                Throws.TypeOf<System.ArgumentException>());
        }

        [Test]
        public void Withers_CreanNuevasInstancias_YReemplazanCampos()
        {
            var d1 = Dir();
            var d2 = Dir(linea1: "Jr. Nuevo 456");
            var telefono1 = Telefono.FromTexto("999-888-777");
            var telefono2 = Telefono.FromTexto("555-444-333");
            var emisor = EmisorSnapshot.Create("20123456789", "ACME S.A.", d1, telefono: telefono1);

            var conDir = emisor.ConDireccion(d2);
            Assert.That(conDir, Is.Not.SameAs(emisor));
            Assert.That(conDir.Direccion, Is.SameAs(d2));

            var conNombreCom = conDir.ConNombreComercial("   NUEVA   TIENDA ");
            Assert.That(conNombreCom.NombreComercial, Is.EqualTo("NUEVA TIENDA"));

            var conEmail = conNombreCom.ConEmail(Mail("ventas@acme.com"));
            Assert.That(conEmail.Email!.Value, Is.EqualTo("ventas@acme.com"));

            var conTelefono = conEmail.ConTelefono(telefono2);
            Assert.That(conTelefono.Telefono, Is.Not.Null);
            Assert.That(conTelefono.Telefono, Is.SameAs(telefono2));
            Assert.That(conTelefono.Telefono!.Numeros[0].Canonico, Is.EqualTo("555444333"));
        }

        [Test]
        public void ToString_IncluyeRucYRazonSocial_YNombreComercialSiExiste()
        {
            var dir = Dir();

            var sinCom = EmisorSnapshot.Create("20123456789", "ACME S.A.", dir, telefono: Telefono.FromTexto("999-888-777"));
            Assert.That(sinCom.ToString(), Does.Contain("20123456789").And.Contain("ACME S.A."));

            var conCom = EmisorSnapshot.Create("20123456789", "ACME S.A.", dir, "Tienda", telefono: Telefono.FromTexto("999-888-777"));
            Assert.That(conCom.ToString(), Does.Contain("\"Tienda\""));
        }
    }
}
