using System;
using System.Collections.Generic;
using NUnit.Framework;
using ConfiguracionSistemaBC.Domain.ValueObjects;

namespace ConfiguracionSistemaBC.Tests.UnitTests.ValueObjects
{
    [TestFixture]
    public class AmbienteFeTests
    {
        [Test]
        public void Create_Valido_NormalizaYDevuelveInstanciaCanonica()
        {
            var a = AmbienteFe.Create("prueba");
            var b = AmbienteFe.Create("PRODUCCION");

            Assert.That(a, Is.SameAs(AmbienteFe.PRUEBA));
            Assert.That(b, Is.SameAs(AmbienteFe.PRODUCCION));
            Assert.That(a.EsPrueba, Is.True);
            Assert.That(b.EsProduccion, Is.True);
        }

        [Test]
        public void Create_Invalido_LanzaExcepcionAdecuada()
        {
            Assert.That(() => AmbienteFe.Create(null!), Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => AmbienteFe.Create("dev"), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => AmbienteFe.Create(""), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => AmbienteFe.Create("   "), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void TryCreate_DevuelveTrueSoloParaValoresValidos()
        {
            Assert.That(AmbienteFe.TryCreate("prueba", out var a), Is.True);
            Assert.That(a, Is.SameAs(AmbienteFe.PRUEBA));

            Assert.That(AmbienteFe.TryCreate("PRODUCCION", out var b), Is.True);
            Assert.That(b, Is.SameAs(AmbienteFe.PRODUCCION));

            Assert.That(AmbienteFe.TryCreate("dev", out _), Is.False);
            Assert.That(AmbienteFe.TryCreate("", out _), Is.False);
            Assert.That(AmbienteFe.TryCreate(null, out _), Is.False);
        }

        [Test]
        public void All_ContieneSoloLasDosInstanciasCanonicas_SinDuplicados()
        {
            Assert.That(AmbienteFe.All, Has.Member(AmbienteFe.PRUEBA));
            Assert.That(AmbienteFe.All, Has.Member(AmbienteFe.PRODUCCION));
            Assert.That(new HashSet<AmbienteFe>(AmbienteFe.All).Count, Is.EqualTo(2));
        }

        [Test]
        public void Flags_EsPrueba_EsProduccion_SonCoherentes()
        {
            var prueba = AmbienteFe.PRUEBA;
            var prod = AmbienteFe.PRODUCCION;

            Assert.That(prueba.EsPrueba, Is.True);
            Assert.That(prueba.EsProduccion, Is.False);

            Assert.That(prod.EsProduccion, Is.True);
            Assert.That(prod.EsPrueba, Is.False);
        }

        [Test]
        public void IgualdadPorValor_Y_Operadores_DebenFuncionar()
        {
            var a1 = AmbienteFe.Create("PRUEBA");
            var a2 = AmbienteFe.PRUEBA;
            var b  = AmbienteFe.PRODUCCION;

            Assert.That(a1.Equals(a2), Is.True);
            Assert.That(a1 == a2, Is.True);
            Assert.That(a1 != a2, Is.False);
            Assert.That(a1.Equals(b), Is.False);
            Assert.That(a1.GetHashCode(), Is.EqualTo(a2.GetHashCode()));
        }

        [Test]
        public void Conversiones_ToString_Implicit_Y_Explicit()
        {
            Assert.That(AmbienteFe.PRUEBA.ToString(), Is.EqualTo("PRUEBA"));
            Assert.That(AmbienteFe.PRODUCCION.ToString(), Is.EqualTo("PRODUCCION"));

            string sPrueba = AmbienteFe.PRUEBA;     // implícita
            string sProd   = AmbienteFe.PRODUCCION; // implícita
            Assert.That(sPrueba, Is.EqualTo("PRUEBA"));
            Assert.That(sProd,   Is.EqualTo("PRODUCCION"));

            var exp1 = (AmbienteFe)"prueba";        // explícita
            var exp2 = (AmbienteFe)"PRODUCCION";    // explícita
            Assert.That(exp1, Is.SameAs(AmbienteFe.PRUEBA));
            Assert.That(exp2, Is.SameAs(AmbienteFe.PRODUCCION));
        }

        [Test]
        public void EsTransicionValida_RespetaReglaDeIrreversibilidad()
        {
            var prueba = AmbienteFe.PRUEBA;
            var prod   = AmbienteFe.PRODUCCION;

            // Válidas
            Assert.That(prueba.EsTransicionValida(prueba), Is.True);   // idempotente
            Assert.That(prueba.EsTransicionValida(prod),   Is.True);
            Assert.That(prod.EsTransicionValida(prod),     Is.True);   // idempotente

            // Inválida
            Assert.That(prod.EsTransicionValida(prueba), Is.False);
        }

        [Test]
        public void ValidarTransicion_LanzaSoloEnCasoIrreversible()
        {
            var prueba = AmbienteFe.PRUEBA;
            var prod   = AmbienteFe.PRODUCCION;

            // No lanza
            Assert.DoesNotThrow(() => AmbienteFe.ValidarTransicion(prueba, prueba));
            Assert.DoesNotThrow(() => AmbienteFe.ValidarTransicion(prueba, prod));
            Assert.DoesNotThrow(() => AmbienteFe.ValidarTransicion(prod,   prod));

            // Lanza en PRODUCCION -> PRUEBA
            Assert.That(() => AmbienteFe.ValidarTransicion(prod, prueba), Throws.TypeOf<InvalidOperationException>());

            // Nulos
            Assert.That(() => AmbienteFe.ValidarTransicion(null!, prod),   Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => AmbienteFe.ValidarTransicion(prueba, null!), Throws.TypeOf<ArgumentNullException>());
        }
    }
}