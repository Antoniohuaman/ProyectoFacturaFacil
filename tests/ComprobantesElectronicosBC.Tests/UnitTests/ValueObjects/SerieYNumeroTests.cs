using System;
using NUnit.Framework;
using ComprobantesElectronicosBC.Domain.ValueObjects;

namespace ComprobantesElectronicosBC.Tests.UnitTests.ValueObjects
{
    public class SerieYNumeroTests
    {
        [Test]
        public void Create_ValidaYNormaliza_SerieUpper_NumeroDentroDeRango()
        {
            var vo = SerieYNumero.Create(" f001 ", 123);

            Assert.Multiple(() =>
            {
                Assert.That(vo.Serie, Is.EqualTo("F001"));
                Assert.That(vo.Numero, Is.EqualTo(123));
                Assert.That(vo.Numero8, Is.EqualTo("00000123"));
                Assert.That(vo.IdUbl, Is.EqualTo("F001-00000123"));
                Assert.That(vo.Value, Is.EqualTo("F001-00000123")); // alias compatible
                Assert.That(vo.ToString(), Is.EqualTo("F001-00000123"));
            });
        }

        [Test]
        public void Create_LanzaSiSerieInvalida()
        {
            // vacío
            Assert.That(() => SerieYNumero.Create("", 1),
                Throws.TypeOf<ArgumentException>());
            // demasiado larga
            Assert.That(() => SerieYNumero.Create("ABCDE", 1),
                Throws.TypeOf<ArgumentException>());
            // caracteres no permitidos
            Assert.That(() => SerieYNumero.Create("F00-", 1),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void Create_LanzaSiNumeroFueraDeRango()
        {
            Assert.That(() => SerieYNumero.Create("F001", 0),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => SerieYNumero.Create("F001", 100_000_000),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void TryParse_AceptaConGuionOEspacio_YNormaliza()
        {
            Assert.That(SerieYNumero.TryParse("f001-25", out var a), Is.True);
            Assert.That(a!.Serie, Is.EqualTo("F001"));
            Assert.That(a.Numero, Is.EqualTo(25));

            Assert.That(SerieYNumero.TryParse("B12 7", out var b), Is.True);
            Assert.That(b!.Serie, Is.EqualTo("B12"));
            Assert.That(b.Numero8, Is.EqualTo("00000007"));
        }

        [Test]
        public void TryParse_RechazaFormatoIncorrecto()
        {
            Assert.That(SerieYNumero.TryParse("F001", out _), Is.False);          // falta número
            Assert.That(SerieYNumero.TryParse("F001-000000000", out _), Is.False); // 9+ dígitos
            Assert.That(SerieYNumero.TryParse("F001-0", out _), Is.False);         // 0 no válido
            Assert.That(SerieYNumero.TryParse("F*01-1", out _), Is.False);         // serie inválida
        }

        [Test]
        public void EsCompatibleConTipo_ReglasBasicasFCon01_BCon03()
        {
            var f = SerieYNumero.Create("F001", 1);
            var b = SerieYNumero.Create("B001", 1);
            var x = SerieYNumero.Create("E001", 1); // otros tipos: true

            Assert.Multiple(() =>
            {
                Assert.That(f.EsCompatibleConTipo("01"), Is.True);
                Assert.That(f.EsCompatibleConTipo("03"), Is.False);
                Assert.That(b.EsCompatibleConTipo("03"), Is.True);
                Assert.That(b.EsCompatibleConTipo("01"), Is.False);
                Assert.That(x.EsCompatibleConTipo("07"), Is.True);
            });
        }

        [Test]
        public void Next_IncrementaNumero_YLanzaEnMaximo()
        {
            var v1 = SerieYNumero.Create("F001", 123);
            var v2 = v1.Next();

            Assert.Multiple(() =>
            {
                Assert.That(v2.Serie, Is.EqualTo("F001"));
                Assert.That(v2.Numero, Is.EqualTo(124));
            });

            var max = SerieYNumero.Create("F001", 99_999_999);
            Assert.That(() => max.Next(), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void IgualdadPorValor_MismoContenido_EsIgual()
        {
            var a = SerieYNumero.Create("F001", 5);
            var b = SerieYNumero.Create("f001", 5);
            var c = SerieYNumero.Create("F001", 6);

            Assert.Multiple(() =>
            {
                Assert.That(a, Is.EqualTo(b));
                Assert.That(a, Is.Not.EqualTo(c));
            });
        }

        [Test]
        public void Deconstruct_FuncionaYEntregaComponentes()
        {
            var vo = SerieYNumero.Create("B001", 77);
            var (serie, numero) = vo;

            Assert.Multiple(() =>
            {
                Assert.That(serie, Is.EqualTo("B001"));
                Assert.That(numero, Is.EqualTo(77));
            });
        }

        [Test]
        public void IdYValue_SonAliasesDeIdUbl()
        {
            var vo = SerieYNumero.Create("F001", 42);
            Assert.Multiple(() =>
            {
                Assert.That(vo.Id, Is.EqualTo(vo.IdUbl));
                Assert.That(vo.Value, Is.EqualTo(vo.IdUbl));
            });
        }
    }
}
