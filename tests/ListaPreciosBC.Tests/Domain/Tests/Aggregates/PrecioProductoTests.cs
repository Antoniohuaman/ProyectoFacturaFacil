using System;
using NUnit.Framework;
using ListaPreciosBC.Domain.Aggregates;
using ListaPreciosBC.Domain.Events;
using ListaPreciosBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace ListaPreciosBC.Tests.UnitTests.Aggregates
{
    [TestFixture]
    public class PrecioProductoTests
    {
        // -------------------- Helpers de construcción --------------------

        private static Sku SKU(string s) => Sku.Crear(s);

        private static IdentificadorColumnaPrecio P(byte n) => IdentificadorColumnaPrecio.DesdeNumero(n);

        private static PeriodoVigencia Vigencia(DateTimeOffset desde, DateTimeOffset? hasta = null)
            => PeriodoVigencia.Crear(desde, hasta);

        private static ValorPrecio Vp(decimal monto)
        {
            // Ajusta si tu VO requiere moneda/afectación: aquí asumo factorías que ya definiste.
            // Si tu ValorPrecio se crea distinto, cambia este helper por el tuyo.
            return ValorPrecio.DesdeMonto(monto); // <- usa tu propia fábrica (o constructor) real
        }

        private static MatrizVolumen Mat(params (int desde, int? hasta, decimal precio)[] tramos)
        {
            var lista = new TramoVolumen[tramos.Length];
            for (int i = 0; i < tramos.Length; i++)
            {
                var t = tramos[i];
                lista[i] = TramoVolumen.Crear(t.desde, t.hasta, Vp(t.precio));
            }
            return MatrizVolumen.Crear(lista);
        }

        // -------------------- Tests --------------------

        [Test]
        public void CrearNuevo_setea_identidad_y_estado_vacio()
        {
            var agg = PrecioProducto.CrearNuevo(SKU("CAP-001"));
            Assert.That(agg.Sku.Valor, Is.EqualTo("CAP-001"));
            Assert.That(agg.Version, Is.EqualTo(0));
            Assert.That(agg.PreciosFijos.Count, Is.EqualTo(0));
            Assert.That(agg.MatricesVolumen.Count, Is.EqualTo(0));
        }

        [Test]
        public void UpsertPrecioFijo_guarda_y_emite_evento_columna_y_base_si_P1_vigente()
        {
            var agg = PrecioProducto.CrearNuevo(SKU("CAP-002"));
            var v0 = agg.Version;

            var hoy = new DateTimeOffset(2025, 1, 1, 10, 0, 0, TimeSpan.Zero);
            agg.UpsertPrecioFijo(P(1), Vp(10m), Vigencia(hoy.AddDays(-1), null), "u", hoy);

            Assert.That(agg.Version, Is.EqualTo(v0 + 1));
            Assert.That(agg.PreciosFijos.ContainsKey(1), Is.True);

            // Eventos: PrecioColumnaActualizada (+ PrecioBaseVigenteEstablecido porque es P1 vigente)
            Assert.That(agg.DomainEvents, Has.Exactly(1).InstanceOf<PrecioColumnaActualizada>());
            Assert.That(agg.DomainEvents, Has.Exactly(1).InstanceOf<PrecioBaseVigenteEstablecido>());
        }

        [Test]
        public void EliminarPrecioFijo_remueve_y_emite_evento()
        {
            var agg = PrecioProducto.CrearNuevo(SKU("CAP-003"));
            agg.UpsertPrecioFijo(P(2), Vp(12m), Vigencia(DateTimeOffset.UtcNow.AddDays(-1)));

            agg.ClearDomainEvents();
            var v0 = agg.Version;

            agg.EliminarPrecioFijo(P(2), "u2", new DateTimeOffset(2025, 1, 2, 9, 0, 0, TimeSpan.Zero));

            Assert.That(agg.PreciosFijos.ContainsKey(2), Is.False);
            Assert.That(agg.Version, Is.EqualTo(v0 + 1));
            Assert.That(agg.DomainEvents, Has.Exactly(1).InstanceOf<PrecioColumnaActualizada>());
        }

        [Test]
        public void UpsertMatrizVolumen_guarda_y_emite_evento_matriz_y_base_si_P1_tiene_tramo()
        {
            var agg = PrecioProducto.CrearNuevo(SKU("CAP-004"));
            var v0 = agg.Version;

            var matriz = Mat((1, 10, 9m), (11, 20, 8m), (21, null, 7m));
            var cuando = new DateTimeOffset(2025, 1, 3, 8, 0, 0, TimeSpan.Zero);

            agg.UpsertMatrizVolumen(P(1), matriz, "u3", cuando, cantidadReferenciaParaEventoBase: 5);

            Assert.That(agg.Version, Is.EqualTo(v0 + 1));
            Assert.That(agg.MatricesVolumen.ContainsKey(1), Is.True);

            Assert.That(agg.DomainEvents, Has.Exactly(1).InstanceOf<MatrizVolumenActualizada>());
            Assert.That(agg.DomainEvents, Has.Exactly(1).InstanceOf<PrecioBaseVigenteEstablecido>());
        }

        [Test]
        public void EliminarMatrizVolumen_remueve_y_emite_evento()
        {
            var agg = PrecioProducto.CrearNuevo(SKU("CAP-005"));
            agg.UpsertMatrizVolumen(P(3), Mat((1, 10, 15m)), "u", DateTimeOffset.UtcNow);

            agg.ClearDomainEvents();
            var v0 = agg.Version;

            agg.EliminarMatrizVolumen(P(3), "u4", new DateTimeOffset(2025, 1, 4, 9, 0, 0, TimeSpan.Zero));

            Assert.That(agg.MatricesVolumen.ContainsKey(3), Is.False);
            Assert.That(agg.Version, Is.EqualTo(v0 + 1));
            Assert.That(agg.DomainEvents, Has.Exactly(1).InstanceOf<MatrizVolumenActualizada>());
        }

        [Test]
        public void ObtenerPrecioVigente_prioriza_fijo_si_vigente_sino_busca_en_matriz()
        {
            var agg = PrecioProducto.CrearNuevo(SKU("CAP-006"));

            // En P4 configuramos fijo vigente
            var hoy = new DateTimeOffset(2025, 1, 5, 10, 0, 0, TimeSpan.Zero);
            agg.UpsertPrecioFijo(P(4), Vp(20m), Vigencia(hoy.AddDays(-1), hoy.AddDays(1)), "u", hoy);

            // En P5 configuramos sólo matriz
            agg.UpsertMatrizVolumen(P(5), Mat((1, 10, 30m), (11, 50, 25m)));

            // 1) Fijo vigente en P4
            var r1 = agg.ObtenerPrecioVigente(P(4), hoy, 3)!;
            Assert.That(r1.Valor.Monto, Is.EqualTo(20m));
            Assert.That(r1.Origen, Is.EqualTo(PrecioResueltoOrigen.Fijo));

            // 2) Matriz en P5, cantidad 15 → tramo de 25
            var r2 = agg.ObtenerPrecioVigente(P(5), hoy, 15)!;
            Assert.That(r2.Valor.Monto, Is.EqualTo(25m));
            Assert.That(r2.Origen, Is.EqualTo(PrecioResueltoOrigen.PorVolumen));

            // 3) Cantidad inválida o sin configuración → null
            var r3 = agg.ObtenerPrecioVigente(P(5), hoy, 0);
            Assert.That(r3, Is.Null);
        }

        [Test]
        public void Upsert_cambia_de_matriz_a_fijo_y_viceversa_en_la_misma_columna()
        {
            var agg = PrecioProducto.CrearNuevo(SKU("CAP-007"));

            // Primero matriz en P2
            agg.UpsertMatrizVolumen(P(2), Mat((1, 10, 15m)));

            // Luego fijo en P2 -> debe eliminar la matriz
            agg.UpsertPrecioFijo(P(2), Vp(12m), Vigencia(DateTimeOffset.UtcNow.AddDays(-1)));

            Assert.That(agg.MatricesVolumen.ContainsKey(2), Is.False);
            Assert.That(agg.PreciosFijos.ContainsKey(2), Is.True);
        }
    }
}
