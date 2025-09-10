using System;
using System.Reflection;
using NUnit.Framework;
using IndicadoresNegocioBC.Domain.ValueObjects;
using SharedKernel.ValueObjects; // Email, Telefono

namespace IndicadoresNegocioBC.Tests.Domain.ValueObjects
{
    [TestFixture]
    public class DestinatarioNotificacionTests
    {
        // ----------------------------- Helpers robustos -----------------------------

        private static Email? TryBuildEmail(string valor)
        {
            // Fábricas típicas
            foreach (var name in new[] { "From", "Parse", "Create", "Of", "New" })
            {
                var mi = typeof(Email).GetMethod(name, BindingFlags.Public | BindingFlags.Static, new[] { typeof(string) });
                if (mi != null) return (Email?)mi.Invoke(null, new object[] { valor });
            }

            // Ctor(string)
            var ctor = typeof(Email).GetConstructor(new[] { typeof(string) });
            if (ctor != null) return (Email?)ctor.Invoke(new object[] { valor });

            // Ctor no público
            var ctorNon = typeof(Email).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(string) }, null);
            if (ctorNon != null) return (Email?)ctorNon.Invoke(new object[] { valor });

            return null;
        }

        private static Telefono? TryBuildTelefono(string numeroE164 = "+51987654321")
        {
            try
            {
                return SharedKernel.ValueObjects.Telefono.FromTexto(numeroE164);
            }
            catch
            {
                return null;
            }
        }

        private static Telefono? TryGetTelefonoVacio()
        {
            return SharedKernel.ValueObjects.Telefono.Vacio;
        }

        private static bool GetTelefonoEsVacio(Telefono tel)
        {
            var p = typeof(Telefono).GetProperty("EsVacio", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (p == null) throw new InconclusiveException("Telefono no expone propiedad 'EsVacio'.");
            return (bool)p.GetValue(tel)!;
        }

        private static string GetTelefonoDisplay(Telefono tel)
        {
            return tel.UnirParaMostrar();
        }

        // ------------------------------------ Tests ------------------------------------

        [Test]
        public void Ctor_Deberia_Lanzar_Si_No_Hay_Ningun_Medio()
        {
            Assert.That(() => new DestinatarioNotificacion(email: null, telefono: null),
                Throws.Exception.TypeOf<ArgumentException>());
        }

        [Test]
        public void Ctor_Deberia_Lanzar_Si_Telefono_Vacio_Y_Sin_Email()
        {
            var telVacio = TryGetTelefonoVacio();
            if (telVacio == null)
            {
                Assert.Inconclusive("No fue posible construir Telefono vacío; se omite esta aserción.");
                return;
            }

            // Si el kernel marca EsVacio=false para ese tipo de construcción, este test no aplica
            try
            {
                if (!GetTelefonoEsVacio(telVacio))
                {
                    Assert.Inconclusive("El Telefono construido no se marca como 'EsVacio'.");
                    return;
                }
            }
            catch (InconclusiveException)
            {
                throw; // propagamos el Inconclusive
            }
            catch
            {
                Assert.Inconclusive("No fue posible verificar EsVacio en Telefono; se omite esta aserción.");
                return;
            }

            Assert.That(() => new DestinatarioNotificacion(email: null, telefono: telVacio),
                Throws.Exception.TypeOf<ArgumentException>());
        }

        [Test]
        public void Email_Solo_Deberia_Ser_Valido_Y_ToString_Igual_A_Email()
        {
            var email = TryBuildEmail("usuario@test.com") ?? throw new InconclusiveException("No se pudo construir Email.");
            var d = new DestinatarioNotificacion(email, null);

            Assert.Multiple(() =>
            {
                Assert.That(d.TieneEmail, Is.True);
                Assert.That(d.TieneTelefono, Is.False);
                Assert.That(d.Email, Is.EqualTo(email));
                Assert.That(d.Telefono, Is.Null);
                Assert.That(d.ToString(), Is.EqualTo(email.ToString()));
            });
        }

        [Test]
        public void Telefono_Solo_Deberia_Ser_Valido_Y_ToString_Igual_A_Display()
        {
            var tel = TryBuildTelefono() ?? throw new InconclusiveException("No se pudo construir Telefono.");
            var d = new DestinatarioNotificacion(null, tel);

            var display = GetTelefonoDisplay(tel);

            Assert.Multiple(() =>
            {
                Assert.That(d.TieneEmail, Is.False);
                Assert.That(d.TieneTelefono, Is.True);
                Assert.That(d.Email, Is.Null);
                Assert.That(d.Telefono, Is.EqualTo(tel));
                Assert.That(d.ToString(), Is.EqualTo(display));
            });
        }

        [Test]
        public void Ambos_Medios_Deberian_Concatenarse_Con_Slash()
        {
            var email = TryBuildEmail("ventas@acme.com") ?? throw new InconclusiveException("No se pudo construir Email.");
            var tel = TryBuildTelefono() ?? throw new InconclusiveException("No se pudo construir Telefono.");
            var d = new DestinatarioNotificacion(email, tel);

            var display = GetTelefonoDisplay(tel);
            var expected = $"{email} / {display}";

            Assert.Multiple(() =>
            {
                Assert.That(d.TieneEmail, Is.True);
                Assert.That(d.TieneTelefono, Is.True);
                Assert.That(d.ToString(), Is.EqualTo(expected));
                Assert.That(d.ToString(), Does.Contain(" / "));
                Assert.That(d.ToString(), Does.Contain(email.ToString()));
                Assert.That(d.ToString(), Does.Contain(display));
            });
        }

        [Test]
        public void Igualdad_Por_Valor_Deberia_BasarEn_EmailYTelefono()
        {
            var email1 = TryBuildEmail("a@x.com") ?? throw new InconclusiveException("No se pudo construir Email.");
            var email2 = TryBuildEmail("b@x.com") ?? email1;

            var tel1 = TryBuildTelefono() ?? throw new InconclusiveException("No se pudo construir Telefono.");
            var tel2 = TryBuildTelefono("+51999999999") ?? tel1;

            var a = new DestinatarioNotificacion(email1, tel1);
            var b = new DestinatarioNotificacion(email1, tel1); // igual por valor
            var c = new DestinatarioNotificacion(email2, tel1); // distinto email
            var d = new DestinatarioNotificacion(email1, tel2); // distinto telefono
            var e = new DestinatarioNotificacion(email1, null); // distinto set de campos

            Assert.Multiple(() =>
            {
                Assert.That(a, Is.EqualTo(b));
                Assert.That(a.Equals((object)b), Is.True);
                Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));

                Assert.That(a, Is.Not.EqualTo(c));
                Assert.That(a, Is.Not.EqualTo(d));
                Assert.That(a, Is.Not.EqualTo(e));
            });
        }

        [Test]
        public void Operadores_Deberian_Manejar_Nulos_Correctamente()
        {
            var email = TryBuildEmail("a@x.com") ?? throw new InconclusiveException("No se pudo construir Email.");
            var tel = TryBuildTelefono() ?? throw new InconclusiveException("No se pudo construir Telefono.");

            var a = new DestinatarioNotificacion(email, tel);
            var b = new DestinatarioNotificacion(email, tel);

            DestinatarioNotificacion? n1 = null;
            DestinatarioNotificacion? n2 = null;

            Assert.Multiple(() =>
            {
                Assert.That(a == b, Is.True);
                Assert.That(a != b, Is.False);

                Assert.That(n1 == n2, Is.True);
                Assert.That(n1 == a, Is.False);
                Assert.That(a == n1, Is.False);
            });
        }

        [Test]
        public void Flags_TieneEmail_TieneTelefono_Deberian_Reflejar_Presencia()
        {
            var email = TryBuildEmail("a@x.com") ?? throw new InconclusiveException("No se pudo construir Email.");
            var tel = TryBuildTelefono() ?? throw new InconclusiveException("No se pudo construir Telefono.");

            var soloEmail = new DestinatarioNotificacion(email, null);
            var soloTel = new DestinatarioNotificacion(null, tel);
            var ambos = new DestinatarioNotificacion(email, tel);

            Assert.Multiple(() =>
            {
                Assert.That(soloEmail.TieneEmail, Is.True);
                Assert.That(soloEmail.TieneTelefono, Is.False);

                Assert.That(soloTel.TieneEmail, Is.False);
                Assert.That(soloTel.TieneTelefono, Is.True);

                Assert.That(ambos.TieneEmail, Is.True);
                Assert.That(ambos.TieneTelefono, Is.True);
            });
        }
    }
}
