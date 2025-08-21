using NUnit.Framework;
using ListaPreciosBC.Domain.ValueObjects;

namespace ListaPreciosBC.Tests.UnitTests.ValueObjects
{
    [TestFixture]
    public class PrecioResueltoTests
    {
        private static ValorPrecio VP(decimal monto) => ValorPrecio.DesdeMonto(monto);

        [Test]
        public void Crea_con_valores_validos()
        {
            var r = new PrecioResuelto(VP(10m), PrecioResueltoOrigen.Fijo, 3);
            Assert.That(r.Valor.Monto, Is.EqualTo(10m));
            Assert.That(r.Origen, Is.EqualTo(PrecioResueltoOrigen.Fijo));
            Assert.That(r.CantidadSolicitada, Is.EqualTo(3));
            Assert.That(r.ToString(), Does.Contain("Fijo"));
        }

        [Test]
        public void Rechaza_valor_nulo()
        {
            Assert.That(() => new PrecioResuelto(null!, PrecioResueltoOrigen.Fijo, 1),
                Throws.ArgumentNullException);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Rechaza_cantidad_menor_a_1(int cantidad)
        {
            Assert.That(() => new PrecioResuelto(VP(5m), PrecioResueltoOrigen.PorVolumen, cantidad),
                Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        [Test]
        public void Igualdad_por_valor()
        {
            var a = new PrecioResuelto(VP(8m), PrecioResueltoOrigen.PorVolumen, 2);
            var b = new PrecioResuelto(VP(8m), PrecioResueltoOrigen.PorVolumen, 2);
            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }
    }
}
