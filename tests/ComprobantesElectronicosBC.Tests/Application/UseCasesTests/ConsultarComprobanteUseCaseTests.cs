using System;
using NUnit.Framework;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ComprobantesElectronicosBC.Application.UseCases.ConsultarComprobante;
using ComprobantesElectronicosBC.Domain.Aggregates;
using ComprobantesElectronicosBC.Domain.Repositories;
using Moq;
using SharedKernel.Exceptions;

namespace ComprobantesElectronicosBC.Tests.Application.UseCases
{
    public class ConsultarComprobanteUseCaseTests
    {
        private static ComprobanteElectronico DummyAgg()
            => (ComprobanteElectronico)RuntimeHelpers.GetUninitializedObject(typeof(ComprobanteElectronico));

        [Test]
        public async Task Consulta_por_Id_devuelve_output()
        {
            // Arrange
            var repo = new Mock<IComprobanteRepository>(MockBehavior.Strict);
            var mapper = new Mock<IConsultarComprobanteMapper>(MockBehavior.Strict);

            var id = Guid.NewGuid();
            var agg = DummyAgg();

            repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);

            var expected = new ConsultarComprobanteOutputDto
            {
                ComprobanteId = id,
                TipoComprobante = "01",
                Serie = "F001",
                Numero = 123,
                Estado = "Emitido",
                FechaEmision = new DateOnly(2025, 1, 2),
                Total = 150.25m,
                Moneda = "PEN",
                EmisorRuc = "20123456789",
                EmisorRazonSocial = "ACME S.A.C.",
                ClienteDocumento = "6-20654321987",
                ClienteNombre = "CLIENTE DEMO"
            };

            mapper.Setup(m => m.Map(agg)).Returns(expected);

            var sut = new ConsultarComprobanteUseCase(repo.Object, mapper.Object);

            // Act
            var outDto = await sut.ExecuteAsync(new ConsultarComprobanteInputDto { ComprobanteId = id });

            // Assert
            Assert.That(outDto.ComprobanteId, Is.EqualTo(id));
            Assert.That(outDto.SerieNumero, Is.EqualTo("F001-00000123"));

            repo.VerifyAll();
            mapper.VerifyAll();
        }

        [Test]
        public async Task Consulta_por_SerieNumero_devuelve_output()
        {
            // Arrange
            var repo = new Mock<IComprobanteRepository>(MockBehavior.Strict);
            var mapper = new Mock<IConsultarComprobanteMapper>(MockBehavior.Strict);

            var agg = DummyAgg();

            // Espera serie normalizada en mayúsculas y número válido
            repo.Setup(r => r.GetBySerieNumeroAsync("B001", 9, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);

            var expected = new ConsultarComprobanteOutputDto
            {
                ComprobanteId = Guid.NewGuid(),
                TipoComprobante = "03",
                Serie = "B001",
                Numero = 9,
                Estado = "Borrador",
                FechaEmision = new DateOnly(2025, 2, 10),
                Total = 10.00m,
                Moneda = "PEN",
                EmisorRuc = "20123456789",
                EmisorRazonSocial = "ACME S.A.C.",
                ClienteDocumento = "1-12345678",
                ClienteNombre = "Juan Pérez"
            };

            mapper.Setup(m => m.Map(agg)).Returns(expected);

            var sut = new ConsultarComprobanteUseCase(repo.Object, mapper.Object);

            // Act
            var outDto = await sut.ExecuteAsync(new ConsultarComprobanteInputDto { Serie = "b001", Numero = 9 });

            // Assert
            Assert.That(outDto.TipoComprobante, Is.EqualTo("03"));
            Assert.That(outDto.SerieNumero, Is.EqualTo("B001-00000009"));

            repo.VerifyAll();
            mapper.VerifyAll();
        }

        [Test]
public void Consulta_sin_criterios_lanza_ArgumentException()
{
    var repo = new Mock<IComprobanteRepository>();
    var mapper = new Mock<IConsultarComprobanteMapper>();
    var sut = new ConsultarComprobanteUseCase(repo.Object, mapper.Object);

    var ex = Assert.ThrowsAsync<ArgumentException>(() =>
        sut.ExecuteAsync(new ConsultarComprobanteInputDto()));

    Assert.That(ex, Is.Not.Null);
    Assert.That(ex!.Message, Does.Contain("Proporcione ComprobanteId o Serie y Número"));
}

        [Test]
        public void Consulta_no_encontrado_por_Id_lanza_NotFound()
        {
            var repo = new Mock<IComprobanteRepository>(MockBehavior.Strict);
            var mapper = new Mock<IConsultarComprobanteMapper>();
            var sut = new ConsultarComprobanteUseCase(repo.Object, mapper.Object);

            var id = Guid.NewGuid();
            repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ComprobanteElectronico?)null);

            Assert.ThrowsAsync<NotFoundException>(() =>
                sut.ExecuteAsync(new ConsultarComprobanteInputDto { ComprobanteId = id }));

            repo.VerifyAll();
        }

        [Test]
        public void Consulta_no_encontrado_por_SerieNumero_lanza_NotFound()
        {
            var repo = new Mock<IComprobanteRepository>(MockBehavior.Strict);
            var mapper = new Mock<IConsultarComprobanteMapper>();
            var sut = new ConsultarComprobanteUseCase(repo.Object, mapper.Object);

            repo.Setup(r => r.GetBySerieNumeroAsync("F001", 999, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ComprobanteElectronico?)null);

            Assert.ThrowsAsync<NotFoundException>(() =>
                sut.ExecuteAsync(new ConsultarComprobanteInputDto { Serie = "F001", Numero = 999 }));

            repo.VerifyAll();
        }

        [Test]
        public async Task Consulta_con_Id_y_SerieNumero_prioriza_Id()
        {
            var repo = new Mock<IComprobanteRepository>(MockBehavior.Strict);
            var mapper = new Mock<IConsultarComprobanteMapper>(MockBehavior.Strict);

            var id = Guid.NewGuid();
            var agg = DummyAgg();

            repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);

            var expected = new ConsultarComprobanteOutputDto
            {
                ComprobanteId = id,
                TipoComprobante = "01",
                Serie = "F001",
                Numero = 1,
                Estado = "Emitido",
                FechaEmision = new DateOnly(2025, 1, 1),
                Total = 1,
                Moneda = "PEN",
                EmisorRuc = "20123456789",
                EmisorRazonSocial = "ACME",
                ClienteDocumento = "6-20123456789",
                ClienteNombre = "CLIENTE"
            };

            mapper.Setup(m => m.Map(agg)).Returns(expected);

            var sut = new ConsultarComprobanteUseCase(repo.Object, mapper.Object);

            var outDto = await sut.ExecuteAsync(new ConsultarComprobanteInputDto
            {
                ComprobanteId = id,
                Serie = "B001",
                Numero = 77
            });

            Assert.That(outDto.ComprobanteId, Is.EqualTo(id));
            repo.VerifyAll();
            mapper.VerifyAll();
        }

        [Test]
        public void Serie_invalida_en_criterio_SerieNumero_propaga_ArgumentException()
        {
            var repo = new Mock<IComprobanteRepository>();
            var mapper = new Mock<IConsultarComprobanteMapper>();
            var sut = new ConsultarComprobanteUseCase(repo.Object, mapper.Object);

            // Serie de 5 caracteres (inválida para el VO SerieYNumero)
            Assert.ThrowsAsync<ArgumentException>(() =>
                sut.ExecuteAsync(new ConsultarComprobanteInputDto { Serie = "ABCDE", Numero = 1 }));
        }
    }
}
