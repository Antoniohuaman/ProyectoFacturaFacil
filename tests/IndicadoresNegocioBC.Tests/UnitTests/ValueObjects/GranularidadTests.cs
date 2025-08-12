using System;
using System.Linq;
using IndicadoresNegocioBC.Domain.ValueObjects;
using NUnit.Framework;

namespace IndicadoresNegocioBC.Tests.UnitTests.ValueObjects
{
    public class GranularidadTests
    {
        [TestCase((byte)1, "DIA")]
        [TestCase((byte)2, "SEMANA")]
        [TestCase((byte)3, "MES")]
        [TestCase((byte)4, "ANIO")]
        public void DesdeCodigo_Valido_DevuelveSingleton(byte codigo, string esperado)
        {
            var g = Granularidad.DesdeCodigo(codigo);

            Assert.That(g.Nombre, Is.EqualTo(esperado));

            var expectedRef = codigo switch
            {
                1 => Granularidad.Dia,
                2 => Granularidad.Semana,
                3 => Granularidad.Mes,
                4 => Granularidad.Anio,
                _ => throw new ArgumentOutOfRangeException()
            };
            Assert.That(ReferenceEquals(g, expectedRef), Is.True);
            Assert.That(g.Codigo, Is.EqualTo(codigo));
        }

        [Test]
        public void DesdeCodigo_Invalido_Lanza()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Granularidad.DesdeCodigo(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => Granularidad.DesdeCodigo(5));
        }

        [TestCase("dia", "DIA")]
        [TestCase("  semanal ", "SEMANA")]
        [TestCase("Month", "MES")]
        [TestCase("AÑO", "ANIO")]
        public void DesdeTexto_Valido_NormalizaYReconoceAliased(string texto, string esperadoNombre)
        {
            var g = Granularidad.DesdeTexto(texto);
            Assert.That(g.Nombre, Is.EqualTo(esperadoNombre));
        }

        [Test]
        public void DesdeTexto_Invalido_Lanza()
        {
            Assert.Throws<ArgumentException>(() => Granularidad.DesdeTexto(null!));
            Assert.Throws<ArgumentException>(() => Granularidad.DesdeTexto("   "));
            Assert.Throws<ArgumentException>(() => Granularidad.DesdeTexto("TRIMESTRE"));
        }

        [Test]
        public void ToString_DevuelveNombre()
        {
            Assert.That(Granularidad.Dia.ToString(), Is.EqualTo("DIA"));
            Assert.That(Granularidad.Semana.ToString(), Is.EqualTo("SEMANA"));
            Assert.That(Granularidad.Mes.ToString(), Is.EqualTo("MES"));
            Assert.That(Granularidad.Anio.ToString(), Is.EqualTo("ANIO"));
        }

        [Test]
        public void Todos_TieneCuatro_YSonUnicos_YCorrespondientes()
        {
            var todos = Granularidad.Todos;

            Assert.Multiple(() =>
            {
                Assert.That(todos, Is.Not.Null);
                Assert.That(todos.Count, Is.EqualTo(4));
                Assert.That(todos, Is.Unique);
            });

            // Equivalencia (sin importar el orden)
            Assert.That(
                todos.ToArray(),
                Is.EquivalentTo(new[]
                {
                    Granularidad.Dia, Granularidad.Semana, Granularidad.Mes, Granularidad.Anio
                })
            );
        }

        // --------- ObtenerInicio (alineación) ---------

        [Test]
        public void ObtenerInicio_Dia_DevuelveMismaFecha()
        {
            var fecha = new DateOnly(2025, 8, 13); // miércoles
            var inicio = Granularidad.Dia.ObtenerInicio(fecha);
            Assert.That(inicio, Is.EqualTo(fecha));
        }

        [Test]
        public void ObtenerInicio_Semana_LunesComoInicio()
        {
            // Miércoles 13/08/2025 -> inicio lunes 11/08/2025
            var fecha = new DateOnly(2025, 8, 13);
            var inicio = Granularidad.Semana.ObtenerInicio(fecha, DayOfWeek.Monday);
            Assert.That(inicio, Is.EqualTo(new DateOnly(2025, 8, 11)));
        }

