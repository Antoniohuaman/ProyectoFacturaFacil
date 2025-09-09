using System;
using System.Linq;
using NUnit.Framework;
using IndicadoresNegocioBC.Domain.ValueObjects;

namespace IndicadoresNegocioBC.Tests.Domain.ValueObjects
{
    [TestFixture]
    public class GranularidadTests
    {
        [Test]
        public void DesdeCodigo_Deberia_Retornar_Instancias_Conocidas()
        {
            Assert.That(Granularidad.DesdeCodigo(1), Is.SameAs(Granularidad.Dia));
            Assert.That(Granularidad.DesdeCodigo(2), Is.SameAs(Granularidad.Semana));
            Assert.That(Granularidad.DesdeCodigo(3), Is.SameAs(Granularidad.Mes));
            Assert.That(Granularidad.DesdeCodigo(4), Is.SameAs(Granularidad.Anio));
        }

        [Test]
        public void DesdeCodigo_Invalido_Deberia_Fallar()
        {
            Assert.That(() => Granularidad.DesdeCodigo(0), Throws.Exception.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => Granularidad.DesdeCodigo(5), Throws.Exception.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void DesdeTexto_Deberia_Soportar_Alias_Trim_Y_CaseInsensitive()
        {
            // Día
            Assert.That(Granularidad.DesdeTexto("DIA"), Is.SameAs(Granularidad.Dia));
            Assert.That(Granularidad.DesdeTexto("diario"), Is.SameAs(Granularidad.Dia));
            Assert.That(Granularidad.DesdeTexto(" day "), Is.SameAs(Granularidad.Dia));

            // Semana
            Assert.That(Granularidad.DesdeTexto("SEMANA"), Is.SameAs(Granularidad.Semana));
            Assert.That(Granularidad.DesdeTexto("semanal"), Is.SameAs(Granularidad.Semana));
            Assert.That(Granularidad.DesdeTexto("WEEK"), Is.SameAs(Granularidad.Semana));

            // Mes
            Assert.That(Granularidad.DesdeTexto("MES"), Is.SameAs(Granularidad.Mes));
            Assert.That(Granularidad.DesdeTexto("mensual"), Is.SameAs(Granularidad.Mes));
            Assert.That(Granularidad.DesdeTexto("month"), Is.SameAs(Granularidad.Mes));

            // Año
            Assert.That(Granularidad.DesdeTexto("ANIO"), Is.SameAs(Granularidad.Anio));
            Assert.That(Granularidad.DesdeTexto("AÑO"), Is.SameAs(Granularidad.Anio));
            Assert.That(Granularidad.DesdeTexto("anual"), Is.SameAs(Granularidad.Anio));
            Assert.That(Granularidad.DesdeTexto("YEAR"), Is.SameAs(Granularidad.Anio));
        }

        [Test]
        public void DesdeTexto_VacioODesconocido_Deberia_Fallar()
        {
            Assert.That(() => Granularidad.DesdeTexto(""), Throws.ArgumentException);
            Assert.That(() => Granularidad.DesdeTexto("  "), Throws.ArgumentException);
            Assert.That(() => Granularidad.DesdeTexto("quincena"), Throws.ArgumentException);
        }

        [Test]
        public void Todos_Deberia_Incluir_Las_Cuatro_Instancias_Singleton()
        {
            var todos = Granularidad.Todos;

            Assert.That(todos, Is.Not.Null);
            Assert.That(todos.Count, Is.EqualTo(4));
            Assert.That(todos.Contains(Granularidad.Dia), Is.True);
            Assert.That(todos.Contains(Granularidad.Semana), Is.True);
            Assert.That(todos.Contains(Granularidad.Mes), Is.True);
            Assert.That(todos.Contains(Granularidad.Anio), Is.True);

            // Unicidad por referencia (singletons)
            Assert.That(todos.Distinct().Count(), Is.EqualTo(4));
            Assert.That(todos[0], Is.SameAs(Granularidad.Dia).Or.SameAs(Granularidad.Semana).Or.SameAs(Granularidad.Mes).Or.SameAs(Granularidad.Anio));
        }

        [Test]
        public void ToString_Deberia_Retornar_Nombre_Normalizado()
        {
            Assert.That(Granularidad.Dia.ToString(), Is.EqualTo("DIA"));
            Assert.That(Granularidad.Semana.ToString(), Is.EqualTo("SEMANA"));
            Assert.That(Granularidad.Mes.ToString(), Is.EqualTo("MES"));
            Assert.That(Granularidad.Anio.ToString(), Is.EqualTo("ANIO"));
        }

        // ---------------- ObtenerInicio ----------------

        [Test]
        public void ObtenerInicio_Dia_Deberia_Retornar_MismaFecha()
        {
            var f = new DateOnly(2025, 03, 12); // Miércoles
            Assert.That(Granularidad.Dia.ObtenerInicio(f), Is.EqualTo(f));
        }

        [Test]
        public void ObtenerInicio_Semana_DefaultMonday_Deberia_Retornar_Lunes_Correcto()
        {
            // Semana con inicio Lunes (default)
            // Miercoles 12/03/2025 -> Lunes 10/03/2025
            var f1 = new DateOnly(2025, 03, 12); // Wed
            var esperado = new DateOnly(2025, 03, 10); // Mon
            Assert.That(Granularidad.Semana.ObtenerInicio(f1), Is.EqualTo(esperado));

            // Domingo 16/03/2025 -> Lunes 10/03/2025 (domingo pertenece a semana que comenzó el lunes anterior)
            var f2 = new DateOnly(2025, 03, 16); // Sun
            Assert.That(Granularidad.Semana.ObtenerInicio(f2), Is.EqualTo(esperado));
        }

        [Test]
        public void ObtenerInicio_Semana_SundayFirst_Deberia_Retornar_Domingo_Correcto()
        {
            // Si la semana empieza en domingo:
            // Miércoles 12/03/2025 -> Domingo 09/03/2025
            var f = new DateOnly(2025, 03, 12);
            var esperado = new DateOnly(2025, 03, 09); // Sun
            Assert.That(Granularidad.Semana.ObtenerInicio(f, DayOfWeek.Sunday), Is.EqualTo(esperado));
        }

        [Test]
        public void ObtenerInicio_Mes_Deberia_Retornar_PrimeroDeMes()
        {
            var f = new DateOnly(2025, 07, 19);
            Assert.That(Granularidad.Mes.ObtenerInicio(f), Is.EqualTo(new DateOnly(2025, 07, 01)));
        }

        [Test]
        public void ObtenerInicio_Anio_Deberia_Retornar_PrimeroDeEnero()
        {
            var f = new DateOnly(2025, 11, 30);
            Assert.That(Granularidad.Anio.ObtenerInicio(f), Is.EqualTo(new DateOnly(2025, 01, 01)));
        }

        [Test]
        public void ObtenerInicio_Semana_CruceDeAnio_Deberia_Calcular_Bien()
        {
            // 01/01/2024 es Lunes
            // - Con inicio Lunes: inicio de semana = 01/01/2024
            // - Con inicio Domingo: inicio de semana = 31/12/2023
            var f = new DateOnly(2024, 01, 01); // Mon

            Assert.That(Granularidad.Semana.ObtenerInicio(f, DayOfWeek.Monday), Is.EqualTo(new DateOnly(2024, 01, 01)));
            Assert.That(Granularidad.Semana.ObtenerInicio(f, DayOfWeek.Sunday), Is.EqualTo(new DateOnly(2023, 12, 31)));
        }

        // ---------------- SiguienteInicio ----------------

        [Test]
        public void SiguienteInicio_Dia_Deberia_Sumar_1_Dia()
        {
            var inicio = new DateOnly(2025, 03, 10);
            Assert.That(Granularidad.Dia.SiguienteInicio(inicio), Is.EqualTo(new DateOnly(2025, 03, 11)));
        }

        [Test]
        public void SiguienteInicio_Semana_DefaultMonday_Deberia_Sumar_7_Dias_Desde_Alineado()
        {
            var lunes = new DateOnly(2025, 03, 10);
            Assert.That(Granularidad.Semana.SiguienteInicio(lunes), Is.EqualTo(new DateOnly(2025, 03, 17)));
        }

        [Test]
        public void SiguienteInicio_Semana_Desde_NoAlineado_Deberia_AlinearYLuego_Avanzar()
        {
            // Miércoles 12/03/2025 (no alineado con Monday) -> se alinea a 10/03/2025 y suma 7 -> 17/03/2025
            var miercoles = new DateOnly(2025, 03, 12);
            Assert.That(Granularidad.Semana.SiguienteInicio(miercoles), Is.EqualTo(new DateOnly(2025, 03, 17)));
        }

        [Test]
        public void SiguienteInicio_Semana_SundayFirst_Desde_NoAlineado_Deberia_AlinearYLuego_Avanzar()
        {
            // Con semana comenzando domingo:
            // Miércoles 12/03/2025 -> inicio 09/03/2025 + 7 = 16/03/2025
            var miercoles = new DateOnly(2025, 03, 12);
            Assert.That(Granularidad.Semana.SiguienteInicio(miercoles, DayOfWeek.Sunday), Is.EqualTo(new DateOnly(2025, 03, 16)));
        }

        [Test]
        public void SiguienteInicio_Mes_Deberia_Ir_Al_PrimerDia_Del_SiguienteMes()
        {
            var f = new DateOnly(2025, 01, 15); // no alineado
            // Se alinea a 2025-01-01 y luego +1 mes => 2025-02-01
            Assert.That(Granularidad.Mes.SiguienteInicio(f), Is.EqualTo(new DateOnly(2025, 02, 01)));
        }

        [Test]
        public void SiguienteInicio_Anio_Deberia_Ir_Al_PrimeroDeEnero_Siguiente()
        {
            var f = new DateOnly(2024, 06, 10); // no alineado
            Assert.That(Granularidad.Anio.SiguienteInicio(f), Is.EqualTo(new DateOnly(2025, 01, 01)));
        }

        // ---------------- AsegurarAlineado ----------------

        [Test]
        public void AsegurarAlineado_Dia_Siempre_Pasa()
        {
            // Para día, cualquier fecha está alineada por definición.
            Assert.DoesNotThrow(() => Granularidad.Dia.AsegurarAlineado(new DateOnly(2025, 03, 12)));
        }

        [Test]
        public void AsegurarAlineado_Semana_DefaultMonday()
        {
            var lunes = new DateOnly(2025, 03, 10);
            var miercoles = new DateOnly(2025, 03, 12);

            Assert.DoesNotThrow(() => Granularidad.Semana.AsegurarAlineado(lunes));
            Assert.That(() => Granularidad.Semana.AsegurarAlineado(miercoles),
                Throws.Exception.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void AsegurarAlineado_Semana_SundayFirst()
        {
            var domingo = new DateOnly(2025, 03, 09);
            var lunes = new DateOnly(2025, 03, 10);

            Assert.DoesNotThrow(() => Granularidad.Semana.AsegurarAlineado(domingo, DayOfWeek.Sunday));
            Assert.That(() => Granularidad.Semana.AsegurarAlineado(lunes, DayOfWeek.Sunday),
                Throws.Exception.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void AsegurarAlineado_Mes()
        {
            var primero = new DateOnly(2025, 07, 01);
            var otro = new DateOnly(2025, 07, 02);

            Assert.DoesNotThrow(() => Granularidad.Mes.AsegurarAlineado(primero));
            Assert.That(() => Granularidad.Mes.AsegurarAlineado(otro),
                Throws.Exception.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void AsegurarAlineado_Anio()
        {
            var primero = new DateOnly(2025, 01, 01);
            var otro = new DateOnly(2025, 01, 02);

            Assert.DoesNotThrow(() => Granularidad.Anio.AsegurarAlineado(primero));
            Assert.That(() => Granularidad.Anio.AsegurarAlineado(otro),
                Throws.Exception.TypeOf<InvalidOperationException>());
        }
    }
}
