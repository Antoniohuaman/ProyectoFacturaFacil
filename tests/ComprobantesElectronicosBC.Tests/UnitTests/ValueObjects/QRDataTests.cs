using System;
using NUnit.Framework;
using ComprobantesElectronicosBC.Domain.ValueObjects;

namespace ComprobantesElectronicosBC.Tests.UnitTests.ValueObjects
{
    public class QRDataTests
    {
        [Test]
        public void BuildPayload_Completo_ConCompradorYHash_OK()
        {
            // Arrange
            var rucEmisor = "20600552849";
            var tipo = "01";                 // Factura
            var serie = "f001";              // se normaliza a MAYÚS
            var numero = 9135;               // -> 00009135
            var igv = 74.754m;               // se redondea a 74.75
            var total = 490.0m;              // 490.00
            var fecha = new DateOnly(2025, 07, 31);
            var tipoDocAdq = "6";            // RUC
            var nroDocAdq = "20606272741";
            var hash = "UBbi+vOuBGOt+6EnQHp/l4dMvNg=";

            // Act
            var qr = QRData.Create(
                rucEmisor, tipo, serie, numero,
                montoIgv: igv, importeTotal: total, fechaEmision: fecha,
                tipoDocAdquiriente: tipoDocAdq, numDocAdquiriente: nroDocAdq, digestValue: hash);

            // Assert
            var esperado = "20600552849|01|F001|00009135|74.75|490.00|2025-07-31|6|20606272741|UBbi+vOuBGOt+6EnQHp/l4dMvNg=";
            Assert.That(qr.Payload, Is.EqualTo(esperado));
            Assert.That(qr.ToString(), Is.EqualTo(esperado)); // si ToString() devuelve el payload
        }

        [Test]
        public void BuildPayload_SinCompradorNiHash_UsaGuiones_OK()
        {
            var qr = QRData.Create(
                rucEmisor: "20120571487",
                tipoCpe: "03",              // Boleta
                serie: "b010",
                numero: 405,
                montoIgv: 53.39m,
                importeTotal: 350.00m,
                fechaEmision: new DateOnly(2025, 07, 31),
                tipoDocAdquiriente: null,   // → "-"
                numDocAdquiriente: null,    // → "-"
                digestValue: null           // → "-"
            );

            var esperado = "20120571487|03|B010|00000405|53.39|350.00|2025-07-31|-|-|-";
            Assert.That(qr.Payload, Is.EqualTo(esperado));
        }

        [Test]
        public void Numero_SePadrea8_CuandoEsMenorA8Digitos()
        {
            var qr = QRData.Create(
                "20600552849", "01", "F1", 7,
                montoIgv: 0.00m, importeTotal: 10.00m, fechaEmision: new DateOnly(2025, 1, 1),
                tipoDocAdquiriente: null, numDocAdquiriente: null, digestValue: null
            );

            Assert.That(qr.Payload, Does.Contain("|F1|00000007|"));
        }

        [Test]
        public void Rechaza_RucEmisorInvalido()
        {
            Assert.That(
                () => QRData.Create(
                    "20600X52849", "01", "F001", 1,
                    montoIgv: 0.00m, importeTotal: 1.00m, fechaEmision: new DateOnly(2025, 1, 1),
                    tipoDocAdquiriente: null, numDocAdquiriente: null, digestValue: null),
                Throws.ArgumentException.With.Message.Contains("RUC"));
        }

        [Test]
        public void Rechaza_TipoCpeNoPermitido()
        {
            var ex = Assert.Throws<ArgumentException>(() => QRData.Create(
                "20600552849", "99", "F001", 1,
                montoIgv: 0.00m, importeTotal: 1.00m, fechaEmision: new DateOnly(2025, 1, 1),
                tipoDocAdquiriente: null, numDocAdquiriente: null, digestValue: null));
            Assert.That(ex!.Message.ToLower(), Does.Contain("tipo"));
        }

        [Test]
        public void Rechaza_SerieInvalida()
        {
            var ex = Assert.Throws<ArgumentException>(() => QRData.Create(
                "20600552849", "01", "F-01", 1,
                montoIgv: 0.00m, importeTotal: 1.00m, fechaEmision: new DateOnly(2025, 1, 1),
                tipoDocAdquiriente: null, numDocAdquiriente: null, digestValue: null));
            Assert.That(ex!.Message.ToLower(), Does.Contain("serie"));
        }

        [Test]
        public void Rechaza_NumeroFueraDeRango()
        {
            Assert.That(
                () => QRData.Create(
                    "20600552849", "01", "F001", 0,
                    montoIgv: 0.00m, importeTotal: 1.00m, fechaEmision: new DateOnly(2025, 1, 1),
                    tipoDocAdquiriente: null, numDocAdquiriente: null, digestValue: null),
                Throws.Exception);

            Assert.That(
                () => QRData.Create(
                    "20600552849", "01", "F001", 100_000_000,
                    montoIgv: 0.00m, importeTotal: 1.00m, fechaEmision: new DateOnly(2025, 1, 1),
                    tipoDocAdquiriente: null, numDocAdquiriente: null, digestValue: null),
                Throws.Exception);
        }

        [Test]
        public void Rechaza_IgvNegativo()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => QRData.Create(
                "20600552849", "01", "F001", 1,
                montoIgv: -0.01m, importeTotal: 10.00m, fechaEmision: new DateOnly(2025, 1, 1),
                tipoDocAdquiriente: null, numDocAdquiriente: null, digestValue: null));
            Assert.That(ex!.Message.ToLower(), Does.Contain("igv"));
        }

        [Test]
        public void Normaliza_SerieAMayusculas_Y_RedondeaDecimales()
        {
            var qr = QRData.Create(
                "20600552849", "01", "fabc", 12,
                montoIgv: 1.234m, importeTotal: 10.235m, fechaEmision: new DateOnly(2025, 1, 2),
                tipoDocAdquiriente: null, numDocAdquiriente: null, digestValue: null
            );

            var payload = qr.Payload;
            Assert.That(payload, Does.Contain("|FABC|00000012|"));  // serie a MAYÚS
            Assert.That(payload, Does.Contain("|1.23|10.24|"));     // redondeo 2 decimales
            Assert.That(payload, Does.Contain("|2025-01-02|"));     // fecha ISO
        }

        [Test]
        public void CompradorOpcional_UsaGuion_CuandoNoAplica()
        {
            var qr = QRData.Create(
                "20600552849", "01", "F001", 1,
                montoIgv: 0.00m, importeTotal: 1.00m, fechaEmision: new DateOnly(2025, 1, 1),
                tipoDocAdquiriente: null, numDocAdquiriente: null, digestValue: null
            );

            Assert.That(qr.Payload, Does.EndWith("|-|-|-"));
        }
    }
}
