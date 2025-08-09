using System;
using System.Linq;
using NUnit.Framework;
using ComprobantesElectronicosBC.Domain.ValueObjects;

namespace ComprobantesElectronicosBC.Tests.UnitTests.ValueObjects
{
    [TestFixture]
    public class CDRTests
    {
        [Test]
        public void Create_Aceptado_SinObservaciones_FijaFlagsYDatosBasicos()
        {
            var before = DateTimeOffset.UtcNow.AddSeconds(-1);

            var cdr = CDR.Create(
                codigoRespuesta: 0,
                mensaje: "Aceptado",
                notas: new[] { "  OK  ", "", "   " },
                rucEmisor: " 20600000001 ",
                tipoCpe: "01",
                serie: "F001",
                numero: "00001234"
            );

            var after = DateTimeOffset.UtcNow.AddSeconds(1);

            Assert.Multiple(() =>
            {
                Assert.That(cdr.CodigoRespuesta, Is.EqualTo(0));
                Assert.That(cdr.Mensaje, Is.EqualTo("Aceptado"));
                Assert.That(cdr.EsAceptado, Is.True);
                Assert.That(cdr.EsAceptadoSinObservaciones, Is.True);
                Assert.That(cdr.EsAceptadoConObservaciones, Is.False);
                Assert.That(cdr.EsRechazado, Is.False);
                Assert.That(cdr.EsErrorComunicacion, Is.False);
                Assert.That(cdr.EsResultadoFinal, Is.True);
                // Notas se normalizan y vacías se filtran
                Assert.That(cdr.Notas.Count, Is.EqualTo(1));
                Assert.That(cdr.Notas[0], Is.EqualTo("OK"));
                // Timestamp dentro del rango (Create sin fecha explícita usa UtcNow)
                Assert.That(cdr.FechaHoraRespuesta, Is.InRange(before, after));
                // Metadatos normalizados
                Assert.That(cdr.RucEmisor, Is.EqualTo("20600000001"));
                Assert.That(cdr.TipoCpe, Is.EqualTo("01"));
                Assert.That(cdr.Serie, Is.EqualTo("F001"));
                Assert.That(cdr.Numero, Is.EqualTo("00001234"));
            });
        }

        [Test]
        public void Create_AceptadoConObservaciones98_SetFlagsYRetieneNotas()
        {
            var cdr = CDR.CreateAceptadoConObservaciones(
                mensaje: "Aceptado con observaciones",
                notas: new[] { "Obs1", "Obs2" }
            );

            Assert.Multiple(() =>
            {
                Assert.That(cdr.CodigoRespuesta, Is.EqualTo(98));
                Assert.That(cdr.EsAceptado, Is.True);
                Assert.That(cdr.EsAceptadoConObservaciones, Is.True);
                Assert.That(cdr.EsAceptadoSinObservaciones, Is.False);
                Assert.That(cdr.Notas.Count, Is.EqualTo(2));
            });
        }

        [Test]
        public void CreateRechazado_ValidaRango_2000_3999_YFlags()
        {
            var cdr = CDR.CreateRechazado(2001, "RUC del receptor inválido", notas: new[] { "2010" });

            Assert.Multiple(() =>
            {
                Assert.That(cdr.CodigoRespuesta, Is.EqualTo(2001));
                Assert.That(cdr.EsRechazado, Is.True);
                Assert.That(cdr.EsResultadoFinal, Is.True);
                Assert.That(cdr.EsAceptado, Is.False);
                Assert.That(cdr.EsErrorComunicacion, Is.False);
            });

            var exLow = Assert.Throws<ArgumentOutOfRangeException>(() => CDR.CreateRechazado(1999, "msg"));
            var exHigh = Assert.Throws<ArgumentOutOfRangeException>(() => CDR.CreateRechazado(4000, "msg"));
            Assert.That(exLow, Is.Not.Null);
            Assert.That(exHigh, Is.Not.Null);
        }

        [Test]
        public void Create_ErrorComunicacion_EnRango100_199_SetFlag()
        {
            var cdr = CDR.Create(101, "Timeout en SUNAT");

            Assert.Multiple(() =>
            {
                Assert.That(cdr.EsErrorComunicacion, Is.True);
                Assert.That(cdr.EsResultadoFinal, Is.False);
                Assert.That(cdr.EsAceptado, Is.False);
                Assert.That(cdr.EsRechazado, Is.False);
            });
        }

        [Test]
        public void Create_MensajeObligatorio_ThrowSiVacio()
        {
            Assert.Throws<ArgumentException>(() => CDR.Create(0, " "));
        }

