using System;
using IndicadoresNegocioBC.Domain.ValueObjects;
using NUnit.Framework;

namespace IndicadoresNegocioBC.Tests.UnitTests.ValueObjects
{
    public class PeriodoTests
    {
        // ---------------------- Fábricas alineadas ----------------------

        [Test]
        public void PorDia_CreaPeriodoDeUnDia_Alineado()
        {
            var dia = new DateOnly(2025, 8, 13);
            var p = Periodo.PorDia(dia);

            Assert.Multiple(() =>
            {
                Assert.That(p.Inicio, Is.EqualTo(dia));
                Assert.That(p.FinInclusive, Is.EqualTo(dia));
                Assert.That(p.Dias, Is.EqualTo(1));
                Assert.That(p.FinExclusivo, Is.EqualTo(dia.AddDays(1)));
                Assert.That(p.Granularidad, Is.SameAs(Granularidad.Dia));
            });

            Assert.DoesNotThrow(p.AsegurarAlineado);
        }

        [Test]
        public void PorSemana_LunesComoInicio_AlineaYDuracion7Dias()
        {
            var fecha = new DateOnly(2025, 8, 13); // miércoles
            var p = Periodo.PorSemana(fecha, DayOfWeek.Monday);

            Assert.Multiple(() =>
            {
                Assert.That(p.Inicio, Is.EqualTo(new DateOnly(2025, 8, 11)));   // lunes
                Assert.That(p.FinInclusive, Is.EqualTo(new DateOnly(2025, 8, 17))); // domingo
                Assert.That(p.Dias, Is.EqualTo(7));
                Assert.That(p.PrimerDiaSemana, Is.EqualTo(DayOfWeek.Monday));
                Assert.That(p.Granularidad, Is.SameAs(Granularidad.Semana));
            });

            Assert.DoesNotThrow(p.AsegurarAlineado);
        }

        [Test]
        public void PorSemana_DomingoComoInicio_AlineaYDuracion7Dias()
        {
            var fecha = new DateOnly(2025, 8, 13); // miércoles
            var p = Periodo.PorSemana(fecha, DayOfWeek.Sunday);

            Assert.Multiple(() =>
            {
                Assert.That(p.Inicio, Is.EqualTo(new DateOnly(2025, 8, 10)));   // domingo
                Assert.That(p.FinInclusive, Is.EqualTo(new DateOnly(2025, 8, 16))); // sábado
                Assert.That(p.Dias, Is.EqualTo(7));
                Assert.That(p.PrimerDiaSemana, Is.EqualTo(DayOfWeek.Sunday));
            });

            Assert.DoesNotThrow(p.AsegurarAlineado);
        }

        [Test]
        public void PorMes_AlineaPrimerYUltimoDia_DuracionCorrecta()
        {
            var p = Periodo.PorMes(2025, 2); // 2025 no es bisiesto

            Assert.Multiple(() =>
            {
                Assert.That(p.Inicio, Is.EqualTo(new DateOnly(2025, 2, 1)));
                Assert.That(p.FinInclusive, Is.EqualTo(new DateOnly(2025, 2, 28)));
                Assert.That(p.Dias, Is.EqualTo(28));
                Assert.That(p.Granularidad, Is.SameAs(Granularidad.Mes));
            });

            Assert.DoesNotThrow(p.AsegurarAlineado);
        }

        [Test]
        public void PorMes_FebreroBisiesto_29Dias()
        {
            var p = Periodo.PorMes(2024, 2); // 2024 bisiesto
            Assert.That(p.Dias, Is.EqualTo(29));
            Assert.DoesNotThrow(p.AsegurarAlineado);
        }

