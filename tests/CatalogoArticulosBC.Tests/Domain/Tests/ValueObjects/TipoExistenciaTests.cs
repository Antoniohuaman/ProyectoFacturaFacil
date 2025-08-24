using System;
using System.Linq;
using NUnit.Framework;
using CatalogoArticulosBC.Domain.ValueObjects;

namespace CatalogoArticulosBC.Domain.Tests.ValueObjects
{
    [TestFixture]
    public class TipoExistenciaTests
    {
        [Test]
        public void Enum_TieneDiezValores_Estables()
        {
            var valores = Enum.GetValues(typeof(TipoExistencia))
                              .Cast<TipoExistencia>()
                              .ToArray();

            Assert.That(valores.Length, Is.EqualTo(10), "El enum debe tener exactamente 10 valores.");

            Assert.That((int)TipoExistencia.Mercaderias,         Is.EqualTo(1));
            Assert.That((int)TipoExistencia.ProductosTerminados, Is.EqualTo(2));
            Assert.That((int)TipoExistencia.Servicios,           Is.EqualTo(3));
            Assert.That((int)TipoExistencia.MateriasPrimas,      Is.EqualTo(4));
            Assert.That((int)TipoExistencia.Envases,             Is.EqualTo(5));
            Assert.That((int)TipoExistencia.MaterialesAuxiliares,Is.EqualTo(6));
            Assert.That((int)TipoExistencia.Suministros,         Is.EqualTo(7));
            Assert.That((int)TipoExistencia.Repuestos,           Is.EqualTo(8));
            Assert.That((int)TipoExistencia.Embalajes,           Is.EqualTo(9));
            Assert.That((int)TipoExistencia.Otros,               Is.EqualTo(10));
        }

        [TestCase(TipoExistencia.Mercaderias,          "Mercaderías")]
        [TestCase(TipoExistencia.ProductosTerminados,  "Productos Terminados")]
        [TestCase(TipoExistencia.Servicios,            "Servicios")]
        [TestCase(TipoExistencia.MateriasPrimas,       "Materias Primas")]
        [TestCase(TipoExistencia.Envases,              "Envases")]
        [TestCase(TipoExistencia.MaterialesAuxiliares, "Materiales Auxiliares")]
        [TestCase(TipoExistencia.Suministros,          "Suministros")]
        [TestCase(TipoExistencia.Repuestos,            "Repuestos")]
        [TestCase(TipoExistencia.Embalajes,            "Embalajes")]
        [TestCase(TipoExistencia.Otros,                "Otros")]
        public void Descripcion_ParaCadaValor_EsLaEsperada(TipoExistencia tipo, string esperado)
        {
            var texto = tipo.Descripcion();

            Assert.That(texto, Is.EqualTo(esperado));
        }

        [Test]
        public void Descripcion_TodosSonUnicos_SinDuplicados()
        {
            var descripciones = Enum.GetValues(typeof(TipoExistencia))
                                    .Cast<TipoExistencia>()
                                    .Select(t => t.Descripcion())
                                    .ToArray();

            Assert.That(descripciones.Distinct().Count(), Is.EqualTo(descripciones.Length));
        }

        [Test]
        public void Descripcion_ValorDesconocido_LanzaNotSupportedException()
        {
            var invalido = (TipoExistencia)999;

            TestDelegate act = () => _ = invalido.Descripcion();

            Assert.That(act, Throws.TypeOf<NotSupportedException>()
                .With.Message.Contains("999"));
        }
    }
}
