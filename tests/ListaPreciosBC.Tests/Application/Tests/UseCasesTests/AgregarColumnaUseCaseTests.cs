using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using ListaPreciosBC.Application.UseCases;
using ListaPreciosBC.Application.DTOs;
using ListaPreciosBC.Domain.Aggregates;
using ListaPreciosBC.Domain.ValueObjects;
using ListaPreciosBC.Domain.Repositories;
using ListaPreciosBC.Application.Interfaces;

namespace ListaPreciosBC.Tests.Application.Tests.UseCasesTests
{
    [TestFixture]
    public class AgregarColumnaUseCaseTests
    {
        [Test]
        public async Task ExecuteAsync_AgregaColumnaCorrectamente_RetornaDtoEsperado()
        {
            // Arrange
            var listaPrecioId = Guid.NewGuid();
            var usuario = "testuser";
            var cuando = DateTime.UtcNow;
            var dto = new AgregarColumnaDto
            {
                ListaPrecioId = listaPrecioId,
                NumeroColumna = 1, // P1
                Nombre = "Precio Especial",
                Modo = ModoValorizacionColumna.Fijo,
                EsBase = false,
                Visible = true,
                Orden = 2,
                Usuario = usuario,
                Cuando = cuando
            };

            // La plantilla inicial tiene una columna diferente (P2)
            var columnaInicial = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.DesdeNumero(2),
                NombreColumnaPrecio.Crear("Base"),
                ModoValorizacionColumna.Fijo,
                esBase: true,
                visible: true,
                orden: 1
            );
            var plantillaColumnas = PlantillaColumnasPrecio.Crear(new[] { columnaInicial });
            var listaPrecio = ListaPrecio.CrearNueva(listaPrecioId, plantillaColumnas, usuario, cuando);

            var repoMock = new Mock<IListaPrecioRepository>();
            repoMock.Setup(x => x.ObtenerPorIdAsync(listaPrecioId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(listaPrecio);
            repoMock.Setup(x => x.GuardarAsync(listaPrecio, 0, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var uowMock = new Mock<IUnitOfWork>();
            uowMock.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

            var useCase = new AgregarColumnaUseCase(repoMock.Object, uowMock.Object);

            // Act
            var result = await useCase.ExecuteAsync(dto);

            // Assert
            Assert.That(result.ListaPrecioId, Is.EqualTo(listaPrecioId));
            Assert.That(result.Nombre.ToString(), Is.EqualTo(dto.Nombre));
            Assert.That(result.Orden, Is.EqualTo(dto.Orden));
            Assert.That(result.EsBase, Is.EqualTo(dto.EsBase));
            Assert.That(result.Visible, Is.EqualTo(dto.Visible));
            Assert.That(result.Modo, Is.EqualTo(dto.Modo));
        }
    }
}
