using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ListaPreciosBC.Domain.Policies;
using ListaPreciosBC.Domain.ValueObjects;

namespace ListaPreciosBC.Tests.Domain.Tests.BusinessRulesTests.PoliciesTests
{
    [TestFixture]
    public class PuedeAgregarColumnaPolicyTests
    {
        private static IdentificadorColumnaPrecio Id(int numero)
        {
            if (IdentificadorColumnaPrecio.TryDesdeNumero((byte)numero, out var id))
            {
                return id!;
            }

            var ctor = typeof(IdentificadorColumnaPrecio)
                .GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(string), typeof(byte) }, null)
                ?? throw new InvalidOperationException("No se pudo acceder al constructor interno de IdentificadorColumnaPrecio.");

            return (IdentificadorColumnaPrecio)ctor.Invoke(new object[] { $"P{numero}", (byte)numero });
        }

        [Test]
        public void Validar_DeberiaRetornarTrue_CuandoHayMenosDeMaxOrden()
        {
            var columnas = Enumerable.Range(1, ConfiguracionColumnaPrecio.MaxOrden - 1)
                .Select(i => ConfiguracionColumnaPrecio.Crear(
                    Id(i),
                    NombreColumnaPrecio.Crear($"Columna{i}"),
                    ModoValorizacionColumna.Fijo,
                    orden: (byte)i
                )).ToList();
            var resultado = PuedeAgregarColumnaPolicy.Validar(columnas);
            Assert.That(resultado, Is.True);
        }

        [Test]
        public void Validar_DeberiaRetornarFalse_CuandoHayMaxOrden()
        {
            var columnas = Enumerable.Range(1, ConfiguracionColumnaPrecio.MaxOrden)
                .Select(i => ConfiguracionColumnaPrecio.Crear(
                    Id(i),
                    NombreColumnaPrecio.Crear($"Columna{i}"),
                    ModoValorizacionColumna.Fijo,
                    orden: (byte)i
                )).ToList();
            var resultado = PuedeAgregarColumnaPolicy.Validar(columnas);
            Assert.That(resultado, Is.False);
        }

        [Test]
        public void Validar_DeberiaRetornarTrue_CuandoNoHayColumnas()
        {
            var columnas = new List<ConfiguracionColumnaPrecio>();
            var resultado = PuedeAgregarColumnaPolicy.Validar(columnas);
            Assert.That(resultado, Is.True);
        }
    }
}
