using System;
using NUnit.Framework;
using IndicadoresNegocioBC.Domain.ValueObjects;

namespace IndicadoresNegocioBC.Tests.Domain.ValueObjects
{
    [TestFixture]
    public class HorarioNotificacionTests
    {
        [Test]
        public void FromHorasMinutos_Deberia_Crear_Valores_Validos_Bordes()
        {
            var h0 = HorarioNotificacion.FromHorasMinutos(0, 0);     // 00:00
            var h1 = HorarioNotificacion.FromHorasMinutos(23, 59);   // 23:59

            Assert.Multiple(() =>
            {
                Assert.That(h0.Hora, Is.EqualTo(new TimeSpan(0, 0, 0)));
                Assert.That(h1.Hora, Is.EqualTo(new TimeSpan(23, 59, 0)));

                Assert.That(h0.ToString(), Is.EqualTo("00:00"));
                Assert.That(h1.ToString(), Is.EqualTo("23:59"));
            });
        }

        [Test]
        public void CtorYFactory_Deberian_Lanzar_En_HorasFueraDeRango_O_MinutosInvalidos()
        {
            // hora negativa
            Assert.That(() => new HorarioNotificacion(TimeSpan.FromHours(-1)),
                Throws.TypeOf<ArgumentOutOfRangeException>());

            // 24:00 (>= 24h) inválido por la regla del VO
            Assert.That(() => HorarioNotificacion.FromHorasMinutos(24, 0),
                Throws.TypeOf<ArgumentOutOfRangeException>());

            // minutos 60: lo lanza el ctor de TimeSpan
            Assert.That(() => HorarioNotificacion.FromHorasMinutos(0, 60),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Coincide_Deberia_Ignorar_Segundos_Y_Machar_HoraYMinuto()
        {
            var h = HorarioNotificacion.FromHorasMinutos(8, 30);

            var tOk1 = new DateTimeOffset(2025, 5, 1, 8, 30, 0, TimeSpan.Zero);
            var tOk2 = new DateTimeOffset(2025, 5, 1, 8, 30, 59, TimeSpan.FromHours(-5)); // zona distinta, segundos distintos
            var tBad = new DateTimeOffset(2025, 5, 1, 8, 31, 0, TimeSpan.Zero);

            Assert.Multiple(() =>
            {
                Assert.That(h.Coincide(tOk1), Is.True);
                Assert.That(h.Coincide(tOk2), Is.True);
                Assert.That(h.Coincide(tBad), Is.False);
            });
        }

        [Test]
        public void ToString_Deberia_Formatear_hh_mm_CeroIzquierda()
        {
            var h = HorarioNotificacion.FromHorasMinutos(7, 5);
            Assert.That(h.ToString(), Is.EqualTo("07:05"));

            // Si los segundos existen en Hora, el formato igual es hh:mm
            var h2 = new HorarioNotificacion(new TimeSpan(9, 0, 30));
            Assert.That(h2.ToString(), Is.EqualTo("09:00"));
        }

        [Test]
        public void Igualdad_Operadores_Y_HashCode_Deberian_Basarse_En_TimeSpan()
        {
            var a = HorarioNotificacion.FromHorasMinutos(9, 0);
            var b = HorarioNotificacion.FromHorasMinutos(9, 0);
            var c = HorarioNotificacion.FromHorasMinutos(10, 0);

            // Equals (typed y object)
            Assert.Multiple(() =>
            {
                Assert.That(a.Equals(b), Is.True);
                Assert.That(a.Equals((object)b), Is.True);
                Assert.That(a.Equals(c), Is.False);
            });

            // Operadores == y != (incluyendo nulos)
            HorarioNotificacion? n1 = null;
            HorarioNotificacion? n2 = null;

            Assert.Multiple(() =>
            {
                Assert.That(a == b, Is.True);
                Assert.That(a != c, Is.True);
                Assert.That(n1 == n2, Is.True);
                Assert.That(a == n1, Is.False);
                Assert.That(n1 == a, Is.False);
            });

            // Hash codes iguales para valores iguales
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void Propiedad_Hora_Deberia_Conservar_El_TimeSpan_Pasado()
        {
            var ts = new TimeSpan(21, 45, 0);
            var h = new HorarioNotificacion(ts);
            Assert.That(h.Hora, Is.EqualTo(ts));
        }
    }
}
