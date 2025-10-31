using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CatalogoArticulosBC.Application.Services;
using CatalogoArticulosBC.Application.UseCases;
using NUnit.Framework;

namespace CatalogoArticulosBC.Tests.Application.Tests.UseCasesTests
{
    [TestFixture]
    public class DescargarPlantillaImportacionUseCaseTests
    {
        private DefaultImportSchemaProvider _schemaProvider = null!;
        private DescargarPlantillaImportacionUseCase? _useCase;

        [SetUp]
        public void SetUp()
        {
            _schemaProvider = new DefaultImportSchemaProvider();
            _useCase = new DescargarPlantillaImportacionUseCase(_schemaProvider);
        }

        [Test]
        public async Task Basica_DefaultFormat_GeneratesXlsxAndHeadersMatchProvider()
        {
            // Arrange
            var req = new DescargarPlantillaImportacionUseCase.Request(TipoPlantilla: "Basica", Formato: null);

            // Act
            var resp = await _useCase!.Handle(req);

            // Assert: extension and content-type
            Assert.That(resp.NombreArchivo, Does.StartWith("plantilla_"));
            Assert.That(resp.NombreArchivo, Does.EndWith(".xlsx"));
            Assert.That(resp.ContentType, Is.EqualTo("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));

            // Filename contains a date-like token YYYYMMDDHHMMSS
            var m = Regex.Match(resp.NombreArchivo, @"plantilla_(\d{14})\.xlsx");
            Assert.That(m.Success, Is.True, "El nombre del archivo debe incluir timestamp en formato yyyyMMddHHmmss.");

            // Headers must equal provider's Basica headers exactly in order
            var expected = _schemaProvider.GetBasicaHeaders().ToArray();
            Assert.That(resp.Cabeceras, Is.EqualTo(expected));
        }

        [Test]
        public async Task Completa_CsvFormat_GeneratesCsvAndHeadersMatchProvider()
        {
            // Arrange
            var req = new DescargarPlantillaImportacionUseCase.Request(TipoPlantilla: "Completa", Formato: "csv");

            // Act
            var resp = await _useCase!.Handle(req);

            // Assert: extension and content-type
            Assert.That(resp.NombreArchivo, Does.EndWith(".csv"));
            Assert.That(resp.ContentType, Is.EqualTo("text/csv; charset=utf-8"));

            // Headers must equal provider's Completa headers exactly in order
            var expected = _schemaProvider.GetCompletaHeaders().ToArray();
            Assert.That(resp.Cabeceras, Is.EqualTo(expected));
        }

        [Test]
        public void Tipo_Invalid_ThrowsArgumentException()
        {
            var req = new DescargarPlantillaImportacionUseCase.Request(TipoPlantilla: "NoExiste", Formato: "CSV");

            Assert.That(async () => await _useCase!.Handle(req), Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void Formato_Invalid_ThrowsArgumentException()
        {
            var req = new DescargarPlantillaImportacionUseCase.Request(TipoPlantilla: "Basica", Formato: "ZIP");

            Assert.That(async () => await _useCase!.Handle(req), Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public async Task Tipo_CaseInsensitive_AcceptsDifferentCasing()
        {
            var req = new DescargarPlantillaImportacionUseCase.Request(TipoPlantilla: "bAsIcA", Formato: "csv");
            var resp = await _useCase!.Handle(req);

            var expected = _schemaProvider.GetBasicaHeaders().ToArray();
            Assert.That(resp.Cabeceras, Is.EqualTo(expected));
        }

        [Test]
        public async Task MinimumRequiredHeaders_AreAtStartOfBothTemplates()
        {
            var minima = _schemaProvider.GetMinimumRequiredHeaders().ToArray();

            var basica = (await _useCase!.Handle(new DescargarPlantillaImportacionUseCase.Request("Basica"))).Cabeceras;
            var completa = (await _useCase.Handle(new DescargarPlantillaImportacionUseCase.Request("Completa"))).Cabeceras;

            // The first N headers must equal minima in order
            Assert.That(basica.Take(minima.Length), Is.EqualTo(minima));
            Assert.That(completa.Take(minima.Length), Is.EqualTo(minima));
        }
    }
}
