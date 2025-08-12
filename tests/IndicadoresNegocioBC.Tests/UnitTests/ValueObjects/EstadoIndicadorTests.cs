using System;
using System.Linq;
using IndicadoresNegocioBC.Domain.ValueObjects;
using NUnit.Framework;

namespace IndicadoresNegocioBC.Tests.UnitTests.ValueObjects
{
    public class EstadoIndicadorTests
    {
        [TestCase((byte)1, "CREADO")]
        [TestCase((byte)2, "ACTUALIZADO")]
        [TestCase((byte)3, "CONSOLIDADO")]
        public void DesdeCodigo_Valido_DevuelveInstanciaSingleton(byte codigo, string esperadoNombre)
        {
            var e = EstadoIndicador.DesdeCodigo(codigo);

            Assert.That(e.Nombre, Is.EqualTo(esperadoNombre));

            var expectedRef = codigo switch
            {
                1 => EstadoIndicador.Creado,
                2 => EstadoIndicador.Actualizado,
                3 => EstadoIndicador.Consolidado,
                _ => throw new ArgumentOutOfRangeException()
            };
            Assert.That(ReferenceEquals(e, expectedRef), Is.True);
            Assert.That(e.Codigo, Is.EqualTo(codigo));
        }

        [Test]
        public void DesdeCodigo_Invalido_LanzaArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => EstadoIndicador.DesdeCodigo(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => EstadoIndicador.DesdeCodigo(4));
        }

        [TestCase("creado", "CREADO")]
        [TestCase("  ACTUALIZADO  ", "ACTUALIZADO")]
        [TestCase("Consolidado", "CONSOLIDADO")]
        public void DesdeTexto_Valido_CaseInsensitiveYTrim(string texto, string esperadoNombre)
        {
            var e = EstadoIndicador.DesdeTexto(texto);

            Assert.That(e.Nombre, Is.EqualTo(esperadoNombre));

            var expectedRef = esperadoNombre switch
            {
                "CREADO" => EstadoIndicador.Creado,
                "ACTUALIZADO" => EstadoIndicador.Actualizado,
                "CONSOLIDADO" => EstadoIndicador.Consolidado,
                _ => throw new ArgumentOutOfRangeException()
            };
            Assert.That(ReferenceEquals(e, expectedRef), Is.True);
        }

        [Test]
        public void DesdeTexto_Invalido_LanzaArgumentException()
        {
            Assert.Throws<ArgumentException>(() => EstadoIndicador.DesdeTexto(null!));
            Assert.Throws<ArgumentException>(() => EstadoIndicador.DesdeTexto("   "));
            Assert.Throws<ArgumentException>(() => EstadoIndicador.DesdeTexto("EN_PROCESO"));
        }

        [Test]
        public void EsFinal_PermiteMutaciones_SegunEstado()
        {
            Assert.Multiple(() =>
            {
                Assert.That(EstadoIndicador.Creado.EsFinal, Is.False);
                Assert.That(EstadoIndicador.Creado.PermiteMutaciones, Is.True);

                Assert.That(EstadoIndicador.Actualizado.EsFinal, Is.False);
                Assert.That(EstadoIndicador.Actualizado.PermiteMutaciones, Is.True);

                Assert.That(EstadoIndicador.Consolidado.EsFinal, Is.True);
                Assert.That(EstadoIndicador.Consolidado.PermiteMutaciones, Is.False);
            });
        }

        [Test]
        public void EsTransicionValida_Idempotencia_SiempreVerdadera()
        {
            Assert.Multiple(() =>
            {
                Assert.That(EstadoIndicador.EsTransicionValida(EstadoIndicador.Creado, EstadoIndicador.Creado), Is.True);
                Assert.That(EstadoIndicador.EsTransicionValida(EstadoIndicador.Actualizado, EstadoIndicador.Actualizado), Is.True);
                Assert.That(EstadoIndicador.EsTransicionValida(EstadoIndicador.Consolidado, EstadoIndicador.Consolidado), Is.True);
            });
        }

