using System;
using NUnit.Framework;
using ConfiguracionSistemaBC.Domain.Aggregates;
using ConfiguracionSistemaBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace ConfiguracionSistemaBC.Tests.Domain.Aggregates
{
    [TestFixture]
    public class ConfiguracionEmpresa_AmbienteTests
    {
        private static Ruc RUC() => Ruc.From("20600893409");
        // Usa fábrica para Perú con línea y divisiones completas
        private static DomicilioFiscal DF() => DomicilioFiscal.FromPeru(
            linea: "Av. Siempre Viva 123",
            ubigeo: "150101",
            departamento: "LIMA",
            provincia: "LIMA",
            distrito: "LIMA",
            addressTypeCode: "0000");
        private static Moneda PEN() => Moneda.PEN();

        [Test]
        public void Transicion_de_Produccion_a_Prueba_lanza_excepcion()
        {
            var empresa = ConfiguracionEmpresa.RegistrarNueva(RUC(), "ACME SAC", DF(), PEN());
            Assert.That(empresa.Ambiente, Is.EqualTo(AmbienteFe.PRUEBA));

            // Pasamos a PRODUCCION (válido)
            empresa.CambiarAmbiente(AmbienteFe.PRODUCCION);
            Assert.That(empresa.Ambiente, Is.EqualTo(AmbienteFe.PRODUCCION));

            // Intento volver a PRUEBA: inválido
            Assert.That(
                () => empresa.CambiarAmbiente(AmbienteFe.PRUEBA),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("No es posible volver a PRUEBA"));
        }
    }
}
