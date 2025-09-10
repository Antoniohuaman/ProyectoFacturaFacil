using System;
using NUnit.Framework;
using IndicadoresNegocioBC.Domain.ValueObjects;

namespace IndicadoresNegocioBC.Tests.Domain.ValueObjects
{
    [TestFixture]
    public class MedioNotificacionTests
    {
        [Test]
        public void Estaticos_Deberian_Ser_Validados_Y_Consistentes()
        {
            Assert.That(MedioNotificacion.Correo.Valor, Is.EqualTo("CORREO"));
            Assert.That(MedioNotificacion.Sms.Valor, Is.EqualTo("SMS"));
            Assert.That(MedioNotificacion.Ambos.Valor, Is.EqualTo("AMBOS"));

            Assert.That(MedioNotificacion.Correo.EsCorreo, Is.True);
            Assert.That(MedioNotificacion.Correo.EsSms, Is.False);
            Assert.That(MedioNotificacion.Correo.EsAmbos, Is.False);

            Assert.That(MedioNotificacion.Sms.EsSms, Is.True);
            Assert.That(MedioNotificacion.Ambos.EsAmbos, Is.True);
        }

        [Test]
        public void From_Deberia_Normalizar_Case_Y_Trim()
        {
            var m1 = MedioNotificacion.From(" correo ");
            var m2 = MedioNotificacion.From("CoRrEo");

            Assert.That(m1.Valor, Is.EqualTo("CORREO"));
            Assert.That(m2.Valor, Is.EqualTo("CORREO"));

            // Igualdad por valor (no necesariamente misma referencia)
            Assert.That(m1, Is.EqualTo(MedioNotificacion.Correo));
            Assert.That(m2, Is.EqualTo(MedioNotificacion.Correo));
        }

        [Test]
        public void Predicados_Deberian_Corresponder_Con_Valor()
        {
            var correo = MedioNotificacion.From("CORREO");
            var sms = MedioNotificacion.From("SMS");
            var ambos = MedioNotificacion.From("AMBOS");

            Assert.Multiple(() =>
            {
                Assert.That(correo.EsCorreo, Is.True);
                Assert.That(correo.EsSms, Is.False);
                Assert.That(correo.EsAmbos, Is.False);

                Assert.That(sms.EsCorreo, Is.False);
                Assert.That(sms.EsSms, Is.True);
                Assert.That(sms.EsAmbos, Is.False);

                Assert.That(ambos.EsCorreo, Is.False);
                Assert.That(ambos.EsSms, Is.False);
                Assert.That(ambos.EsAmbos, Is.True);
            });
        }

        [Test]
        public void Igualdad_Operadores_Y_GetHashCode_Deberian_Respetar_Valor()
        {
            var a = MedioNotificacion.From("correo");
            var b = MedioNotificacion.From("CORREO");
            var c = MedioNotificacion.Correo;
            var d = MedioNotificacion.Sms;

            // Equals(object) y Equals(typed)
            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.Equals((object)b), Is.True);
            Assert.That(a.Equals((object)d), Is.False);

            // == y !=
            Assert.That(a == b, Is.True);
            Assert.That(a == c, Is.True);
            Assert.That(a != d, Is.True);

            // Null comparators
            MedioNotificacion? n1 = null;
            MedioNotificacion? n2 = null;
            Assert.That(n1 == n2, Is.True);
            Assert.That(n1 == a, Is.False);
            Assert.That(a == n1, Is.False);

            // HashCode consistente para valores iguales
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
            Assert.That(a.GetHashCode(), Is.EqualTo(c.GetHashCode()));
        }

        [Test]
        public void ToString_Deberia_Retornar_Valor_Normalizado()
        {
            Assert.That(MedioNotificacion.From(" sms ").ToString(), Is.EqualTo("SMS"));
            Assert.That(MedioNotificacion.From("ambos").ToString(), Is.EqualTo("AMBOS"));
        }

        [Test]
        public void From_ValoresInvalidos_Deberia_Lanzar_ArgumentException()
        {
            Assert.That(() => MedioNotificacion.From(""),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => MedioNotificacion.From("   "),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => MedioNotificacion.From("WHATSAPP"),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => MedioNotificacion.From(null!),
                Throws.TypeOf<ArgumentException>());
        }
    }
}