        [Test]
        public void PorAnio_AlineaInicioFin_DuracionCorrecta()
        {
            var p = Periodo.PorAnio(2025);

            Assert.Multiple(() =>
            {
                Assert.That(p.Inicio, Is.EqualTo(new DateOnly(2025, 1, 1)));
                Assert.That(p.FinInclusive, Is.EqualTo(new DateOnly(2025, 12, 31)));
                Assert.That(p.Dias, Is.EqualTo(365));
                Assert.That(p.Granularidad, Is.SameAs(Granularidad.Anio));
            });

            Assert.DoesNotThrow(p.AsegurarAlineado);
        }

        [Test]
        public void PorAnio_Bisiesto_366Dias()
        {
            var p = Periodo.PorAnio(2024);
            Assert.That(p.Dias, Is.EqualTo(366));
            Assert.DoesNotThrow(p.AsegurarAlineado);
        }

        // ---------------------- Personalizado ----------------------

        [Test]
        public void Personalizado_RangoValido_SinGranularidad()
        {
            var p = Periodo.Personalizado(new DateOnly(2025, 8, 1), new DateOnly(2025, 8, 15));

            Assert.Multiple(() =>
            {
                Assert.That(p.Granularidad, Is.Null);
                Assert.That(p.Inicio, Is.EqualTo(new DateOnly(2025, 8, 1)));
                Assert.That(p.FinInclusive, Is.EqualTo(new DateOnly(2025, 8, 15)));
                Assert.That(p.Dias, Is.EqualTo(15));
                Assert.That(p.FinExclusivo, Is.EqualTo(new DateOnly(2025, 8, 16)));
            });

            // No valida alineación para personalizados
            Assert.DoesNotThrow(p.AsegurarAlineado);
        }

        [Test]
        public void Personalizado_FinAnteriorAInicio_Lanza()
        {
            Assert.Throws<ArgumentException>(() =>
                Periodo.Personalizado(new DateOnly(2025, 8, 10), new DateOnly(2025, 8, 9)));
        }

        // ---------------------- Contiene / Interseca ----------------------

        [Test]
        public void Contiene_Fecha_SegunRangos()
        {
            var p = Periodo.PorSemana(new DateOnly(2025, 8, 13)); // lunes 11 .. domingo 17

            Assert.Multiple(() =>
            {
                Assert.That(p.Contiene(new DateOnly(2025, 8, 11)), Is.True); // inicio
                Assert.That(p.Contiene(new DateOnly(2025, 8, 14)), Is.True); // medio
                Assert.That(p.Contiene(new DateOnly(2025, 8, 17)), Is.True); // fin
                Assert.That(p.Contiene(new DateOnly(2025, 8, 10)), Is.False); // antes
                Assert.That(p.Contiene(new DateOnly(2025, 8, 18)), Is.False); // después
            });
        }

        [Test]
        public void Interseca_Y_Contiene_Periodos()
        {
            var a = Periodo.Personalizado(new DateOnly(2025, 8, 1), new DateOnly(2025, 8, 10));
            var b = Periodo.Personalizado(new DateOnly(2025, 8, 10), new DateOnly(2025, 8, 20)); // borde común
            var c = Periodo.Personalizado(new DateOnly(2025, 8, 21), new DateOnly(2025, 8, 25));
            var d = Periodo.Personalizado(new DateOnly(2025, 8, 3), new DateOnly(2025, 8, 5));   // dentro de 'a'

            Assert.Multiple(() =>
            {
                Assert.That(a.Interseca(b), Is.True);    // comparten el 10/08
                Assert.That(a.Interseca(c), Is.False);   // disjuntos
                Assert.That(a.Contiene(d), Is.True);     // 'a' contiene a 'd'
                Assert.That(d.Contiene(a), Is.False);    // 'd' no contiene a 'a'
            });
        }

        // ---------------------- Navegación Siguiente/Anterior ----------------------

