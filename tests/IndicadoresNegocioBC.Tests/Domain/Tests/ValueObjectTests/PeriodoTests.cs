using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using IndicadoresNegocioBC.Domain.ValueObjects;

namespace IndicadoresNegocioBC.Tests.Domain.ValueObjects
{
    [TestFixture]
    public class PeriodoTests
    {
        // ================== Helper para crear Periodos misalineados (reflexión) ==================
        private static Periodo CrearPeriodoUnsafe(DateOnly inicio, DateOnly finInclusive, Granularidad? gran, DayOfWeek primerDiaSemana = DayOfWeek.Monday)
        {
            // Constructor privado: (DateOnly inicio, DateOnly finInclusive, Granularidad? granularidad, DayOfWeek primerDiaSemana)
            var ctor = typeof(Periodo).GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null,
                types: new[] { typeof(DateOnly), typeof(DateOnly), typeof(Granularidad), typeof(DayOfWeek) },
                modifiers: null);

            // Cuando gran == null (personalizado) el parámetro del ctor no acepta null (tipo no-nullable). Evitar este helper para personalizados.
            if (gran is null)
                Assert.Inconclusive("Para personalizados usa Periodo.Personalizado; este helper es solo para granularidades.");

            return (Periodo)ctor!.Invoke(new object[] { inicio, finInclusive, gran!, primerDiaSemana });
        }

        // ================== Fábricas alineadas ==================

        [Test]
        public void PorDia_Deberia_Crear_Un_Dia_Con_Propiedades_Correctas()
        {
            var d = new DateOnly(2025, 03, 12);

            var p = Periodo.PorDia(d);

            Assert.That(p.Inicio, Is.EqualTo(d));
            Assert.That(p.FinInclusive, Is.EqualTo(d));
            Assert.That(p.FinExclusivo, Is.EqualTo(d.AddDays(1)));
            Assert.That(p.Dias, Is.EqualTo(1));
            Assert.That(p.Granularidad, Is.SameAs(Granularidad.Dia));
            Assert.DoesNotThrow(p.AsegurarAlineado);
            Assert.That(p.ToString(), Does.Contain("DIA"));
            Assert.That(p.ToString(), Does.Contain("[2025-03-12..2025-03-12]"));
        }

        [Test]
        public void PorSemana_DefaultMonday_Deberia_Alinear_A_Lunes_Y_Durar_7_Dias()
        {
            var fecha = new DateOnly(2025, 03, 12); // miércoles
            var p = Periodo.PorSemana(fecha);       // lunes como default

            Assert.That(p.Inicio, Is.EqualTo(new DateOnly(2025, 03, 10)));    // Lunes
            Assert.That(p.FinInclusive, Is.EqualTo(new DateOnly(2025, 03, 16)));
            Assert.That(p.Dias, Is.EqualTo(7));
            Assert.That(p.Granularidad, Is.SameAs(Granularidad.Semana));
            Assert.That(p.PrimerDiaSemana, Is.EqualTo(DayOfWeek.Monday));
            Assert.DoesNotThrow(p.AsegurarAlineado);

            // Navegación
            var siguiente = p.Siguiente();
            Assert.That(siguiente.Inicio, Is.EqualTo(new DateOnly(2025, 03, 17)));
            Assert.That(siguiente.PrimerDiaSemana, Is.EqualTo(DayOfWeek.Monday));

            var anterior = p.Anterior();
            Assert.That(anterior.Inicio, Is.EqualTo(new DateOnly(2025, 03, 03)));
            Assert.That(anterior.PrimerDiaSemana, Is.EqualTo(DayOfWeek.Monday));
        }

        [Test]
        public void PorSemana_SundayFirst_Deberia_Respetar_PrimerDiaSemana()
        {
            var fecha = new DateOnly(2025, 03, 12); // miércoles
            var p = Periodo.PorSemana(fecha, DayOfWeek.Sunday);

            Assert.That(p.Inicio, Is.EqualTo(new DateOnly(2025, 03, 09))); // Domingo
            Assert.That(p.FinInclusive, Is.EqualTo(new DateOnly(2025, 03, 15)));
            Assert.That(p.Dias, Is.EqualTo(7));
            Assert.That(p.PrimerDiaSemana, Is.EqualTo(DayOfWeek.Sunday));
            Assert.DoesNotThrow(p.AsegurarAlineado);

            // Navegación mantiene Sunday
            var siguiente = p.Siguiente();
            Assert.That(siguiente.Inicio, Is.EqualTo(new DateOnly(2025, 03, 16)));
            Assert.That(siguiente.PrimerDiaSemana, Is.EqualTo(DayOfWeek.Sunday));

            var anterior = p.Anterior();
            Assert.That(anterior.Inicio, Is.EqualTo(new DateOnly(2025, 03, 02)));
            Assert.That(anterior.PrimerDiaSemana, Is.EqualTo(DayOfWeek.Sunday));
        }

