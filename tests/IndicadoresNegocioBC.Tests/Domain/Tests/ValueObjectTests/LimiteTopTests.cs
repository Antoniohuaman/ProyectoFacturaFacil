using System;
using NUnit.Framework;
using IndicadoresNegocioBC.Domain.ValueObjects;

namespace IndicadoresNegocioBC.Tests.Domain.ValueObjects
{
    [TestFixture]
    public class LimiteTopTests
    {
        [Test]
        public void Crear_Valido_Bordes_1_y_Maximo()
        {
            var min = LimiteTop.Crear(1);
            Assert.That(min.Valor, Is.EqualTo(1));

            var max = LimiteTop.Crear(LimiteTop.MaximoPermitido);
            Assert.That(max.Valor, Is.EqualTo(LimiteTop.MaximoPermitido));
        }

        [Test]
        public void Crear_Invalido_MenorQue1_Deberia_Fallar()
        {
            Assert.That(() => LimiteTop.Crear(0), Throws.Exception.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => LimiteTop.Crear(-5), Throws.Exception.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Crear_Invalido_MayorQueMaximo_Deberia_Fallar()
        {
            Assert.That(() => LimiteTop.Crear(LimiteTop.MaximoPermitido + 1),
                Throws.Exception.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Singletons_Deberian_Tener_Valores_Esperados_Y_Equal_Por_Valor()
        {
            Assert.That(LimiteTop.Top5.Valor, Is.EqualTo(5));
            Assert.That(LimiteTop.Top10.Valor, Is.EqualTo(10));
            Assert.That(LimiteTop.Top20.Valor, Is.EqualTo(20));
            Assert.That(LimiteTop.Top50.Valor, Is.EqualTo(50));

            // Igualdad por valor (record)
            Assert.That(LimiteTop.Top10, Is.EqualTo(LimiteTop.Crear(10)));
            Assert.That(LimiteTop.Crear(20), Is.EqualTo(LimiteTop.Crear(20)));
            Assert.That(LimiteTop.Crear(20), Is.Not.EqualTo(LimiteTop.Crear(21)));
        }

        [Test]
        public void DesdeConTope_Clampea_Al_TopePersonalizado()
        {
            var top = LimiteTop.DesdeConTope(valor: 200, maximo: 30);
            Assert.That(top.Valor, Is.EqualTo(30));
        }

        [Test]
        public void DesdeConTope_Usa_MaximoPermitido_Cuando_Maximo_EsNull_O_NoValido()
        {
            // maximo = null -> usa MaximoPermitido
            var a = LimiteTop.DesdeConTope(valor: LimiteTop.MaximoPermitido + 50, maximo: null);
            Assert.That(a.Valor, Is.EqualTo(LimiteTop.MaximoPermitido));

            // maximo <= 0 -> no válido -> usa MaximoPermitido
            var b = LimiteTop.DesdeConTope(valor: 999, maximo: 0);
            Assert.That(b.Valor, Is.EqualTo(LimiteTop.MaximoPermitido));

            var c = LimiteTop.DesdeConTope(valor: 999, maximo: -10);
            Assert.That(c.Valor, Is.EqualTo(LimiteTop.MaximoPermitido));
        }

        [Test]
        public void DesdeConTope_Respeta_Valor_Cuando_No_Excede_El_Tope()
        {
            var top = LimiteTop.DesdeConTope(valor: 15, maximo: 20);
            Assert.That(top.Valor, Is.EqualTo(15));
        }

        [Test]
        public void DesdeConTope_Valor_MenorQue1_Deberia_Fallar()
        {
            // No hay clamp hacia abajo; el ctor valida y debe fallar
            Assert.That(() => LimiteTop.DesdeConTope(0, 50), Throws.Exception.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => LimiteTop.DesdeConTope(-1, 50), Throws.Exception.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void ToString_Deberia_Seguir_Formato()
        {
            Assert.That(LimiteTop.Crear(7).ToString(), Is.EqualTo("Top 7"));
            Assert.That(LimiteTop.Top5.ToString(), Is.EqualTo("Top 5"));
        }

        [Test]
        public void Igualdad_Por_Valor_Deberia_Cumplirse()
        {
            var a = LimiteTop.Crear(9);
            var b = LimiteTop.Crear(9);
            var c = LimiteTop.Crear(8);

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a, Is.Not.EqualTo(c));
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }
    }
}
