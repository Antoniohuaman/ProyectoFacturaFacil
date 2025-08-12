using System;
using System.Collections.Generic;
using System.Linq;
using IndicadoresNegocioBC.Domain.Aggregates;
using IndicadoresNegocioBC.Domain.ValueObjects;
using NUnit.Framework;

namespace IndicadoresNegocioBC.Tests.UnitTests.Aggregates
{
    public class IndicadorNegocioTests
    {
        private static Moneda PEN => new Moneda("PEN");
        private static Moneda USD => new Moneda("USD");

        private static SegmentoIndicador Segmento(Guid? empresaId = null)
        {
            return SegmentoIndicador.ParaEmpresa(empresaId ?? Guid.NewGuid(), PEN);
        }

        private static IndicadorNegocio CrearAgregado(
            IndicadorNegocio.TipoIndicador? tipo = null,
            Periodo? periodo = null,
            SegmentoIndicador? segmento = null)
        {
            return IndicadorNegocio.Crear(
                tipo ?? IndicadorNegocio.TipoIndicador.VentaDiaria,
                periodo ?? Periodo.PorMes(2025, 8),
                segmento ?? Segmento());
        }

        private static IndicadorNegocio.ComprobanteVenta Venta(
            Guid? id = null,
            DateOnly? fecha = null,
            Guid? clienteId = null,
            Dinero? total = null,
            Dinero? igv = null,
            IEnumerable<(string productoId, decimal cantidad, Dinero subtotal)>? items = null)
        {
            var f = fecha ?? new DateOnly(2025, 8, 15);
            var lista = (items ?? new[]
            {
                ("SKU-1", 2m, Dinero.Crear(60m, PEN)),
                ("SKU-2", 1m, Dinero.Crear(40m, PEN)),
            }).Select(x => new IndicadorNegocio.ComprobanteVenta.Item(x.productoId, x.cantidad, x.subtotal)).ToList();

            var t = total ?? Dinero.Crear(lista.Sum(i => i.Subtotal.Monto), PEN);
            var i = igv ?? Dinero.Crear(Math.Round(t.Monto * 0.18m, 2, MidpointRounding.AwayFromZero), PEN);

            return new IndicadorNegocio.ComprobanteVenta(
                id ?? Guid.NewGuid(),
                f,
                clienteId,
                t,
                i,
                lista);
        }

        // ------------------- Creación -------------------

        [Test]
        public void Crear_InicializaEnCreado_ConCerosYVersionCero()
        {
            var agg = CrearAgregado();

            Assert.Multiple(() =>
            {
                Assert.That(agg.Estado, Is.EqualTo(EstadoIndicador.Creado));
                Assert.That(agg.TotalVentas.Monto, Is.EqualTo(0m));
                Assert.That(agg.TotalComprobantes, Is.EqualTo(0));
                Assert.That(agg.Version, Is.EqualTo(0));
                Assert.That(agg.Segmento.Moneda, Is.EqualTo(PEN));
            });
        }

        // ------------------- Registrar Venta -------------------

        [Test]
        public void RegistrarVentaAceptada_AcumulaVentasDiarias_Rankings_Y_Ticket()
        {
            var agg = CrearAgregado();
            var clienteId = Guid.NewGuid();
            var venta = Venta(clienteId: clienteId);

            agg.RegistrarVentaAceptada(venta);

            // Ventas diarias
            var vd = agg.ObtenerVentasDiariasOrdenadas().Single();
            Assert.Multiple(() =>
            {
                Assert.That(vd.Fecha, Is.EqualTo(venta.Fecha));
                Assert.That(vd.TotalVentas, Is.EqualTo(venta.Total));
                Assert.That(vd.TotalIgv, Is.EqualTo(venta.Igv));
                Assert.That(vd.NroComprobantes, Is.EqualTo(1));
            });

            // Ticket
            Assert.Multiple(() =>
            {
                Assert.That(agg.TotalVentas, Is.EqualTo(venta.Total));
                Assert.That(agg.TotalComprobantes, Is.EqualTo(1));
                Assert.That(agg.TicketPromedio.Promedio, Is.EqualTo(venta.Total)); // 1 comprobante
            });

            // Ranking productos
            var rp = agg.RankingProductos.ToDictionary(x => x.ProductoId, x => x);
            Assert.Multiple(() =>
            {
                Assert.That(rp["SKU-1"].Cantidad, Is.EqualTo(2m));
                Assert.That(rp["SKU-1"].TotalVendido, Is.EqualTo(Dinero.Crear(60m, PEN)));
                Assert.That(rp["SKU-2"].Cantidad, Is.EqualTo(1m));
                Assert.That(rp["SKU-2"].TotalVendido, Is.EqualTo(Dinero.Crear(40m, PEN)));
            });

            // Ranking clientes
            var rc = agg.RankingClientes.Single();
            Assert.Multiple(() =>
            {
                Assert.That(rc.ClienteId, Is.EqualTo(clienteId));
                Assert.That(rc.Frecuencia, Is.EqualTo(1));
                Assert.That(rc.TotalComprado, Is.EqualTo(venta.Total));
            });

            // Estado/versión
            Assert.Multiple(() =>
            {
                Assert.That(agg.Estado, Is.EqualTo(EstadoIndicador.Actualizado));
                Assert.That(agg.Version, Is.EqualTo(1));
            });
        }