        [Test]
        public void PorSemana_Cruce_De_Anio_Deberia_Calcular_Bien()
        {
            var fecha = new DateOnly(2024, 01, 01); // Lunes
            var pMon = Periodo.PorSemana(fecha, DayOfWeek.Monday);
            Assert.That(pMon.Inicio, Is.EqualTo(new DateOnly(2024, 01, 01)));
            Assert.That(pMon.FinInclusive, Is.EqualTo(new DateOnly(2024, 01, 07)));

            var pSun = Periodo.PorSemana(fecha, DayOfWeek.Sunday);
            Assert.That(pSun.Inicio, Is.EqualTo(new DateOnly(2023, 12, 31)));
            Assert.That(pSun.FinInclusive, Is.EqualTo(new DateOnly(2024, 01, 06)));
        }

        [Test]
        public void PorMes_Deberia_Ir_De_Primero_A_Ultimo_Dia()
        {
            var p = Periodo.PorMes(2025, 07);
            Assert.That(p.Inicio, Is.EqualTo(new DateOnly(2025, 07, 01)));
            Assert.That(p.FinInclusive, Is.EqualTo(new DateOnly(2025, 07, 31)));
            Assert.That(p.Dias, Is.EqualTo(31));
            Assert.That(p.Granularidad, Is.SameAs(Granularidad.Mes));
            Assert.DoesNotThrow(p.AsegurarAlineado);

            var siguiente = p.Siguiente();
            Assert.That(siguiente.Inicio, Is.EqualTo(new DateOnly(2025, 08, 01)));
            Assert.That(siguiente.FinInclusive, Is.EqualTo(new DateOnly(2025, 08, 31)));

            var anterior = p.Anterior();
            Assert.That(anterior.Inicio, Is.EqualTo(new DateOnly(2025, 06, 01)));
            Assert.That(anterior.FinInclusive, Is.EqualTo(new DateOnly(2025, 06, 30)));
        }

        [Test]
        public void PorMes_Febrero_AnioBisiesto_Deberia_Tener_29_Dias()
        {
            var p = Periodo.PorMes(2024, 2); // 2024 es bisiesto
            Assert.That(p.Inicio, Is.EqualTo(new DateOnly(2024, 02, 01)));
            Assert.That(p.FinInclusive, Is.EqualTo(new DateOnly(2024, 02, 29)));
            Assert.That(p.Dias, Is.EqualTo(29));
            Assert.DoesNotThrow(p.AsegurarAlineado);
        }

        [Test]
        public void PorAnio_Deberia_Ir_De_0101_A_1231()
        {
            var p = Periodo.PorAnio(2025);
            Assert.That(p.Inicio, Is.EqualTo(new DateOnly(2025, 01, 01)));
            Assert.That(p.FinInclusive, Is.EqualTo(new DateOnly(2025, 12, 31)));
            Assert.That(p.Dias, Is.EqualTo(365));
            Assert.That(p.Granularidad, Is.SameAs(Granularidad.Anio));
            Assert.DoesNotThrow(p.AsegurarAlineado);

            var siguiente = p.Siguiente();
            Assert.That(siguiente.Inicio, Is.EqualTo(new DateOnly(2026, 01, 01)));
            var anterior = p.Anterior();
            Assert.That(anterior.Inicio, Is.EqualTo(new DateOnly(2024, 01, 01)));
        }

        // ================== Personalizado ==================

        [Test]
        public void Personalizado_Deberia_Tener_Granularidad_Null_Y_Respetar_Rango()
        {
            var ini = new DateOnly(2025, 03, 10);
            var fin = new DateOnly(2025, 03, 20);

            var p = Periodo.Personalizado(ini, fin);

            Assert.That(p.Granularidad, Is.Null);
            Assert.That(p.Inicio, Is.EqualTo(ini));
            Assert.That(p.FinInclusive, Is.EqualTo(fin));
            Assert.That(p.FinExclusivo, Is.EqualTo(fin.AddDays(1)));
            Assert.That(p.Dias, Is.EqualTo(11));
            Assert.DoesNotThrow(p.AsegurarAlineado); // no-op para personalizado

            // Navegación no soportada
            Assert.That(() => p.Siguiente(), Throws.Exception.TypeOf<NotSupportedException>());
            Assert.That(() => p.Anterior(), Throws.Exception.TypeOf<NotSupportedException>());

            Assert.That(p.ToString(), Does.Contain("PERSONALIZADO"));
        }

