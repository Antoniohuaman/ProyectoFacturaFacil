using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IndicadoresNegocioBC.Application.Contracts.Inbound;
using IndicadoresNegocioBC.Application.UseCases;
using IndicadoresNegocioBC.Domain.Aggregates;
using IndicadoresNegocioBC.Domain.Repositories;
using IndicadoresNegocioBC.Domain.ValueObjects;
using Moq;
using NUnit.Framework;

namespace IndicadoresNegocioBC.Tests.UnitTests.UseCases
{
    public class RegistrarAnulacionComprobanteUseCaseTests
    {
        [Test]
        public async Task Anulacion_ReviertenTotalesYRealizaUpdateEnLos16Agregados()
        {
            // ---------- Arrange ----------
            var repoMock = new Mock<IIndicadorNegocioRepository>();
            var store = new Dictionary<string, IndicadorNegocio>();
            int addCount = 0, updateCount = 0;

            repoMock
                .Setup(m => m.GetByClaveAsync(
                    It.IsAny<IndicadorNegocio.TipoIndicador>(),
                    It.IsAny<Periodo>(),
                    It.IsAny<SegmentoIndicador>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((IndicadorNegocio.TipoIndicador t, Periodo p, SegmentoIndicador s, CancellationToken _) =>
                {
                    store.TryGetValue(KeyOf(t, p, s), out var agg);
                    return agg;
                });

            repoMock
                .Setup(m => m.AddAsync(It.IsAny<IndicadorNegocio>(), It.IsAny<CancellationToken>()))
                .Callback((IndicadorNegocio agg, CancellationToken _) =>
                {
                    store[KeyOf(agg.Tipo, agg.Periodo, agg.Segmento)] = agg;
                    addCount++;
                })
                .Returns(Task.CompletedTask);

            repoMock
                .Setup(m => m.UpdateAsync(It.IsAny<IndicadorNegocio>(), It.IsAny<CancellationToken>()))
                .Callback((IndicadorNegocio _, CancellationToken __) => updateCount++)
                .Returns(Task.CompletedTask);

            // Seed: primero registramos la venta (caso de uso de alta)
            var registrarVenta = new RegistrarVentaAceptadaUseCase(repoMock.Object);
            var (evtVenta, fechaVenta, moneda) = CrearEventoVentaDemo();
            await registrarVenta.ExecuteAsync(evtVenta);

            // Sanity: 16 agregados creados (4 granularidades x 4 tipos)
            Assert.That(addCount, Is.EqualTo(16));

            // Armamos evento de anulación (día siguiente para cubrir candidatos)
            var evtAnul = new ComprobanteAnulado(
                ComprobanteId: evtVenta.ComprobanteId,
                FechaAnulacionUtc: new DateTimeOffset(
                    new DateTime(fechaVenta.Year, fechaVenta.Month, fechaVenta.Day, 9, 0, 0, DateTimeKind.Utc)
                ).AddDays(1),
                EmpresaId: evtVenta.EmpresaId,
                EstablecimientoId: evtVenta.EstablecimientoId,
                Moneda: moneda.Codigo,
                Motivo: "Corrección"
            );

            var useCase = new RegistrarAnulacionComprobanteUseCase(repoMock.Object);

            // ---------- Act ----------
            await useCase.ExecuteAsync(evtAnul);

            // ---------- Assert ----------
            // Debe actualizar los 16 agregados existentes
            Assert.That(updateCount, Is.EqualTo(16));

            // En ventas diarias del día original, totales a cero e idempotencia de conteo
            var segmento = evtVenta.EstablecimientoId.HasValue
                ? SegmentoIndicador.ParaEstablecimiento(
                    Establecimiento.Crear(evtVenta.EmpresaId, evtVenta.EstablecimientoId.Value),
                    moneda)
                : SegmentoIndicador.ParaEmpresa(evtVenta.EmpresaId, moneda);

            var periodoDia = Periodo.PorDia(fechaVenta);
            var aggDia = store[KeyOf(IndicadorNegocio.TipoIndicador.VentaDiaria, periodoDia, segmento)];

            Assert.Multiple(() =>
            {
                Assert.That(aggDia.TotalComprobantes, Is.EqualTo(0));
                Assert.That(aggDia.TotalVentas.Monto, Is.EqualTo(0m));
                Assert.That(aggDia.TicketPromedio.Promedio.Monto, Is.EqualTo(0m));
                var ventas = aggDia.ObtenerVentasDiariasOrdenadas();
                // Puede no haber ventas tras la anulación, o puede quedar una fila con montos en cero
                if (ventas.Count > 0)
                {
                    Assert.That(ventas.Count, Is.EqualTo(1));
                    Assert.That(ventas[0].TotalVentas.Monto, Is.EqualTo(0m));
                    Assert.That(ventas[0].TotalIgv.Monto, Is.EqualTo(0m));
                }
                // Si ventas.Count == 0, comportamiento aceptado, no se hacen más aserciones
            });

            // En ranking de clientes del año: si existía una entrada, debe revertirse a 0 o eliminarse
            var periodoAnio = Periodo.PorAnio(fechaVenta.Year);
            var aggCli = store[KeyOf(IndicadorNegocio.TipoIndicador.RankingClientes, periodoAnio, segmento)];

            if (evtVenta.ClienteId.HasValue && aggCli.RankingClientes.Count > 0)
            {
                // dependiendo de la implementación, podría quedar en 0 o eliminarse
                var existe = aggCli.RankingClientes.Any(c => c.ClienteId == evtVenta.ClienteId.Value);
                if (existe)
                {
                    var entrada = aggCli.RankingClientes.First(c => c.ClienteId == evtVenta.ClienteId.Value);
                    Assert.That(entrada.Frecuencia, Is.EqualTo(0));
                    Assert.That(entrada.TotalComprado.Monto, Is.EqualTo(0m));
                }
                else
                {
                    Assert.That(aggCli.RankingClientes.Count, Is.EqualTo(0));
                }
            }
        }

        [Test]
        public async Task Anulacion_Repetida_EsIdempotente_NoModificaTotales()
        {
            // ---------- Arrange ----------
            var repoMock = new Mock<IIndicadorNegocioRepository>();
            var store = new Dictionary<string, IndicadorNegocio>();
            int addCount = 0, updateCount = 0;

            repoMock
                .Setup(m => m.GetByClaveAsync(
                    It.IsAny<IndicadorNegocio.TipoIndicador>(),
                    It.IsAny<Periodo>(),
                    It.IsAny<SegmentoIndicador>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((IndicadorNegocio.TipoIndicador t, Periodo p, SegmentoIndicador s, CancellationToken _) =>
                {
                    store.TryGetValue(KeyOf(t, p, s), out var agg);
                    return agg;
                });

            repoMock
                .Setup(m => m.AddAsync(It.IsAny<IndicadorNegocio>(), It.IsAny<CancellationToken>()))
                .Callback((IndicadorNegocio agg, CancellationToken _) =>
                {
                    store[KeyOf(agg.Tipo, agg.Periodo, agg.Segmento)] = agg;
                    addCount++;
                })
                .Returns(Task.CompletedTask);

            repoMock
                .Setup(m => m.UpdateAsync(It.IsAny<IndicadorNegocio>(), It.IsAny<CancellationToken>()))
                .Callback((IndicadorNegocio _, CancellationToken __) => updateCount++)
                .Returns(Task.CompletedTask);

            // Seed con venta aplicada
            var registrarVenta = new RegistrarVentaAceptadaUseCase(repoMock.Object);
            var (evtVenta, fechaVenta, moneda) = CrearEventoVentaDemo();
            await registrarVenta.ExecuteAsync(evtVenta);

            var segmento = evtVenta.EstablecimientoId.HasValue
                ? SegmentoIndicador.ParaEstablecimiento(
                    Establecimiento.Crear(evtVenta.EmpresaId, evtVenta.EstablecimientoId.Value),
                    moneda)
                : SegmentoIndicador.ParaEmpresa(evtVenta.EmpresaId, moneda);

            var periodoDia = Periodo.PorDia(fechaVenta);
            var claveDia = KeyOf(IndicadorNegocio.TipoIndicador.VentaDiaria, periodoDia, segmento);

            // Primera anulación
            var evtAnul = new ComprobanteAnulado(
                evtVenta.ComprobanteId,
                new DateTimeOffset(new DateTime(fechaVenta.Year, fechaVenta.Month, fechaVenta.Day, 9, 0, 0, DateTimeKind.Utc)).AddDays(1),
                evtVenta.EmpresaId,
                evtVenta.EstablecimientoId,
                moneda.Codigo,
                "Duplicado"
            );
            var useCase = new RegistrarAnulacionComprobanteUseCase(repoMock.Object);
            await useCase.ExecuteAsync(evtAnul);

            // Foto tras primera anulación
            var totalDespues = store[claveDia].TotalVentas.Monto;
            var cantDespues = store[claveDia].TotalComprobantes;

            // Segunda anulación (mismo comprobante) - idempotente
            await useCase.ExecuteAsync(evtAnul);

            // ---------- Assert ----------
            Assert.Multiple(() =>
            {
                Assert.That(addCount, Is.EqualTo(16));         // creados por la venta
                Assert.That(updateCount, Is.EqualTo(32));      // 16 updates por cada anulación
                Assert.That(store[claveDia].TotalVentas.Monto, Is.EqualTo(totalDespues));
                Assert.That(store[claveDia].TotalComprobantes, Is.EqualTo(cantDespues));
            });
        }

        // ---------- Helpers ----------

        private static (ComprobanteEmitidoAceptado evt, DateOnly fecha, Moneda moneda) CrearEventoVentaDemo()
        {
            var empresaId = Guid.NewGuid();
            var establecimientoId = Guid.NewGuid();
            var moneda = new Moneda("PEN");
            var fecha = new DateOnly(2025, 7, 15);

            var items = new List<ComprobanteEmitidoAceptadoItem>
            {
                new("SKU-001", 2, 200m),
                new("SKU-002", 1, 300m)
            };

            var evt = new ComprobanteEmitidoAceptado(
                ComprobanteId: Guid.NewGuid(),
                FechaEmisionUtc: new DateTimeOffset(new DateTime(fecha.Year, fecha.Month, fecha.Day, 10, 23, 0, DateTimeKind.Utc)),
                EmpresaId: empresaId,
                EstablecimientoId: establecimientoId,
                Moneda: moneda.Codigo,
                ClienteId: Guid.NewGuid(),
                Total: 500m,
                Igv: 90m,
                Items: items
            );

            return (evt, fecha, moneda);
        }

        private static string KeyOf(IndicadorNegocio.TipoIndicador tipo, Periodo periodo, SegmentoIndicador seg)
        {
            var est = seg.Establecimiento?.EstablecimientoId.ToString("N") ?? "ALL";
            return $"{tipo.Codigo}:{periodo.Inicio:yyyyMMdd}-{periodo.FinInclusive:yyyyMMdd}:{seg.EmpresaId:N}:{est}:{seg.Moneda.Codigo}";
        }
    }
}