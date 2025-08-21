#nullable enable
using System;
using NUnit.Framework;
using ListaPreciosBC.Domain.Aggregates;
using ListaPreciosBC.Domain.Services;
using ListaPreciosBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace ListaPreciosBC.Tests.UnitTests.Services
{
    [TestFixture]
    public class ResolverPrecioServiceTests
    {
        private static Sku SKU(string v) => Sku.Crear(v);
        private static IdentificadorColumnaPrecio P(byte n) => IdentificadorColumnaPrecio.DesdeNumero(n);
        private static PeriodoVigencia Vig(DateTimeOffset desde, DateTimeOffset? hasta = null) => PeriodoVigencia.Crear(desde, hasta);
        private static ValorPrecio VP(decimal monto) => ValorPrecio.DesdeMonto(monto); // usa Moneda.PEN por default (adaptador que agregaste)
        private static MatrizVolumen Mat(params (int desde, int? hasta, decimal precio)[] tramos)
        {
            var list = new TramoVolumen[tramos.Length];
            for (int i = 0; i < tramos.Length; i++)
            {
                var t = tramos[i];
                list[i] = TramoVolumen.Crear(t.desde, t.hasta, VP(t.precio));
            }
            return MatrizVolumen.Crear(list);
        }

        [Test]
        public void Si_NoReciboColumna_UsaBase_P1_y_Resuelve_Fijo()
        {
            var svc = new ResolverPrecioService();
            var prod = PrecioProducto.CrearNuevo(SKU("CAP-100"));
            var hoy  = new DateTimeOffset(2025, 1, 10, 10, 0, 0, TimeSpan.Zero);

            // Precio fijo en P1 vigente
            prod.UpsertPrecioFijo(P(1), VP(10m), Vig(hoy.AddDays(-1), null));

            // Afectación null => no grava, neto = bruto = 10
            var dto = svc.Resolver(prod, columnaSolicitada: null, cantidad: 3, fecha: hoy,
                                   afectacion: null, tasaImpuestoFraccion: 0.18m,
                                   plantillaOpcional: null, // fallback P1
                                   mostrarConImpuestos: true);

            Assert.That(dto, Is.Not.Null);
            Assert.That(dto!.ColumnaAplicada.Numero, Is.EqualTo(1));
            Assert.That(dto.ValorOriginal.Monto, Is.EqualTo(10m));
            Assert.That(dto.Bruto.Monto, Is.EqualTo(10m));
            Assert.That(dto.Neto.Monto,  Is.EqualTo(10m));
            Assert.That(dto.Mostrar.Monto, Is.EqualTo(10m));
            Assert.That(dto.Origen, Is.EqualTo(PrecioResueltoOrigen.Fijo));
            Assert.That(dto.TramoDesde, Is.Null);
            Assert.That(dto.TramoHasta, Is.Null);
        }

        [Test]
        public void Si_ColumnaNoTieneValor_Fallback_a_Base()
        {
            var svc = new ResolverPrecioService();
            var prod = PrecioProducto.CrearNuevo(SKU("CAP-101"));
            var hoy  = new DateTimeOffset(2025, 1, 11, 9, 0, 0, TimeSpan.Zero);

            // Configuro P1 fijo; P2 sin valor
            prod.UpsertPrecioFijo(P(1), VP(20m), Vig(hoy.AddDays(-2), hoy.AddDays(2)));

            var dto = svc.Resolver(prod, columnaSolicitada: P(2), cantidad: 1, fecha: hoy,
                                   afectacion: null, tasaImpuestoFraccion: 0.18m,
                                   plantillaOpcional: null, // fallback a P1
                                   mostrarConImpuestos: false);

            Assert.That(dto, Is.Not.Null);
            Assert.That(dto!.ColumnaAplicada.Numero, Is.EqualTo(1)); // cayó a base
            Assert.That(dto.Neto.Monto, Is.EqualTo(20m));
            Assert.That(dto.Bruto.Monto, Is.EqualTo(20m));
        }

        [Test]
        public void Con_MatrizPorVolumen_Devuelve_Tramo_Aplicado()
        {
            var svc = new ResolverPrecioService();
            var prod = PrecioProducto.CrearNuevo(SKU("CAP-102"));
            var hoy  = new DateTimeOffset(2025, 1, 12, 8, 0, 0, TimeSpan.Zero);

            // P2 = matriz: [1..10]=30, [11..50]=25, [51..∞]=22
            prod.UpsertMatrizVolumen(P(2), Mat((1,10,30m), (11,50,25m), (51,null,22m)));

            var dto = svc.Resolver(prod, columnaSolicitada: P(2), cantidad: 15, fecha: hoy,
                                   afectacion: null, tasaImpuestoFraccion: 0.18m,
                                   plantillaOpcional: null, mostrarConImpuestos: true);

            Assert.That(dto, Is.Not.Null);
            Assert.That(dto!.Origen, Is.EqualTo(PrecioResueltoOrigen.PorVolumen));
            Assert.That(dto.ValorOriginal.Monto, Is.EqualTo(25m)); // tramo 11..50
            Assert.That(dto.TramoDesde, Is.EqualTo(11));
            Assert.That(dto.TramoHasta, Is.EqualTo(50));
            Assert.That(dto.Mostrar.Monto, Is.EqualTo(25m)); // sin impuestos (afectación null)
        }
    }
}