        [Test]
        public void Personalizado_FinMenorQueInicio_Deberia_Fallar()
        {
            var ini = new DateOnly(2025, 03, 10);
            var fin = new DateOnly(2025, 03, 09);

            Assert.That(() => Periodo.Personalizado(ini, fin), Throws.ArgumentException);
        }

        // ================== Contiene / Interseca ==================

        [Test]
        public void Contiene_Fecha_Y_Periodo_Deberia_Funcionar_Inclusive()
        {
            var p = Periodo.PorSemana(new DateOnly(2025, 03, 12)); // 10..16
            Assert.That(p.Contiene(new DateOnly(2025, 03, 10)), Is.True); // borde
            Assert.That(p.Contiene(new DateOnly(2025, 03, 13)), Is.True);
            Assert.That(p.Contiene(new DateOnly(2025, 03, 16)), Is.True); // borde
            Assert.That(p.Contiene(new DateOnly(2025, 03, 17)), Is.False);

            var q = Periodo.PorDia(new DateOnly(2025, 03, 12));
            Assert.That(p.Contiene(q), Is.True); // contiene completamente

            var r = Periodo.PorDia(new DateOnly(2025, 03, 17));
            Assert.That(p.Interseca(r), Is.False);

            var s = Periodo.PorDia(new DateOnly(2025, 03, 16));
            Assert.That(p.Interseca(s), Is.True); // borde
        }

        // ================== AsegurarAlineado (casos que deben lanzar) ==================

        [Test]
        public void AsegurarAlineado_Dia_FinDistinto_Deberia_Lanzar()
        {
            var ini = new DateOnly(2025, 03, 12);
            var fin = ini.AddDays(1); // mal para DIA
            var p = CrearPeriodoUnsafe(ini, fin, Granularidad.Dia);

            Assert.That(() => p.AsegurarAlineado(),
                Throws.Exception.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void AsegurarAlineado_Semana_Inicio_NoAlineado_O_Fin_Incorrecto_Deberia_Lanzar()
        {
            // Inicio no alineado a Monday
            var p1 = CrearPeriodoUnsafe(new DateOnly(2025, 03, 11), new DateOnly(2025, 03, 17), Granularidad.Semana, DayOfWeek.Monday);
            Assert.That(() => p1.AsegurarAlineado(),
                Throws.Exception.TypeOf<InvalidOperationException>());

            // Inicio alineado pero fin incorrecto (no 6 días después)
            var p2 = CrearPeriodoUnsafe(new DateOnly(2025, 03, 10), new DateOnly(2025, 03, 18), Granularidad.Semana, DayOfWeek.Monday);
            Assert.That(() => p2.AsegurarAlineado(),
                Throws.Exception.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void AsegurarAlineado_Mes_PrimeroOUltimoIncorrectos_Deberia_Lanzar()
        {
            // Inicio no es primero de mes
            var p1 = CrearPeriodoUnsafe(new DateOnly(2025, 07, 02), new DateOnly(2025, 07, 31), Granularidad.Mes);
            Assert.That(() => p1.AsegurarAlineado(),
                Throws.Exception.TypeOf<InvalidOperationException>());

            // Fin no es último del mes
            var p2 = CrearPeriodoUnsafe(new DateOnly(2025, 07, 01), new DateOnly(2025, 07, 30), Granularidad.Mes);
            Assert.That(() => p2.AsegurarAlineado(),
                Throws.Exception.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void AsegurarAlineado_Anio_InicioOFinIncorrectos_Deberia_Lanzar()
        {
            var p1 = CrearPeriodoUnsafe(new DateOnly(2025, 01, 02), new DateOnly(2025, 12, 31), Granularidad.Anio);
            Assert.That(() => p1.AsegurarAlineado(),
                Throws.Exception.TypeOf<InvalidOperationException>());

            var p2 = CrearPeriodoUnsafe(new DateOnly(2025, 01, 01), new DateOnly(2025, 12, 30), Granularidad.Anio);
            Assert.That(() => p2.AsegurarAlineado(),
                Throws.Exception.TypeOf<InvalidOperationException>());
        }
    }
}