        [Test]
        public void RegistrarVentaAceptada_Idempotente_RepetirNoCambia()
        {
            var agg = CrearAgregado();
            var venta = Venta();

            agg.RegistrarVentaAceptada(venta);
            var v1 = agg.Version;
            agg.RegistrarVentaAceptada(venta); // repetida
            var v2 = agg.Version;

            Assert.Multiple(() =>
            {
                Assert.That(agg.TotalVentas, Is.EqualTo(venta.Total));
                Assert.That(agg.TotalComprobantes, Is.EqualTo(1));
                Assert.That(v2, Is.EqualTo(v1)); // no incrementa versión en idempotencia
            });
        }

        [Test]
        public void RegistrarVentaAceptada_MonedaDistinta_Lanza()
        {
            var agg = CrearAgregado();
            var venta = Venta(total: Dinero.Crear(100m, USD), igv: Dinero.Crear(18m, USD));

            Assert.Throws<InvalidOperationException>(() => agg.RegistrarVentaAceptada(venta));
        }

        [Test]
        public void RegistrarVentaAceptada_FechaFueraDePeriodo_Lanza()
        {
            var agg = CrearAgregado(periodo: Periodo.PorMes(2025, 8)); // 01..31 Ago
            var venta = Venta(fecha: new DateOnly(2025, 9, 1));        // fuera

            Assert.Throws<InvalidOperationException>(() => agg.RegistrarVentaAceptada(venta));
        }

        [Test]
        public void RegistrarVentaAceptada_SinCliente_NoAfectaRankingClientes()
        {
            var agg = CrearAgregado();
            var venta = Venta(clienteId: null);

            agg.RegistrarVentaAceptada(venta);

            Assert.That(agg.RankingClientes.Count, Is.EqualTo(0));
        }

        // ------------------- Anulación -------------------

        [Test]
        public void RegistrarAnulacion_RevierteTodo_Y_Idempotente()
        {
            var agg = CrearAgregado();
            var venta = Venta();
            agg.RegistrarVentaAceptada(venta);

            var versionTrasVenta = agg.Version;

            // Anula
            agg.RegistrarAnulacion(venta.ComprobanteId);

            Assert.Multiple(() =>
            {
                // Ceros
                Assert.That(agg.TotalVentas.Monto, Is.EqualTo(0m));
                Assert.That(agg.TotalComprobantes, Is.EqualTo(0));
                Assert.That(agg.ObtenerVentasDiariasOrdenadas().Count, Is.EqualTo(0));
                Assert.That(agg.RankingProductos.Count, Is.EqualTo(0));
                Assert.That(agg.RankingClientes.Count, Is.EqualTo(0));
                Assert.That(agg.Version, Is.EqualTo(versionTrasVenta + 1));
            });

            // Idempotente: anular nuevamente no cambia nada ni incrementa versión
            var vAntes = agg.Version;
            agg.RegistrarAnulacion(venta.ComprobanteId);
            Assert.That(agg.Version, Is.EqualTo(vAntes));
        }

        [Test]
        public void RegistrarAnulacion_DeComprobanteInexistente_NoHaceNada()
        {
            var agg = CrearAgregado();
            var v0 = agg.Version;
            agg.RegistrarAnulacion(Guid.NewGuid());
            Assert.That(agg.Version, Is.EqualTo(v0));
        }

        // ------------------- Consolidación -------------------

        [Test]
        public void Consolidar_BloqueaMutaciones()
        {
            var agg = CrearAgregado();
            var venta = Venta();

            agg.RegistrarVentaAceptada(venta);
            agg.ConsolidarPeriodo();

            Assert.Multiple(() =>
            {
                Assert.That(agg.Estado, Is.EqualTo(EstadoIndicador.Consolidado));
                Assert.That(agg.ConsolidadoEn.HasValue, Is.True);
            });

            // intentar mutar luego debe fallar
            var otra = Venta(id: Guid.NewGuid());
            Assert.Throws<InvalidOperationException>(() => agg.RegistrarVentaAceptada(otra));
            Assert.Throws<InvalidOperationException>(() => agg.RegistrarAnulacion(venta.ComprobanteId));
        }

        // ------------------- Consultas Top / Ordenamientos -------------------

