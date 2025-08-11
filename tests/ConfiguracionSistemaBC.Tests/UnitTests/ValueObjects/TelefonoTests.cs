using System;
using System.Collections.Generic;
using NUnit.Framework;
using ConfiguracionSistemaBC.Domain.ValueObjects;

namespace ConfiguracionSistemaBC.Tests.UnitTests.ValueObjects
{
    [TestFixture]
    public class TelefonoTests
    {
        [Test]
        public void FromTexto_NullOBlanco_RegresaVacio()
        {
            var a = Telefono.FromTexto(null);
            var b = Telefono.FromTexto("   ");
            Assert.That(a, Is.SameAs(Telefono.Vacio));
            Assert.That(b, Is.SameAs(Telefono.Vacio));
            Assert.That(a.EsVacio, Is.True);
            Assert.That(a.UnirParaMostrar(), Is.EqualTo(string.Empty));
        }

        [Test]
        public void FromTexto_AceptaSeparadoresYNormalizaNumeros()
        {
            // 3 números usando distintos separadores: " / " y " | " y múltiples espacios
            var t = Telefono.FromTexto("+51 999 888 777 / (01) 234-5678  |  01-444 55 66");

            Assert.That(t.Numeros.Count, Is.EqualTo(3));

            // Orden preservado y forma canónica correcta
            Assert.That(t.Numeros[0].Canonico, Is.EqualTo("+51999888777"));
            Assert.That(t.Numeros[0].Mostrar, Is.EqualTo("+51 999 888 777"));

            Assert.That(t.Numeros[1].Canonico, Is.EqualTo("012345678"));
            Assert.That(t.Numeros[1].Mostrar, Is.EqualTo("(01) 234-5678"));

            Assert.That(t.Numeros[2].Canonico, Is.EqualTo("014445566"));
            Assert.That(t.Numeros[2].Mostrar, Is.EqualTo("01-444 55 66"));

            // UnirParaMostrar usa " / " por defecto
            Assert.That(t.UnirParaMostrar(), Is.EqualTo("+51 999 888 777 / (01) 234-5678 / 01-444 55 66"));
        }

        [Test]
        public void FromTexto_TomaGuionConEspaciosComoSeparador()
        {
            // " - " (guion rodeado de espacios) se trata como separador entre teléfonos
            var t = Telefono.FromTexto("999 888 777 - (01) 234 5678 - 0123456");
            Assert.That(t.Numeros.Count, Is.EqualTo(3));
            Assert.That(t.Numeros[0].Canonico, Is.EqualTo("999888777"));
            Assert.That(t.Numeros[1].Canonico, Is.EqualTo("012345678"));
            Assert.That(t.Numeros[2].Canonico, Is.EqualTo("0123456"));
        }

        [Test]
        public void FromTexto_DeduplicaPorFormaCanonica()
        {
            // Los 3 representan el mismo número
            var t = Telefono.FromTexto("999 888 777 / 999888777 / (999) 888-777");
            Assert.That(t.Numeros.Count, Is.EqualTo(1));
            Assert.That(t.Numeros[0].Canonico, Is.EqualTo("999888777"));
        }

        [Test]
        public void FromTexto_MasDeTresTelefonos_Lanza()
        {
            // 4 distintos -> debe lanzar
            Assert.That(
                () => Telefono.FromTexto("999111222 / 999111223 | 999111224 - 999111225"),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void FromTexto_CaracteresInvalidos_Lanza()
        {
            // Letra en medio
            Assert.That(() => Telefono.FromTexto("123A456"), Throws.TypeOf<ArgumentOutOfRangeException>());
            // '+' no al inicio (queda en medio tras limpieza)
            Assert.That(() => Telefono.FromTexto("12+34 56"), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void FromTexto_LongitudInvalida_Lanza()
        {
            // Local < 6
            Assert.That(() => Telefono.FromTexto("12345"), Throws.TypeOf<ArgumentOutOfRangeException>());
            // Local > 15
            Assert.That(() => Telefono.FromTexto("1234567890123456"), Throws.TypeOf<ArgumentOutOfRangeException>());

            // Internacional (+) < 8
            Assert.That(() => Telefono.FromTexto("+1234567"), Throws.TypeOf<ArgumentOutOfRangeException>());
            // Internacional > 15
            Assert.That(() => Telefono.FromTexto("+1234567890123456"), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void TryFromTexto_ComportamientoEsperado()
        {
            Assert.That(Telefono.TryFromTexto("999 888 777 / (01) 234-5678", out var ok), Is.True);
            Assert.That(ok, Is.Not.Null);
            Assert.That(ok!.Numeros.Count, Is.EqualTo(2));

            // Inválido -> false
            Assert.That(Telefono.TryFromTexto("12A3456", out _), Is.False);

            // Más de 3 -> false
            Assert.That(Telefono.TryFromTexto("1/2/3/4", out _), Is.False);

            // Nulo o blanco -> válido y Vacio
            Assert.That(Telefono.TryFromTexto(null, out var v1), Is.True);
            Assert.That(v1, Is.SameAs(Telefono.Vacio));
            Assert.That(Telefono.TryFromTexto("   ", out var v2), Is.True);
            Assert.That(v2, Is.SameAs(Telefono.Vacio));
        }

        [Test]
        public void FromLista_AceptaHastaTres_OMenos_YDeduplica()
        {
            var t = Telefono.FromLista(new[]
            {
                "(01) 234 5678",
                "012345678",      // mismo que el primero (dup)
                "+51 999 888 777"
            });

            Assert.That(t.Numeros.Count, Is.EqualTo(2));
            Assert.That(t.Numeros[0].Canonico, Is.EqualTo("012345678"));
            Assert.That(t.Numeros[1].Canonico, Is.EqualTo("+51999888777"));
        }

        [Test]
        public void FromLista_MasDeTresDistinctos_Lanza()
        {
            Assert.That(() => Telefono.FromLista(new[]
            {
                "0123456", "0123457", "0123458", "0123459"
            }), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void FromLista_Null_LanzaArgumentNull()
        {
            Assert.That(() => Telefono.FromLista(null!), Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void Igualdad_IgnoraOrden_Y_UsaFormasCanonicas()
        {
            var a = Telefono.FromTexto("+51 999 888 777 / (01) 234-5678");
            var b = Telefono.FromTexto(" (01) 234-5678 | +51 999 888 777 "); // mismo set, distinto orden
            var c = Telefono.FromTexto("(01) 234-5678"); // subset

            Assert.That(a.Equals(b), Is.True);
            Assert.That(a == b, Is.True);
            Assert.That(a != b, Is.False);

            Assert.That(a.Equals(c), Is.False);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void ToString_Y_UnirParaMostrar()
        {
            var t = Telefono.FromTexto("999 888 777 / (01) 234-5678");
            var s1 = t.UnirParaMostrar();               // por defecto " / "
            var s2 = t.UnirParaMostrar(" | ");          // custom

            Assert.That(s1, Is.EqualTo("999 888 777 / (01) 234-5678"));
            Assert.That(s2, Is.EqualTo("999 888 777 | (01) 234-5678"));
            Assert.That(t.ToString(), Is.EqualTo(s1)); // ToString delega a UnirParaMostrar()
        }
    }
}