#nullable enable
using System;
using CatalogoArticulosBC.Domain.ValueObjects;
using NUnit.Framework;
using SharedKernel.ValueObjects;

namespace CatalogoArticulosBC.Tests.Domain.Tests.ValueObjectsTests
{
    [TestFixture]
    public class PrecioCompraTests
    {
    private static Moneda PEN => Moneda.PEN();

        [Test]
        public void Desde_DeberiaCrearConMontoPositivoYMonedaValida()
        {
            // Arrange
            var monto = 25.456m;

            // Act
            var sut = PrecioCompra.Desde(monto, PEN);

            // Assert
            Assert.That(sut, Is.Not.Null);
            Assert.That(sut.Monto, Is.EqualTo(25.46m)); // redondeo a 2 decimales
            Assert.That(sut.Moneda, Is.EqualTo(PEN));
        }

        [Test]
        public void Desde_DeberiaPermitirMontoCero()
        {
            var sut = PrecioCompra.Desde(0m, PEN);

            Assert.That(sut.Monto, Is.EqualTo(0m));
            Assert.That(sut.Moneda, Is.EqualTo(PEN));
        }

        [Test]
        public void Desde_DeberiaLanzarSiMontoNegativo()
        {
            // Si cambias a DomainException, actualiza el tipo esperado.
            Assert.That(() => PrecioCompra.Desde(-0.01m, PEN),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Equals_DeberiaSerTrueParaMismoMontoYMoneda()
        {
            var a = PrecioCompra.Desde(10m, PEN);
            var b = PrecioCompra.Desde(10.00m, PEN);

            Assert.That(a.Equals(b), Is.True);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void ToString_DeberiaIncluirMonedaYCantidadFormateada()
        {
            var sut = PrecioCompra.Desde(12.5m, PEN);

            Assert.That(sut.ToString(), Does.Contain("PEN"));
            Assert.That(sut.ToString(), Does.Contain("12.50"));
        }

        [Test]
        public void DesdeNullable_DeberiaDevolverNullCuandoMontoNull()
        {
            PrecioCompra? sut = PrecioCompra.DesdeNullable(null, PEN);

            Assert.That(sut, Is.Null);
        }

        [Test]
        public void DesdeNullable_DeberiaCrearInstanciaCuandoMontoTieneValor()
        {
            PrecioCompra? sut = PrecioCompra.DesdeNullable(7.1m, PEN);

            Assert.That(sut, Is.Not.Null);
            Assert.That(sut!.Monto, Is.EqualTo(7.10m));
        }
    }
}