        [Test]
        public void SiguienteYAnterior_PorDia()
        {
            var p = Periodo.PorDia(new DateOnly(2025, 8, 13));
            var next = p.Siguiente();
            var prev = p.Anterior();

            Assert.Multiple(() =>
            {
                Assert.That(next.Inicio, Is.EqualTo(new DateOnly(2025, 8, 14)));
                Assert.That(next.FinInclusive, Is.EqualTo(new DateOnly(2025, 8, 14)));
                Assert.That(prev.Inicio, Is.EqualTo(new DateOnly(2025, 8, 12)));
                Assert.That(prev.FinInclusive, Is.EqualTo(new DateOnly(2025, 8, 12)));
            });
        }

        [Test]
        public void SiguienteYAnterior_PorSemana_RespetaPrimerDiaSemana()
        {
            var p = Periodo.PorSemana(new DateOnly(2025, 8, 13), DayOfWeek.Monday); // 11..17
            var next = p.Siguiente();  // 18..24
            var prev = p.Anterior();   // 04..10

            Assert.Multiple(() =>
            {
                Assert.That(next.Inicio, Is.EqualTo(new DateOnly(2025, 8, 18)));
                Assert.That(next.FinInclusive, Is.EqualTo(new DateOnly(2025, 8, 24)));
                Assert.That(prev.Inicio, Is.EqualTo(new DateOnly(2025, 8, 4)));
                Assert.That(prev.FinInclusive, Is.EqualTo(new DateOnly(2025, 8, 10)));
            });
        }

        [Test]
        public void SiguienteYAnterior_PorMes()
        {
            var p = Periodo.PorMes(2025, 1); // Ene 2025
            var next = p.Siguiente();        // Feb 2025
            var prev = p.Anterior();         // Dic 2024

            Assert.Multiple(() =>
            {
                Assert.That(next.Inicio, Is.EqualTo(new DateOnly(2025, 2, 1)));
                Assert.That(next.FinInclusive, Is.EqualTo(new DateOnly(2025, 2, 28)));
                Assert.That(prev.Inicio, Is.EqualTo(new DateOnly(2024, 12, 1)));
                Assert.That(prev.FinInclusive, Is.EqualTo(new DateOnly(2024, 12, 31)));
            });
        }

        [Test]
        public void SiguienteYAnterior_PorAnio()
        {
            var p = Periodo.PorAnio(2025);
            var next = p.Siguiente(); // 2026
            var prev = p.Anterior();  // 2024

            Assert.Multiple(() =>
            {
                Assert.That(next.Inicio, Is.EqualTo(new DateOnly(2026, 1, 1)));
                Assert.That(next.FinInclusive, Is.EqualTo(new DateOnly(2026, 12, 31)));
                Assert.That(prev.Inicio, Is.EqualTo(new DateOnly(2024, 1, 1)));
                Assert.That(prev.FinInclusive, Is.EqualTo(new DateOnly(2024, 12, 31)));
            });
        }

        [Test]
        public void Siguiente_Anterior_EnPersonalizado_NoSoportado()
        {
            var p = Periodo.Personalizado(new DateOnly(2025, 8, 1), new DateOnly(2025, 8, 15));

            Assert.Throws<NotSupportedException>(() => p.Siguiente());
            Assert.Throws<NotSupportedException>(() => p.Anterior());
        }

        // ---------------------- ToString ----------------------

        [Test]
        public void ToString_FormatoEsperado()
        {
            var p1 = Periodo.PorDia(new DateOnly(2025, 8, 13));
            var p2 = Periodo.PorMes(2025, 8);
            var p3 = Periodo.Personalizado(new DateOnly(2025, 8, 1), new DateOnly(2025, 8, 15));

            Assert.Multiple(() =>
            {
                Assert.That(p1.ToString(), Is.EqualTo("DIA [2025-08-13..2025-08-13]"));
                Assert.That(p2.ToString(), Is.EqualTo("MES [2025-08-01..2025-08-31]"));
                Assert.That(p3.ToString(), Is.EqualTo("PERSONALIZADO [2025-08-01..2025-08-15]"));
            });
        }
    }
}