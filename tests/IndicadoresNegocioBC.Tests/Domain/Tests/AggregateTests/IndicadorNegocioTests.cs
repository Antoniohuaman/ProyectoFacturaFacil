using System;
using System.Collections.Generic;
using NUnit.Framework;
using IndicadoresNegocioBC.Domain.Aggregates;
using IndicadoresNegocioBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace IndicadoresNegocioBC.Tests.Domain
{
    [TestFixture]
    public class IndicadorNegocioTests
    {
        // ================= Builders: AJUSTA a tus factories reales =================

    private static Moneda PEN() => SharedKernel.ValueObjects.Moneda.PEN();
    private static Moneda USD() => SharedKernel.ValueObjects.Moneda.USD();
    private static Dinero Money(decimal m) => new Dinero(m, PEN());
        private static Dinero IgvDe(Dinero baseImponible, decimal tasa) => Money(Math.Round(baseImponible.Monto * tasa, 2));

        private static Periodo PeriodoMensualActual()
        {
            var hoy = IndicadoresNegocioBC.Tests.TestUtils.TestTime.BaseUtc();
            return Periodo.PorMes(hoy.Year, hoy.Month);
        }

        private static SegmentoIndicador SegmentoSoles()
        {
            // Usamos empresaId dummy para test, puedes ajustar si tienes un valor fijo
            var empresaId = EmpresaId.From(Guid.NewGuid().ToString());
            return SegmentoIndicador.ParaEmpresa(empresaId, PEN());
        }

        private static IndicadorNegocio NuevoIndicador()
            => IndicadorNegocio.Crear(IndicadorNegocio.TipoIndicador.VentaDiaria, PeriodoMensualActual(), SegmentoSoles());

    private static EstablecimientoId Estab() => EstablecimientoId.From(Guid.NewGuid());
    private static UsuarioId Vendedor() => UsuarioId.From(Guid.NewGuid());

        private static IndicadorNegocio.ComprobanteVenta Venta(
            Guid? id = null,
            DateOnly? fecha = null,
            string tipo = "factura",
            decimal subtotal = 100m,
            decimal tasaIgv = 0.18m,
            Guid? clienteId = null,
            bool conVendedor = true)
        {
            var idVenta = id ?? Guid.NewGuid();
            var baseUtc = IndicadoresNegocioBC.Tests.TestUtils.TestTime.BaseUtc();
            var f = fecha ?? new DateOnly(baseUtc.Year, baseUtc.Month, Math.Min(15, baseUtc.Day));
            var sub = Money(subtotal);
            var igv = IgvDe(sub, tasaIgv);
            var total = Money(sub.Monto + igv.Monto);

            var items = new List<IndicadorNegocio.ComprobanteVenta.Item>
            {
                new IndicadorNegocio.ComprobanteVenta.Item("P001", 1m, sub)
            };

            return new IndicadorNegocio.ComprobanteVenta(
                comprobanteId: idVenta,
                fecha: f,
                clienteId: clienteId,
                total: total,
                igv: igv,
                items: items,
                vendedorId: conVendedor ? Vendedor() : null,
                tipoComprobante: tipo,
                establecimientoId: Estab()
            );
        }

        // ============================== TESTS ==============================

        [Test]
        public void RegistrarVentaAceptada_NormalizaTipoComprobante_Y_Idempotencia()
        {
            var agg = NuevoIndicador();

            var v1 = Venta(tipo: "  factura  "); // minúsculas + espacios
            agg.RegistrarVentaAceptada(v1);
            agg.RegistrarVentaAceptada(v1); // idempotente

            // Debe contar 1 tanto si consulto con "FACTURA" como con " factura "
            Assert.That(agg.ObtenerCantidadPorTipoComprobante("FACTURA"), Is.EqualTo(1));
            Assert.That(agg.ObtenerCantidadPorTipoComprobante(" factura "), Is.EqualTo(1));
        }

        [Test]
        public void ObtenerCantidadPorTipoComprobante_AceptaSoloDesde_O_SoloHasta()
        {
            var agg = NuevoIndicador();

            var hoy = DateOnly.FromDateTime(IndicadoresNegocioBC.Tests.TestUtils.TestTime.BaseUtc());
            var ayer = hoy.AddDays(-1);
            var manana = hoy.AddDays(1);

            var vAyer = Venta(id: Guid.NewGuid(), fecha: ayer, tipo: "Boleta");
            var vHoy  = Venta(id: Guid.NewGuid(), fecha: hoy,  tipo: "Boleta");
            var vMan  = Venta(id: Guid.NewGuid(), fecha: manana, tipo: "Boleta");

            agg.RegistrarVentaAceptada(vAyer);
            agg.RegistrarVentaAceptada(vHoy);
            agg.RegistrarVentaAceptada(vMan);

            // solo desde hoy => cuenta hoy y mañana
            Assert.That(agg.ObtenerCantidadPorTipoComprobante("boleta", desde: hoy), Is.EqualTo(2));

            // solo hasta hoy => cuenta ayer y hoy
            Assert.That(agg.ObtenerCantidadPorTipoComprobante("Boleta", hasta: hoy), Is.EqualTo(2));

            // rango completo => los 3
            Assert.That(agg.ObtenerCantidadPorTipoComprobante("BOLETA", desde: ayer, hasta: manana), Is.EqualTo(3));
        }

        [Test]
        public void RegistrarVentaAceptada_ValidaMonedaEnTotalIgvEItems()
        {
            var agg = NuevoIndicador();

            // Construimos una venta válida y luego le cambiamos el IGV a otra moneda para probar el fallo.
            var v = Venta();
            var otraMoneda = USD();
            var igvUsd = new Dinero(v.Igv.Monto, otraMoneda);

            var items = new List<IndicadorNegocio.ComprobanteVenta.Item>
            {
                // Subtotal en moneda OK
                new IndicadorNegocio.ComprobanteVenta.Item("P001", 1m, v.Items[0].Subtotal)
            };

            var ventaMonedaInvalida = new IndicadorNegocio.ComprobanteVenta(
                comprobanteId: Guid.NewGuid(),
                fecha: v.Fecha,
                clienteId: v.ClienteId,
                total: v.Total,
                igv: igvUsd, // IGV en otra moneda -> debe fallar
                items: items,
                vendedorId: v.VendedorId,
                tipoComprobante: v.TipoComprobante,
                establecimientoId: v.EstablecimientoId
            );

            Assert.That(() => agg.RegistrarVentaAceptada(ventaMonedaInvalida),
                Throws.InvalidOperationException.With.Message.Contains("Moneda distinta a la del segmento"));
        }

        [Test]
        public void RegistrarAnulacion_EsIdempotente_Y_ReversaAcumulados()
        {
            var agg = NuevoIndicador();
            var v = Venta(tipo: "Factura", subtotal: 200m);

            agg.RegistrarVentaAceptada(v);

            var totalAntes = agg.TotalVentas;
            Assert.That(totalAntes.Monto, Is.GreaterThan(0m));

            // Anulo
            agg.RegistrarAnulacion(v.ComprobanteId);
            var totalDespues = agg.TotalVentas;

            Assert.That(totalDespues.Monto, Is.LessThan(totalAntes.Monto));

            // Anular de nuevo no debe cambiar nada
            agg.RegistrarAnulacion(v.ComprobanteId);
            Assert.That(agg.TotalVentas.Monto, Is.EqualTo(totalDespues.Monto));
        }
    }
}
