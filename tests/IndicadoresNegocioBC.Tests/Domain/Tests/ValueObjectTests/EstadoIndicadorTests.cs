using System;
using System.Linq;
using NUnit.Framework;
using IndicadoresNegocioBC.Domain.ValueObjects;

namespace IndicadoresNegocioBC.Tests.Domain.ValueObjects
{
    [TestFixture]
    public class EstadoIndicadorTests
    {
        [Test]
        public void DesdeCodigo_Deberia_Retornar_Instancias_Conocidas()
        {
            var a = EstadoIndicador.DesdeCodigo(1);
            var b = EstadoIndicador.DesdeCodigo(2);
            var c = EstadoIndicador.DesdeCodigo(3);

            Assert.That(a, Is.SameAs(EstadoIndicador.Creado));
            Assert.That(b, Is.SameAs(EstadoIndicador.Actualizado));
            Assert.That(c, Is.SameAs(EstadoIndicador.Consolidado));
        }

        [Test]
        public void DesdeCodigo_CodigoInvalido_Deberia_Fallar()
        {
            Assert.That(() => EstadoIndicador.DesdeCodigo(0), Throws.Exception.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => EstadoIndicador.DesdeCodigo(4), Throws.Exception.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => EstadoIndicador.DesdeCodigo(byte.MaxValue), Throws.Exception.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void DesdeTexto_Deberia_Ser_CaseInsensitive_Y_Trim()
        {
            Assert.That(EstadoIndicador.DesdeTexto("creado"), Is.SameAs(EstadoIndicador.Creado));
            Assert.That(EstadoIndicador.DesdeTexto("  ACTUALIZADO "), Is.SameAs(EstadoIndicador.Actualizado));
            Assert.That(EstadoIndicador.DesdeTexto("Consolidado"), Is.SameAs(EstadoIndicador.Consolidado));
        }

        [Test]
        public void DesdeTexto_TextoInvalidoOVacio_Deberia_Fallar()
        {
            Assert.That(() => EstadoIndicador.DesdeTexto(""), Throws.ArgumentException);
            Assert.That(() => EstadoIndicador.DesdeTexto("   "), Throws.ArgumentException);
            Assert.That(() => EstadoIndicador.DesdeTexto("INVALIDO"), Throws.ArgumentException);
        }

        [Test]
        public void EsFinal_Y_PermiteMutaciones_Deberian_Coherir()
        {
            Assert.That(EstadoIndicador.Creado.EsFinal, Is.False);
            Assert.That(EstadoIndicador.Actualizado.EsFinal, Is.False);
            Assert.That(EstadoIndicador.Consolidado.EsFinal, Is.True);

            Assert.That(EstadoIndicador.Creado.PermiteMutaciones, Is.True);
            Assert.That(EstadoIndicador.Actualizado.PermiteMutaciones, Is.True);
            Assert.That(EstadoIndicador.Consolidado.PermiteMutaciones, Is.False);
        }

        [Test]
        public void ToString_Deberia_Retornar_Nombre_Normalizado()
        {
            Assert.That(EstadoIndicador.Creado.ToString(), Is.EqualTo("CREADO"));
            Assert.That(EstadoIndicador.Actualizado.ToString(), Is.EqualTo("ACTUALIZADO"));
            Assert.That(EstadoIndicador.Consolidado.ToString(), Is.EqualTo("CONSOLIDADO"));
        }

        [Test]
        public void Transiciones_Validas_Deberian_Ser_Aceptadas()
        {
            // Idempotencia
            Assert.That(EstadoIndicador.EsTransicionValida(EstadoIndicador.Creado, EstadoIndicador.Creado), Is.True);
            Assert.That(EstadoIndicador.EsTransicionValida(EstadoIndicador.Actualizado, EstadoIndicador.Actualizado), Is.True);
            Assert.That(EstadoIndicador.EsTransicionValida(EstadoIndicador.Consolidado, EstadoIndicador.Consolidado), Is.True);

            // CREADO -> ACTUALIZADO / CONSOLIDADO
            Assert.That(EstadoIndicador.EsTransicionValida(EstadoIndicador.Creado, EstadoIndicador.Actualizado), Is.True);
            Assert.That(EstadoIndicador.EsTransicionValida(EstadoIndicador.Creado, EstadoIndicador.Consolidado), Is.True);

            // ACTUALIZADO -> CONSOLIDADO
            Assert.That(EstadoIndicador.EsTransicionValida(EstadoIndicador.Actualizado, EstadoIndicador.Consolidado), Is.True);
        }

        [Test]
        public void Transiciones_Invalidas_Deberian_Ser_Rechazadas()
        {
            // Retrocesos
            Assert.That(EstadoIndicador.EsTransicionValida(EstadoIndicador.Actualizado, EstadoIndicador.Creado), Is.False);
            Assert.That(EstadoIndicador.EsTransicionValida(EstadoIndicador.Consolidado, EstadoIndicador.Creado), Is.False);
            Assert.That(EstadoIndicador.EsTransicionValida(EstadoIndicador.Consolidado, EstadoIndicador.Actualizado), Is.False);

            // Creado -> (otro que no sea Actualizado/Consolidado) no aplica porque solo hay 3
        }

        [Test]
        public void AsegurarTransicionValida_Deberia_NoLanzar_En_Validas_Y_Lanzar_En_Invalidas()
        {
            // No lanza
            Assert.DoesNotThrow(() => EstadoIndicador.AsegurarTransicionValida(EstadoIndicador.Creado, EstadoIndicador.Actualizado));
            Assert.DoesNotThrow(() => EstadoIndicador.AsegurarTransicionValida(EstadoIndicador.Creado, EstadoIndicador.Consolidado));
            Assert.DoesNotThrow(() => EstadoIndicador.AsegurarTransicionValida(EstadoIndicador.Actualizado, EstadoIndicador.Consolidado));
            Assert.DoesNotThrow(() => EstadoIndicador.AsegurarTransicionValida(EstadoIndicador.Creado, EstadoIndicador.Creado)); // idempotente

            // Lanza
            Assert.That(() => EstadoIndicador.AsegurarTransicionValida(EstadoIndicador.Actualizado, EstadoIndicador.Creado),
                Throws.Exception.TypeOf<InvalidOperationException>());
            Assert.That(() => EstadoIndicador.AsegurarTransicionValida(EstadoIndicador.Consolidado, EstadoIndicador.Creado),
                Throws.Exception.TypeOf<InvalidOperationException>());
            Assert.That(() => EstadoIndicador.AsegurarTransicionValida(EstadoIndicador.Consolidado, EstadoIndicador.Actualizado),
                Throws.Exception.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void Todos_Deberia_Tener_Tres_Estados_Unicos_Y_Los_Conocidos()
        {
            var todos = EstadoIndicador.Todos;

            Assert.That(todos, Is.Not.Null);
            Assert.That(todos.Count, Is.EqualTo(3));

            // Debe contener exactamente esas referencias (smart enum)
            Assert.That(todos.Contains(EstadoIndicador.Creado), Is.True);
            Assert.That(todos.Contains(EstadoIndicador.Actualizado), Is.True);
            Assert.That(todos.Contains(EstadoIndicador.Consolidado), Is.True);

            // Unicidad por referencia
            Assert.That(todos.Distinct().Count(), Is.EqualTo(3));
        }
    }
}
