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
    public class RegistrarVentaAceptadaUseCaseTests
    {
        [Test]
        public async Task PrimeraEjecucion_Crea16AgregadosYAplicaVenta()
        {
            // Arrange
            var repoMock = new Mock<IIndicadorNegocioRepository>();

            var store = new Dictionary<string, IndicadorNegocio>();
            var agregadosAgregados = new List<IndicadorNegocio>();
            var agregadosActualizados = new List<IndicadorNegocio>();

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
                    agregadosAgregados.Add(agg);
                })
                .Returns(Task.CompletedTask);

            repoMock
                .Setup(m => m.UpdateAsync(It.IsAny<IndicadorNegocio>(), It.IsAny<CancellationToken>()))
                .Callback((IndicadorNegocio agg, CancellationToken _) => agregadosActualizados.Add(agg))
                .Returns(Task.CompletedTask);

            var useCase = new RegistrarVentaAceptadaUseCase(repoMock.Object);

            var (evento, fecha, moneda) = CrearEventoDemo();

            // Act
            await useCase.ExecuteAsync(evento);

            // Assert
            // 4 granularidades x 4 tipos = 16 agregados creados
            Assert.That(agregadosAgregados.Count, Is.EqualTo(16));
            Assert.That(agregadosActualizados.Count, Is.EqualTo(0));

            // Tomamos algunos agregados para validar estado
            var seg = evento.EstablecimientoId.HasValue
                ? SegmentoIndicador.ParaEstablecimiento(
                    Establecimiento.Crear(evento.EmpresaId, evento.EstablecimientoId.Value),
                    new Moneda(evento.Moneda))
                : SegmentoIndicador.ParaEmpresa(evento.EmpresaId, new Moneda(evento.Moneda));

            var periodoDia = Periodo.PorDia(fecha);
            var aggVentasDia = store[KeyOf(IndicadorNegocio.TipoIndicador.VentaDiaria, periodoDia, seg)];
            Assert.Multiple(() =>
            {
                // Totales
                Assert.That(aggVentasDia.TotalVentas.Monto, Is.EqualTo(evento.Total));
                Assert.That(aggVentasDia.TotalComprobantes, Is.EqualTo(1));
                // Debe existir la venta del día
                var ventas = aggVentasDia.ObtenerVentasDiariasOrdenadas();
                Assert.That(ventas.Count, Is.EqualTo(1));
                Assert.That(ventas[0].Fecha, Is.EqualTo(fecha));
                Assert.That(ventas[0].TotalVentas.Monto, Is.EqualTo(evento.Total));
                Assert.That(ventas[0].TotalIgv.Monto, Is.EqualTo(evento.Igv));
                // Ticket promedio = total / 1
                Assert.That(aggVentasDia.TicketPromedio.Promedio.Monto, Is.EqualTo(evento.Total));
                // Moneda
                Assert.That(aggVentasDia.TotalVentas.Moneda.Codigo, Is.EqualTo(moneda.Codigo));
            });

            // Ranking de productos: suma cantidades y subtotales de los items
            var periodoMes = Periodo.PorMes(fecha.Year, fecha.Month);
            var aggRankingProd = store[KeyOf(IndicadorNegocio.TipoIndicador.RankingProductos, periodoMes, seg)];
            var itemsPorProducto = evento.Items.GroupBy(i => i.ProductoId)
                                               .ToDictionary(g => g.Key,
                                                             g => new
                                                             {
                                                                 Cant = g.Sum(x => x.Cantidad),
                                                                 Sub = g.Sum(x => x.Subtotal)
                                                             });

            foreach (var entrada in aggRankingProd.RankingProductos)
            {
                Assert.That(itemsPorProducto.ContainsKey(entrada.ProductoId), Is.True);
                var esperado = itemsPorProducto[entrada.ProductoId];
                Assert.That(entrada.Cantidad, Is.EqualTo(esperado.Cant));
                Assert.That(entrada.TotalVendido.Monto, Is.EqualTo(esperado.Sub));
            }

            // Ranking de clientes: si hay ClienteId, debe haber una entrada con total y frecuencia 1
            var periodoAnio = Periodo.PorAnio(fecha.Year);
            var aggRankingCli = store[KeyOf(IndicadorNegocio.TipoIndicador.RankingClientes, periodoAnio, seg)];
            if (evento.ClienteId.HasValue)
            {
                var entradaCli = aggRankingCli.RankingClientes.Single();
                Assert.Multiple(() =>
                {
                    Assert.That(entradaCli.ClienteId, Is.EqualTo(evento.ClienteId.Value));
                    Assert.That(entradaCli.Frecuencia, Is.EqualTo(1));
                    Assert.That(entradaCli.TotalComprado.Monto, Is.EqualTo(evento.Total));
                });
            }
        }

        [Test]
        public async Task SegundaEjecucion_MismoComprobante_NoDuplicaYRealizaUpdate()
        {
            // Arrange
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

            var useCase = new RegistrarVentaAceptadaUseCase(repoMock.Object);
            var (evento, fecha, _) = CrearEventoDemo();

            // Act 1: primera ejecución -> crea y agrega
            await useCase.ExecuteAsync(evento);

            // Guardamos una foto de un agregado para comparar
            var seg = evento.EstablecimientoId.HasValue
                ? SegmentoIndicador.ParaEstablecimiento(
                    Establecimiento.Crear(evento.EmpresaId, evento.EstablecimientoId.Value),
                    new Moneda(evento.Moneda))
                : SegmentoIndicador.ParaEmpresa(evento.EmpresaId, new Moneda(evento.Moneda));

            var periodoDia = Periodo.PorDia(fecha);
            var clave = KeyOf(IndicadorNegocio.TipoIndicador.VentaDiaria, periodoDia, seg);
            var totalVentasAntes = store[clave].TotalVentas.Monto;
            var nroCompAntes = store[clave].TotalComprobantes;

            // Act 2: misma venta (mismo ComprobanteId) -> idempotente, debe hacer Update pero sin cambiar totales
            await useCase.ExecuteAsync(evento);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(addCount, Is.EqualTo(16));     // 4 granularidades x 4 tipos
                Assert.That(updateCount, Is.EqualTo(16));  // segunda pasada hace Update en los 16
                Assert.That(store[clave].TotalVentas.Monto, Is.EqualTo(totalVentasAntes)); // no duplicó
                Assert.That(store[clave].TotalComprobantes, Is.EqualTo(nroCompAntes));     // no incrementó
            });
        }

        // ---------- Helpers ----------

        private static (ComprobanteEmitidoAceptado evt, DateOnly fecha, Moneda moneda) CrearEventoDemo()
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