        [Test]
        public void ObtenerInicio_Semana_DomingoComoInicio()
        {
            // Miércoles 13/08/2025 -> inicio domingo 10/08/2025
            var fecha = new DateOnly(2025, 8, 13);
            var inicio = Granularidad.Semana.ObtenerInicio(fecha, DayOfWeek.Sunday);
            Assert.That(inicio, Is.EqualTo(new DateOnly(2025, 8, 10)));
        }

        [Test]
        public void ObtenerInicio_Mes_PrimerDiaDelMes()
        {
            var fecha = new DateOnly(2025, 8, 13);
            var inicio = Granularidad.Mes.ObtenerInicio(fecha);
            Assert.That(inicio, Is.EqualTo(new DateOnly(2025, 8, 1)));
        }

        [Test]
        public void ObtenerInicio_Anio_PrimerDiaDelAnio()
        {
            var fecha = new DateOnly(2025, 12, 31);
            var inicio = Granularidad.Anio.ObtenerInicio(fecha);
            Assert.That(inicio, Is.EqualTo(new DateOnly(2025, 1, 1)));
        }

        // --------- SiguienteInicio ---------

        [Test]
        public void SiguienteInicio_Dia_AvanzaUnDia()
        {
            var inicio = new DateOnly(2025, 8, 13);
            var siguiente = Granularidad.Dia.SiguienteInicio(inicio);
            Assert.That(siguiente, Is.EqualTo(new DateOnly(2025, 8, 14)));
        }

        [Test]
        public void SiguienteInicio_Semana_AvanzaSieteDias_RespetandoInicioSemana()
        {
            var inicio = new DateOnly(2025, 8, 11); // lunes
            var siguiente = Granularidad.Semana.SiguienteInicio(inicio, DayOfWeek.Monday);
            Assert.That(siguiente, Is.EqualTo(new DateOnly(2025, 8, 18)));
        }

        [Test]
        public void SiguienteInicio_Mes_AvanzaAlPrimerDiaDelMesSiguiente()
        {
            var inicio = new DateOnly(2025, 1, 1);
            var siguiente = Granularidad.Mes.SiguienteInicio(inicio);
            Assert.That(siguiente, Is.EqualTo(new DateOnly(2025, 2, 1)));
        }

        [Test]
        public void SiguienteInicio_Mes_RespetaCambioDeAnio()
        {
            var inicio = new DateOnly(2025, 12, 1);
            var siguiente = Granularidad.Mes.SiguienteInicio(inicio);
            Assert.That(siguiente, Is.EqualTo(new DateOnly(2026, 1, 1)));
        }

        [Test]
        public void SiguienteInicio_Anio_AvanzaUnAnio()
        {
            var inicio = new DateOnly(2024, 1, 1);
            var siguiente = Granularidad.Anio.SiguienteInicio(inicio);
            Assert.That(siguiente, Is.EqualTo(new DateOnly(2025, 1, 1)));
        }

        // --------- AsegurarAlineado ---------

        [Test]
        public void AsegurarAlineado_Semana_LanzaSiNoEsInicioDeSemana()
        {
            var noInicio = new DateOnly(2025, 8, 13); // miércoles
            Assert.Throws<InvalidOperationException>(() =>
                Granularidad.Semana.AsegurarAlineado(noInicio, DayOfWeek.Monday));
        }

        [Test]
        public void AsegurarAlineado_Semana_NoLanzaSiEsInicioDeSemana()
        {
            var inicio = new DateOnly(2025, 8, 11); // lunes
            Assert.DoesNotThrow(() =>
                Granularidad.Semana.AsegurarAlineado(inicio, DayOfWeek.Monday));
        }

        [Test]
        public void AsegurarAlineado_Mes_LanzaSiNoEsPrimeroDelMes()
        {
            var noInicio = new DateOnly(2025, 8, 2);
            Assert.Throws<InvalidOperationException>(() =>
                Granularidad.Mes.AsegurarAlineado(noInicio));
        }

        [Test]
        public void AsegurarAlineado_Mes_NoLanzaEnPrimeroDelMes()
        {
            var inicio = new DateOnly(2025, 8, 1);
            Assert.DoesNotThrow(() => Granularidad.Mes.AsegurarAlineado(inicio));
        }

        [Test]
        public void AsegurarAlineado_Anio_NoLanzaEnPrimeroDeEnero()
        {
            var inicio = new DateOnly(2025, 1, 1);
            Assert.DoesNotThrow(() => Granularidad.Anio.AsegurarAlineado(inicio));
        }
    }
}