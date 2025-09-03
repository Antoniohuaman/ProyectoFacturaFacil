using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ComprobantesElectronicosBC.Application.DTOs;
using ComprobantesElectronicosBC.Application.Interfaces;
using ComprobantesElectronicosBC.Application.ReadModels;
using ComprobantesElectronicosBC.Application.UseCases;
using Moq;
using NUnit.Framework;

namespace ComprobantesElectronicosBC.Tests.Application.UseCasesTests
{
    public class ListarComprobantesUseCaseTests
    {
        // -------------------------
        // Helpers
        // -------------------------

        private static Mock<IComprobanteQueryRepository> MockQueryRepoReturning(
            IReadOnlyList<ComprobanteResumenDto> items, int total,
            Action<DateOnly?, DateOnly?, string?, bool, int, int>? capture = null)
        {
            var repo = new Mock<IComprobanteQueryRepository>(MockBehavior.Strict);

            repo
                .Setup(r => r.ListarAsync(
                    It.IsAny<DateOnly?>(),
                    It.IsAny<DateOnly?>(),
                    It.IsAny<string?>(),
                    It.IsAny<bool>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .Callback<DateOnly?, DateOnly?, string?, bool, int, int, CancellationToken>(
                    (d, h, s, desc, pn, ps, _) => capture?.Invoke(d, h, s, desc, pn, ps))
                .ReturnsAsync((items, total));

            return repo;
        }

        // -------------------------
        // Tests
        // -------------------------

        [Test]
        public async Task Retorna_pagina_vacia_con_metadatos_estables()
        {
            // Arrange
            var repo = MockQueryRepoReturning(Array.Empty<ComprobanteResumenDto>(), total: 0);
            var sut  = new ListarComprobantesUseCase(repo.Object);

            var input = new ListarComprobantesInputDto
            {
                PageNumber    = 1,
                PageSize      = 10,
                SortBy        = null,
                SortDirection = null,
                Desde         = null,
                Hasta         = null
            };

            // Act
            var result = await sut.ExecuteAsync(input);

            // Assert
            Assert.That(result.Items, Is.Empty);
            Assert.That(result.TotalItems, Is.EqualTo(0));
            Assert.That(result.PageNumber, Is.EqualTo(1));
            Assert.That(result.PageSize, Is.EqualTo(10));
            Assert.That(result.TotalPages, Is.EqualTo(1));
            Assert.That(result.HasPreviousPage, Is.False);
            Assert.That(result.HasNextPage, Is.False);
            Assert.That(result.SortBy, Is.EqualTo("IssueDate"));        // default
            Assert.That(result.SortDirection, Is.EqualTo("DESC"));      // default
        }

        [Test]
        public async Task Normaliza_entrada_page_y_rango_de_fechas_y_orden()
        {
            // Arrange
            DateOnly? capturadoDesde = null;
            DateOnly? capturadoHasta = null;
            string?  capturadoSort   = null;
            bool     capturadoDesc   = false;
            int      capturadoPage   = 0;
            int      capturadoSize   = 0;

            var repo = MockQueryRepoReturning(
                Array.Empty<ComprobanteResumenDto>(),
                total: 0,
                capture: (d, h, s, desc, pn, ps) =>
                {
                    capturadoDesde = d;
                    capturadoHasta = h;
                    capturadoSort  = s;
                    capturadoDesc  = desc;
                    capturadoPage  = pn;
                    capturadoSize  = ps;
                });

            var sut = new ListarComprobantesUseCase(repo.Object);

            // Rango invertido y parámetros "raros"
            var desde = new DateOnly(2025,  5, 20);
            var hasta = new DateOnly(2025,  5, 10);

            var input = new ListarComprobantesInputDto
            {
                PageNumber    = 0,          // => 1
                PageSize      = 0,          // => DefaultPageSize (20)
                SortBy        = "  ",
                SortDirection = "abajo",    // => cualquier cosa != ASC => DESC
                Desde         = desde,
                Hasta         = hasta
            };

            // Act
            var result = await sut.ExecuteAsync(input);

            // Assert: lo enviado al repo ya normalizado
            Assert.That(capturadoPage, Is.EqualTo(1));
            Assert.That(capturadoSize, Is.EqualTo(ListarComprobantesUseCase.DefaultPageSize));
            Assert.That(capturadoSort, Is.EqualTo("IssueDate"));
            Assert.That(capturadoDesc, Is.True);

            // Se hizo swap del rango
            Assert.That(capturadoDesde, Is.EqualTo(hasta));
            Assert.That(capturadoHasta, Is.EqualTo(desde));

            // Y la salida queda consistente con lo normalizado
            Assert.That(result.PageNumber, Is.EqualTo(1));
            Assert.That(result.PageSize, Is.EqualTo(ListarComprobantesUseCase.DefaultPageSize));
            Assert.That(result.SortBy, Is.EqualTo("IssueDate"));
            Assert.That(result.SortDirection, Is.EqualTo("DESC"));
        }

        [Test]
        public async Task Calcula_TotalPages_y_flags_HasPrevious_HasNext()
        {
            // Arrange
            // Simulamos un total de 55 elementos; con pageSize=20 => totalPages=3
            var total = 55;
            var page  = 2;
            var size  = 20;

            var repo = MockQueryRepoReturning(Array.Empty<ComprobanteResumenDto>(), total);
            var sut  = new ListarComprobantesUseCase(repo.Object);

            var input = new ListarComprobantesInputDto
            {
                PageNumber    = page,
                PageSize      = size,
                SortBy        = "IssueDate",
                SortDirection = "ASC"
            };

            // Act
            var result = await sut.ExecuteAsync(input);

            // Assert
            Assert.That(result.TotalItems, Is.EqualTo(total));
            Assert.That(result.TotalPages, Is.EqualTo(3));
            Assert.That(result.PageNumber, Is.EqualTo(page));
            Assert.That(result.PageSize, Is.EqualTo(size));
            Assert.That(result.HasPreviousPage, Is.True);
            Assert.That(result.HasNextPage, Is.True);
            Assert.That(result.SortDirection, Is.EqualTo("ASC"));
        }
    }
}
