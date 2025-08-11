using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

using ComprobantesElectronicosBC.Application.DTOs;
using ComprobantesElectronicosBC.Application.UseCases;
using ComprobantesElectronicosBC.Domain.Aggregates;
using ComprobantesElectronicosBC.Domain.Repositories;

namespace ComprobantesElectronicosBC.Tests.UnitTests.UseCases
{
    [TestFixture]
    public class EmitirComprobanteUseCaseTests
    {
        // -------------------- Helpers --------------------
        private static EmitirComprobanteLineaInput L(
            string nombre, decimal qty, decimal precio, bool incluyeIgv,
            string afectacion = "10", decimal? igvRate = 0.18m,
            decimal? descPct = null, decimal? descMonto = null) =>
            new EmitirComprobanteLineaInput(
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

        // -------------------- Tests --------------------
        [Test]
        public async Task Emitir_Boleta_Contado_OK()
        {
            var repo = new InMemoryComprobanteRepository();
            var uow  = new InMemoryUow();
            var uc   = new EmitirComprobanteUseCase(repo, uow);

            var input = new EmitirComprobanteInput(
                TipoCodigo: "03",
                MonedaCodigo: "PEN",
                FechaEmision: DateOnly.FromDateTime(DateTime.Now),
                FormaPagoCodigo: "10",
                MetodoPagoCodigo: "EFECTIVO",
                MetodoPagoNombre: null,
                DiasCredito: null,
                Serie: "B001",
                Numero: 1,
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
                Lineas: new List<EmitirComprobanteLineaInput> { L("Producto A", 1m, 100m, incluyeIgv:false) }
            );

            var o = await uc.Handle(input);

            Assert.That(o.TipoCodigo, Is.EqualTo("03"));
            Assert.That(o.Serie, Is.EqualTo("B001"));
            Assert.That(o.Numero, Is.EqualTo(1));
            Assert.That(o.Estado, Is.EqualTo("SENT"));
            Assert.That(o.Total, Is.GreaterThan(0m));
        }

        [Test]
        public async Task Emitir_Factura_Credito_ConDescuentoGlobalPct_OK()
        {
            var repo = new InMemoryComprobanteRepository();
            var uow  = new InMemoryUow();
            var uc   = new EmitirComprobanteUseCase(repo, uow);

            var input = new EmitirComprobanteInput(
                TipoCodigo: "01",
                MonedaCodigo: "PEN",
                FechaEmision: DateOnly.FromDateTime(DateTime.Now),
                FormaPagoCodigo: "20",
                MetodoPagoCodigo: null,
                MetodoPagoNombre: null,
                DiasCredito: 15,
                Serie: "F001",
                Numero: 25,
                EmisorRuc: "20123456789",
                EmisorRazonSocial: "MI EMPRESA SAC",
                EmisorUbigeo: "150101",
                EmisorDireccion: "Av. Principal 123",
                EmisorDepartamento: "Lima",
                EmisorProvincia: "Lima",
                EmisorDistrito: "Lima",
                ClienteDocTipo: "6",
                ClienteDocNumero: "20100070970", // RUC válido SUNAT
                ClienteNombre: "CLIENTE SAC",
                ClienteUbigeo: "150101",
                ClienteDireccion: "Jr. Comercio 456",
                ClienteDepartamento: "Lima",
                ClienteProvincia: "Lima",
                ClienteDistrito: "Lima",
                DescuentoGlobalPorcentaje: 5m,
                DescuentoGlobalMonto: null,
                Lineas: new List<EmitirComprobanteLineaInput> { L("Servicio X", 2m, 50m, incluyeIgv:true) }
            );

            var o = await uc.Handle(input);

            Assert.That(o.TipoCodigo, Is.EqualTo("01"));
            Assert.That(o.Serie, Is.EqualTo("F001"));
            Assert.That(o.Numero, Is.EqualTo(25));
            Assert.That(o.Estado, Is.EqualTo("SENT"));
            Assert.That(o.DescuentoGlobal, Is.GreaterThanOrEqualTo(0m));
            Assert.That(o.Total, Is.GreaterThan(0m));
        }

        [Test]
        public void Emitir_SinLineas_DebeFallar()
        {
            var repo = new InMemoryComprobanteRepository();
            var uow  = new InMemoryUow();
            var uc   = new EmitirComprobanteUseCase(repo, uow);

            var input = new EmitirComprobanteInput(
                TipoCodigo: "03",
                MonedaCodigo: "PEN",
                FechaEmision: DateOnly.FromDateTime(DateTime.Now),
                FormaPagoCodigo: "10",
                MetodoPagoCodigo: "EFECTIVO",
                MetodoPagoNombre: null,
                DiasCredito: null,
                Serie: "B001",
                Numero: 2,
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
                Lineas: new List<EmitirComprobanteLineaInput>() // <-- vacío
            );

            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await uc.Handle(input));
            Assert.That(ex!.Message, Does.Contain("al menos una línea"));
        }
    }

    // ==========================================================
    // ==============  DOBLES IN-MEMORY PARA TESTS  =============
    // ==========================================================
    internal sealed class InMemoryComprobanteRepository : IComprobanteRepository
    {
        private readonly ConcurrentDictionary<Guid, ComprobanteElectronico> _db = new();

        public Task<ComprobanteElectronico?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(_db.TryGetValue(id, out var agg) ? agg : null);

        public Task<ComprobanteElectronico?> GetBySerieNumeroAsync(string serie, int numero, CancellationToken ct = default)
            => Task.FromResult(_db.Values.FirstOrDefault(x =>
                x.SerieNumero is not null &&
                x.SerieNumero.Serie == serie &&
                x.SerieNumero.Numero == numero));

        public Task<bool> ExistsSerieNumeroAsync(string serie, int numero, CancellationToken ct = default)
            => Task.FromResult(_db.Values.Any(x =>
                x.SerieNumero is not null &&
                x.SerieNumero.Serie == serie &&
                x.SerieNumero.Numero == numero));

        public Task AddAsync(ComprobanteElectronico aggregate, CancellationToken ct = default)
        { _db[aggregate.ComprobanteId] = aggregate; return Task.CompletedTask; }

        public Task UpdateAsync(ComprobanteElectronico aggregate, CancellationToken ct = default)
        { _db[aggregate.ComprobanteId] = aggregate; return Task.CompletedTask; }

        public Task RemoveAsync(Guid id, CancellationToken ct = default)
        { _db.TryRemove(id, out _); return Task.CompletedTask; }
    }

    internal sealed class InMemoryUow : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(1);
        public Task<ITransaction> BeginTransactionAsync(CancellationToken ct = default)
            => Task.FromResult<ITransaction>(new NoopTx());

        private sealed class NoopTx : ITransaction
        {
            public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;
            public Task RollbackAsync(CancellationToken ct = default) => Task.CompletedTask;
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}