        [Test]
        public void EsTransicionValida_ReglasPermitidas_Y_Prohibidas()
        {
            Assert.Multiple(() =>
            {
                // Permitidas
                Assert.That(EstadoIndicador.EsTransicionValida(EstadoIndicador.Creado, EstadoIndicador.Actualizado), Is.True);
                Assert.That(EstadoIndicador.EsTransicionValida(EstadoIndicador.Creado, EstadoIndicador.Consolidado), Is.True);
                Assert.That(EstadoIndicador.EsTransicionValida(EstadoIndicador.Actualizado, EstadoIndicador.Consolidado), Is.True);

                // Prohibidas
                Assert.That(EstadoIndicador.EsTransicionValida(EstadoIndicador.Actualizado, EstadoIndicador.Creado), Is.False);
                Assert.That(EstadoIndicador.EsTransicionValida(EstadoIndicador.Consolidado, EstadoIndicador.Creado), Is.False);
                Assert.That(EstadoIndicador.EsTransicionValida(EstadoIndicador.Consolidado, EstadoIndicador.Actualizado), Is.False);
            });
        }

        [Test]
        public void AsegurarTransicionValida_NoLanza_ParaPermitidas()
        {
            Assert.DoesNotThrow(() => EstadoIndicador.AsegurarTransicionValida(EstadoIndicador.Creado, EstadoIndicador.Actualizado));
            Assert.DoesNotThrow(() => EstadoIndicador.AsegurarTransicionValida(EstadoIndicador.Creado, EstadoIndicador.Consolidado));
            Assert.DoesNotThrow(() => EstadoIndicador.AsegurarTransicionValida(EstadoIndicador.Actualizado, EstadoIndicador.Consolidado));
        }

        [Test]
        public void AsegurarTransicionValida_LanzaParaInvalidas()
        {
            Assert.Throws<InvalidOperationException>(() =>
                EstadoIndicador.AsegurarTransicionValida(EstadoIndicador.Actualizado, EstadoIndicador.Creado));

            Assert.Throws<InvalidOperationException>(() =>
                EstadoIndicador.AsegurarTransicionValida(EstadoIndicador.Consolidado, EstadoIndicador.Creado));

            Assert.Throws<InvalidOperationException>(() =>
                EstadoIndicador.AsegurarTransicionValida(EstadoIndicador.Consolidado, EstadoIndicador.Actualizado));
        }

        [Test]
        public void ToString_DevuelveNombre()
        {
            Assert.That(EstadoIndicador.Creado.ToString(), Is.EqualTo("CREADO"));
            Assert.That(EstadoIndicador.Actualizado.ToString(), Is.EqualTo("ACTUALIZADO"));
            Assert.That(EstadoIndicador.Consolidado.ToString(), Is.EqualTo("CONSOLIDADO"));
        }

        [Test]
        public void Todos_ContieneExactamenteTres_EInstanciasDefinidas_YUnicas_EnOrden()
        {
            var todos = EstadoIndicador.Todos;

            Assert.Multiple(() =>
            {
                Assert.That(todos, Is.Not.Null);
                Assert.That(todos.Count, Is.EqualTo(3));
                Assert.That(todos, Is.Unique);
                Assert.That(todos[0], Is.SameAs(EstadoIndicador.Creado));
                Assert.That(todos[1], Is.SameAs(EstadoIndicador.Actualizado));
                Assert.That(todos[2], Is.SameAs(EstadoIndicador.Consolidado));
            });

            // Equivalencia (sin importar el orden)
            Assert.That(
                todos.ToArray(),
                Is.EquivalentTo(new[]
                {
                    EstadoIndicador.Creado,
                    EstadoIndicador.Actualizado,
                    EstadoIndicador.Consolidado
                })
            );
        }

        [Test]
        public void Igualdad_PorValor_DesdeCodigoYTexto_Coinciden()
        {
            var a = EstadoIndicador.DesdeCodigo(1);       // CREADO
            var b = EstadoIndicador.DesdeTexto("creado"); // CREADO

            Assert.That(a, Is.EqualTo(b));
            Assert.That(ReferenceEquals(a, b), Is.True); // misma instancia
        }
    }
}