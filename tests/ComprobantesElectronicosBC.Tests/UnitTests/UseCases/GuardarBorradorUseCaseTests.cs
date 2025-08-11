using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

using ComprobantesElectronicosBC.Application.DTOs;
using ComprobantesElectronicosBC.Application.UseCases;

// Usamos los InMemory del proyecto Adapters:
using ComprobantesElectronicosBC.Adapters.Output.Persistence.InMemory;

namespace ComprobantesElectronicosBC.Tests.UnitTests.UseCases
{
    [TestFixture]
    public class GuardarBorradorUseCaseTests
    {
        private static GuardarBorradorLineaInput L(
            string nombre, decimal qty, decimal precio, bool incluyeIgv,
            string afectacion = "10", decimal? igvRate = 0.18m,
            decimal? descPct = null, decimal? descMonto = null) =>
            new GuardarBorradorLineaInput(
                Nombre: nombre,
                Detalle: null,
                UmCodigo: "NIU",
                UmNombre: "Unidad",
                Cantidad: qty,
                PrecioUnitario: precio,
                PrecioIncluyeIgv: incluyeIgv,
                AfectacionCode: afectacion,
                IgvRate: igvRate,
                DescuentoPorcentaje: descPct,
                DescuentoMonto: descMonto
            );

        [Test]
        public async Task Crear_Borrador_ConLineas_OK()
        {
            var repo = new InMemoryComprobanteRepository();
            var uow  = new InMemoryUnitOfWork();
            var uc   = new GuardarBorradorUseCase(repo, uow);

            var input = new GuardarBorradorInput(
                TipoCodigo: "03",
                MonedaCodigo: "PEN",
                FechaEmision: DateOnly.FromDateTime(DateTime.Now),
                FormaPagoCodigo: "10",
                MetodoPagoCodigo: "EFECTIVO",
                MetodoPagoNombre: null,
                DiasCredito: null,
                EmisorRuc: "20123456789",
                EmisorRazonSocial: "MI EMPRESA SAC",
                EmisorUbigeo: "150101",
                EmisorDireccion: "Av. Principal 123",
                EmisorDepartamento: "Lima",
                EmisorProvincia: "Lima",
                EmisorDistrito: "Lima",
                ClienteDocTipo: "1",
                ClienteDocNumero: "12345678",
                ClienteNombre: "Juan Perez",
                ClienteUbigeo: "150101",
                ClienteDireccion: "Calle 1",
                ClienteDepartamento: "Lima",
                ClienteProvincia: "Lima",
                ClienteDistrito: "Lima",
                DescuentoGlobalPorcentaje: null,
                DescuentoGlobalMonto: null,
                Lineas: new List<GuardarBorradorLineaInput>
                {
                    L("Prod A", 2m, 50m, incluyeIgv:false),
                    L("Prod B", 1m, 118m, incluyeIgv:true)
                }
            );

            var o = await uc.Handle(input);

            Assert.That(o.ComprobanteId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(o.Estado, Is.EqualTo("DRAFT"));
            Assert.That(o.Total, Is.GreaterThan(0m));
        }

        [Test]
        public async Task Crear_Borrador_SinLineas_OK_TotalesCero()
        {
            var repo = new InMemoryComprobanteRepository();
            var uow  = new InMemoryUnitOfWork();
            var uc   = new GuardarBorradorUseCase(repo, uow);

            var input = new GuardarBorradorInput(
                TipoCodigo: "01",
                MonedaCodigo: "PEN",
                FechaEmision: DateOnly.FromDateTime(DateTime.Now),
                FormaPagoCodigo: "20",
                MetodoPagoCodigo: null,
                MetodoPagoNombre: null,
                DiasCredito: 30,
                EmisorRuc: "20123456789",
                EmisorRazonSocial: "MI EMPRESA SAC",
                EmisorUbigeo: "150101",
                EmisorDireccion: "Av. Principal 123",
                EmisorDepartamento: "Lima",
                EmisorProvincia: "Lima",
                EmisorDistrito: "Lima",
                ClienteDocTipo: "6",
                ClienteDocNumero: "20100070970", // RUC válido
                ClienteNombre: "CLIENTE SAC",
                ClienteUbigeo: null,
                ClienteDireccion: null,
                ClienteDepartamento: null,
                ClienteProvincia: null,
                ClienteDistrito: null,
                DescuentoGlobalPorcentaje: null,
                DescuentoGlobalMonto: null,
                Lineas: new List<GuardarBorradorLineaInput>() // vacío
            );

            var o = await uc.Handle(input);

            Assert.That(o.Estado, Is.EqualTo("DRAFT"));
            Assert.That(o.SubtotalBase, Is.EqualTo(0m));
            Assert.That(o.IgvTotal, Is.EqualTo(0m));
            Assert.That(o.Total, Is.EqualTo(0m));
        }
    }
}
