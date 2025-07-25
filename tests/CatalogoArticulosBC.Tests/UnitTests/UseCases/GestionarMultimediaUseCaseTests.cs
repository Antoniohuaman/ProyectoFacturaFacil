using System;
using System.Threading.Tasks;
using CatalogoArticulosBC.Application.UseCases;
using CatalogoArticulosBC.Application.Interfaces;
using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.Entities;
using CatalogoArticulosBC.Domain.Exceptions;
using CatalogoArticulosBC.Domain.ValueObjects;
using Moq;
using NUnit.Framework;


namespace CatalogoArticulosBC.Tests
{
    [TestFixture]
    public class GestionarMultimediaUseCaseTests
    {
        private Mock<ICatalogoArticulosRepository> _repoMock = null!;
        private Mock<IUnitOfWork> _uowMock = null!;
        private GestionarMultimediaUseCase _useCase = null!;
        private ProductoSimple _producto = null!;

        [SetUp]
        public void SetUp()
        {
            _repoMock = new Mock<ICatalogoArticulosRepository>();
            _uowMock = new Mock<IUnitOfWork>();
            _producto = new ProductoSimple(
                sku: "SKU-001",
                nombre: "Producto Test",
                descripcion: "Desc",
                unidadMedida: new UnidadMedida("UND"),
                afectacionIgv: new AfectacionIGV("10"),
                precio: 10
            );
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(_producto);
            _useCase = new GestionarMultimediaUseCase(_repoMock.Object, _uowMock.Object);
        }

        [Test]
        public async Task AgregarAsync_AgregaMultimediaCorrectamente()
        {
            // Arrange
            var multimediaId = Guid.Empty; // Se genera dentro del método
            var tipoAdjunto = "image/jpeg";
            var nombreArchivo = "foto.jpg";
            var ruta = "/uploads/foto.jpg";
            var comentario = "Foto principal";
            var tamano = 1024 * 1024;

            // Act
            var resultId = await _useCase.AgregarAsync(
                _producto.ProductoId,
                tipoAdjunto,
                nombreArchivo,
                ruta,
                comentario,
                tamano
            );

            // Assert
            Assert.That(_producto.Multimedia, Has.Exactly(1).Matches<MultimediaProducto>(m =>
                m.TipoAdjunto == tipoAdjunto &&
                m.NombreArchivo == nombreArchivo &&
                m.Ruta == ruta &&
                m.Comentario == comentario &&
                m.Tamano == tamano
            ));
            _repoMock.Verify(r => r.UpdateAsync(_producto), Times.Once);
            _uowMock.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Test]
        public void AgregarAsync_LanzaExcepcion_SiTipoNoPermitido()
        {
            // Arrange
            var tipoAdjunto = "application/x-msdownload";
            var nombreArchivo = "archivo.exe";
            var ruta = "/uploads/archivo.exe";
            var comentario = "Archivo no permitido";
            var tamano = 1024;

            // Act & Assert
            Assert.ThrowsAsync<MultimediaInvalidaException>(async () =>
                await _useCase.AgregarAsync(
                    _producto.ProductoId,
                    tipoAdjunto,
                    nombreArchivo,
                    ruta,
                    comentario,
                    tamano
                )
            );
        }

        [Test]
        public async Task EliminarAsync_EliminaMultimediaCorrectamente()
        {
            // Arrange
            var multimediaId = Guid.NewGuid();
            _producto.AgregarMultimediaAvanzada(
                multimediaId,
                "image/jpeg",
                "foto.jpg",
                "/uploads/foto.jpg",
                "Foto principal",
                1024 * 1024
            );

            // Act
            await _useCase.EliminarAsync(_producto.ProductoId, multimediaId);

            // Assert
            Assert.That(_producto.Multimedia, Is.Empty);
            _repoMock.Verify(r => r.UpdateAsync(_producto), Times.Once);
            _uowMock.Verify(u => u.CommitAsync(), Times.Once);
        }
    }
}