        [Test]
        public void ObtenerTopProductos_OrdenPorMontoYPorCantidad()
        {
            var agg = CrearAgregado();

            // Venta 1: SKU-A (2 x 50), SKU-B (5 x 10)
            var v1 = Venta(items: new[]
            {
                ("A", 2m, Dinero.Crear(100m, PEN)), // monto alto, qty 2
                ("B", 5m, Dinero.Crear(50m, PEN)),  // monto menor, qty 5
            });
            agg.RegistrarVentaAceptada(v1);

            // Venta 2: SKU-B (5 x 10) adicional => qty B = 10, monto B = 100
            var v2 = Venta(id: Guid.NewGuid(), items: new[]
            {
                ("B", 5m, Dinero.Crear(50m, PEN)),
            });
            agg.RegistrarVentaAceptada(v2);

            // Venta 3: SKU-C (1 x 120) => monto C = 120, qty 1
            var v3 = Venta(id: Guid.NewGuid(), items: new[]
            {
                ("C", 1m, Dinero.Crear(120m, PEN)),
            });
            agg.RegistrarVentaAceptada(v3);

            // PorMonto: C(120) > A(100) = B(100) => luego desempata por cantidad
            var topMonto = agg.ObtenerTopProductos(LimiteTop.Top10, IndicadorNegocio.RankingCriterio.PorMonto).ToList();
            Assert.Multiple(() =>
            {
                Assert.That(topMonto[0].ProductoId, Is.EqualTo("C")); // 120
                // A y B ambos 100; desempate por cantidad => A(2) después de B(10)? (ThenByDescending Cantidad)
                Assert.That(topMonto[1].TotalVendido.Monto, Is.EqualTo(100m));
                Assert.That(topMonto[2].TotalVendido.Monto, Is.EqualTo(100m));
                Assert.That(topMonto.Any(x => x.ProductoId == "A"), Is.True);
                Assert.That(topMonto.Any(x => x.ProductoId == "B"), Is.True);
            });

            // PorCantidad: B(10) > A(2) > C(1)
            var topCantidad = agg.ObtenerTopProductos(LimiteTop.Top10, IndicadorNegocio.RankingCriterio.PorCantidad).ToList();
            Assert.Multiple(() =>
            {
                Assert.That(topCantidad[0].ProductoId, Is.EqualTo("B"));
                Assert.That(topCantidad[1].ProductoId, Is.EqualTo("A"));
                Assert.That(topCantidad[2].ProductoId, Is.EqualTo("C"));
            });
        }

        [Test]
        public void ObtenerTopClientes_OrdenPorMontoYFrecuencia()
        {
            var agg = CrearAgregado();

            var clienteX = Guid.NewGuid();
            var clienteY = Guid.NewGuid();

            // X compra 2 veces total 150
            agg.RegistrarVentaAceptada(Venta(clienteId: clienteX, items: new[]
            {
                ("A", 1m, Dinero.Crear(50m, PEN))
            }));
            agg.RegistrarVentaAceptada(Venta(id: Guid.NewGuid(), clienteId: clienteX, items: new[]
            {
                ("B", 1m, Dinero.Crear(100m, PEN))
            }));

            // Y compra 1 vez total 120
            agg.RegistrarVentaAceptada(Venta(id: Guid.NewGuid(), clienteId: clienteY, items: new[]
            {
                ("C", 1m, Dinero.Crear(120m, PEN))
            }));

            var top = agg.ObtenerTopClientes(LimiteTop.Top10);

            // Orden: primero por monto, luego por frecuencia
            Assert.Multiple(() =>
            {
                Assert.That(top[0].ClienteId, Is.EqualTo(clienteX)); // 150 (freq 2)
                Assert.That(top[1].ClienteId, Is.EqualTo(clienteY)); // 120 (freq 1)
                Assert.That(top[0].TotalComprado.Monto, Is.EqualTo(150m));
                Assert.That(top[0].Frecuencia, Is.EqualTo(2));
            });
        }

        [Test]
        public void ObtenerVentasDiariasOrdenadas_DevuelveAscendente()
        {
            var agg = CrearAgregado();

            var v1 = Venta(id: Guid.NewGuid(), fecha: new DateOnly(2025, 8, 10));
            var v2 = Venta(id: Guid.NewGuid(), fecha: new DateOnly(2025, 8, 12));
            var v3 = Venta(id: Guid.NewGuid(), fecha: new DateOnly(2025, 8, 11));

            agg.RegistrarVentaAceptada(v1);
            agg.RegistrarVentaAceptada(v2);
            agg.RegistrarVentaAceptada(v3);

            var dias = agg.ObtenerVentasDiariasOrdenadas().Select(x => x.Fecha).ToList();

            Assert.That(dias, Is.EqualTo(new[]
            {
                new DateOnly(2025, 8, 10),
                new DateOnly(2025, 8, 11),
                new DateOnly(2025, 8, 12),
            }));
        }

        // ------------------- Versiones -------------------

        [Test]
        public void Version_SeIncrementaPorMutacion_NoPorIdempotencia()
        {
            var agg = CrearAgregado();
            var venta = Venta();

            var v0 = agg.Version;
            agg.RegistrarVentaAceptada(venta);
            var v1 = agg.Version;
            agg.RegistrarVentaAceptada(venta); // idempotente
            var v2 = agg.Version;
            agg.RegistrarAnulacion(venta.ComprobanteId);
            var v3 = agg.Version;

            Assert.Multiple(() =>
            {
                Assert.That(v1, Is.EqualTo(v0 + 1));
                Assert.That(v2, Is.EqualTo(v1));     // no cambia
                Assert.That(v3, Is.EqualTo(v2 + 1)); // anulación incrementa
            });
        }
    }
}