        [Test]
        public void Create_ArchivoZip_ReglasDeConsistencia()
        {
            // Si hay zip, debe haber nombre
            var zip = new byte[] { 1, 2, 3 };
            Assert.Throws<ArgumentException>(() =>
                CDR.Create(0, "OK", archivoZip: zip, nombreArchivoZip: null));

            // Si hay nombre sin zip → error
            Assert.Throws<ArgumentException>(() =>
                CDR.Create(0, "OK", archivoZip: null, nombreArchivoZip: "R-20600000001-01-F001-1.zip"));

            // Correcto: nombre + zip
            var cdr = CDR.Create(
                0, "OK",
                archivoZip: zip,
                nombreArchivoZip: "R-20600000001-01-F001-1.zip"
            );

            Assert.Multiple(() =>
            {
                Assert.That(cdr.TieneArchivoZip, Is.True);
                Assert.That(cdr.NombreArchivoZip, Is.EqualTo("R-20600000001-01-F001-1.zip"));
                Assert.That(cdr.ArchivoZip!.Value.Length, Is.EqualTo(3));
            });
        }

        [Test]
        public void WithArchivoZip_NoMutaInstancia_DevuelveNuevaConZip()
        {
            var cdr0 = CDR.Create(98, "Aceptado con observaciones", notas: new[] { "Obs" });
            var cdr1 = cdr0.WithArchivoZip("cdr.zip", new byte[] { 9, 9 });

            Assert.Multiple(() =>
            {
                // Original sin zip
                Assert.That(cdr0.TieneArchivoZip, Is.False);
                Assert.That(cdr0.NombreArchivoZip, Is.Null);
                // Nueva con zip
                Assert.That(cdr1.TieneArchivoZip, Is.True);
                Assert.That(cdr1.NombreArchivoZip, Is.EqualTo("cdr.zip"));
                Assert.That(cdr1.ArchivoZip!.Value.Length, Is.EqualTo(2));
                // Igualdad estructural: difieren por el zip/nombre → no iguales
                Assert.That(cdr1, Is.Not.EqualTo(cdr0));
            });
        }

        [Test]
        public void ToResumen_IncluyeEstadoCodigoDocumentoYNotas()
        {
            var cdr = CDR.Create(
                0, "OK",
                notas: new[] { "N1", "N2" },
                rucEmisor: "20600000001",
                tipoCpe: "01",
                serie: "F001",
                numero: "123"
            );

            var resumen = cdr.ToResumen();

            Assert.Multiple(() =>
            {
                Assert.That(resumen, Does.Contain("[ACEPTADO]"));
                Assert.That(resumen, Does.Contain("Cod=0"));
                Assert.That(resumen, Does.Contain("Doc=01-F001-123"));
                Assert.That(resumen, Does.Contain("Msg='OK'"));
                Assert.That(resumen, Does.Contain("Notas: N1 | N2"));
            });
        }

        [Test]
        public void IgualdadPorValor_MismoContenido_Iguales_DistintoContenido_NoIguales()
        {
            var fecha = DateTimeOffset.UtcNow;
            var a = CDR.Create(0, "OK", fechaHoraRespuesta: fecha, notas: new[] { "n1" }, rucEmisor: "206", tipoCpe: "01", serie: "F001", numero: "1");
            var b = CDR.Create(0, "OK", fechaHoraRespuesta: fecha, notas: new[] { "n1" }, rucEmisor: "206", tipoCpe: "01", serie: "F001", numero: "1");
            var c = CDR.Create(98, "OK", fechaHoraRespuesta: fecha, notas: new[] { "n1" }, rucEmisor: "206", tipoCpe: "01", serie: "F001", numero: "1");

            Assert.Multiple(() =>
            {
                Assert.That(a.CodigoRespuesta, Is.EqualTo(b.CodigoRespuesta));
                Assert.That(a.Mensaje, Is.EqualTo(b.Mensaje));
                Assert.That(a.FechaHoraRespuesta, Is.EqualTo(b.FechaHoraRespuesta));
                Assert.That(a.Notas, Is.EquivalentTo(b.Notas));
                Assert.That(a.RucEmisor, Is.EqualTo(b.RucEmisor));
                Assert.That(a.TipoCpe, Is.EqualTo(b.TipoCpe));
                Assert.That(a.Serie, Is.EqualTo(b.Serie));
                Assert.That(a.Numero, Is.EqualTo(b.Numero));
            });
            Assert.That(a.CodigoRespuesta, Is.Not.EqualTo(c.CodigoRespuesta));
        }
    }
}
