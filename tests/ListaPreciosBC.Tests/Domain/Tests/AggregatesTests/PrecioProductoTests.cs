using System;
using System.Linq;
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

    private static EmpresaId EMP(string v = "EMP-TEST") => EmpresaId.From(v);
    private static ProductoId PID() => ProductoId.New();

        private static IdentificadorColumnaPrecio P(byte n) => IdentificadorColumnaPrecio.DesdeNumero(n);
        private static UnidadDeMedida U(string codigo = "NIU") => UnidadDeMedida.From(codigo);

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
            var emp = EMP();
            var pid = PID();
            var agg = PrecioProducto.CrearNuevo(emp, pid);
            Assert.That(agg.EmpresaId, Is.EqualTo(emp));
            Assert.That(agg.ProductoId, Is.EqualTo(pid));
            Assert.That(agg.Version, Is.EqualTo(0));
            Assert.That(agg.PreciosPorUnidad, Is.Empty);
        }

        [Test]
        public void UpsertPrecioFijo_guarda_y_emite_evento_columna_y_base_si_P1_vigente()
        {
            var agg = PrecioProducto.CrearNuevo(EMP(), PID());
            var v0 = agg.Version;

            var hoy = new DateTimeOffset(2025, 1, 1, 10, 0, 0, TimeSpan.Zero);
            agg.UpsertPrecioFijo(P(1), U(), Vp(10m), Vigencia(hoy.AddDays(-1), null), "u", hoy);

            Assert.That(agg.Version, Is.EqualTo(v0 + 1));
            var registro = agg.PreciosPorUnidad.Single();
            Assert.That(registro.ColumnaId.Numero, Is.EqualTo(1));
            Assert.That(registro.UnidadDeMedida, Is.EqualTo(U()));
            Assert.That(registro.TienePrecioFijo, Is.True);

            // Eventos: PrecioColumnaActualizada (+ PrecioBaseVigenteEstablecido porque es P1 vigente)
            Assert.That(agg.DomainEvents, Has.Exactly(1).InstanceOf<PrecioColumnaActualizada>());
            Assert.That(agg.DomainEvents, Has.Exactly(1).InstanceOf<PrecioBaseVigenteEstablecido>());
            // Tenancy en eventos
            var evCol = (PrecioColumnaActualizada)agg.DomainEvents.First(e => e is PrecioColumnaActualizada);
            Assert.That(evCol.EmpresaId, Is.EqualTo(agg.EmpresaId));
        }

        [Test]
        public void EliminarPrecioFijo_remueve_y_emite_evento()
        {
            var agg = PrecioProducto.CrearNuevo(EMP(), PID());
            agg.UpsertPrecioFijo(P(2), U(), Vp(12m), Vigencia(DateTimeOffset.UtcNow.AddDays(-1)));

            agg.ClearDomainEvents();
            var v0 = agg.Version;

            agg.EliminarPrecioFijo(P(2), U(), "u2", new DateTimeOffset(2025, 1, 2, 9, 0, 0, TimeSpan.Zero));

            Assert.That(agg.PreciosPorUnidad.Any(x => x.ColumnaId.Numero == 2 && x.UnidadDeMedida == U()), Is.False);
            Assert.That(agg.Version, Is.EqualTo(v0 + 1));
            Assert.That(agg.DomainEvents, Has.Exactly(1).InstanceOf<PrecioColumnaActualizada>());
        }

        [Test]
        public void UpsertMatrizVolumen_guarda_y_emite_evento_matriz_y_base_si_P1_tiene_tramo()
        {
            var agg = PrecioProducto.CrearNuevo(EMP(), PID());
            var v0 = agg.Version;

            var matriz = Mat((1, 10, 9m), (11, 20, 8m), (21, null, 7m));
            var cuando = new DateTimeOffset(2025, 1, 3, 8, 0, 0, TimeSpan.Zero);

            agg.UpsertMatrizVolumen(P(1), U(), matriz, "u3", cuando, cantidadReferenciaParaEventoBase: 5);

            Assert.That(agg.Version, Is.EqualTo(v0 + 1));
            Assert.That(agg.PreciosPorUnidad.Single().TieneMatrizVolumen, Is.True);

            Assert.That(agg.DomainEvents, Has.Exactly(1).InstanceOf<MatrizVolumenActualizada>());
            Assert.That(agg.DomainEvents, Has.Exactly(1).InstanceOf<PrecioBaseVigenteEstablecido>());
        }

        [Test]
        public void EliminarMatrizVolumen_remueve_y_emite_evento()
        {
            var agg = PrecioProducto.CrearNuevo(EMP(), PID());
            agg.UpsertMatrizVolumen(P(3), U(), Mat((1, 10, 15m)), "u", DateTimeOffset.UtcNow);

            agg.ClearDomainEvents();
            var v0 = agg.Version;

            agg.EliminarMatrizVolumen(P(3), U(), "u4", new DateTimeOffset(2025, 1, 4, 9, 0, 0, TimeSpan.Zero));

            Assert.That(agg.PreciosPorUnidad.Any(x => x.ColumnaId.Numero == 3 && x.UnidadDeMedida == U()), Is.False);
            Assert.That(agg.Version, Is.EqualTo(v0 + 1));
            Assert.That(agg.DomainEvents, Has.Exactly(1).InstanceOf<MatrizVolumenActualizada>());
        }

        [Test]
        public void ObtenerPrecioVigente_prioriza_fijo_si_vigente_sino_busca_en_matriz()
        {
            var agg = PrecioProducto.CrearNuevo(EMP(), PID());

            // En P4 configuramos fijo vigente
            var hoy = new DateTimeOffset(2025, 1, 5, 10, 0, 0, TimeSpan.Zero);
            agg.UpsertPrecioFijo(P(4), U(), Vp(20m), Vigencia(hoy.AddDays(-1), hoy.AddDays(1)), "u", hoy);

            // En P5 configuramos sólo matriz
            agg.UpsertMatrizVolumen(P(5), U(), Mat((1, 10, 30m), (11, 50, 25m)));

            // 1) Fijo vigente en P4
            var r1 = agg.ObtenerPrecioVigente(P(4), U(), hoy, 3)!;
            Assert.That(r1.Valor.Monto, Is.EqualTo(20m));
            Assert.That(r1.Origen, Is.EqualTo(PrecioResueltoOrigen.Fijo));

            // 2) Matriz en P5, cantidad 15 → tramo de 25
            var r2 = agg.ObtenerPrecioVigente(P(5), U(), hoy, 15)!;
            Assert.That(r2.Valor.Monto, Is.EqualTo(25m));
            Assert.That(r2.Origen, Is.EqualTo(PrecioResueltoOrigen.PorVolumen));

            // 3) Cantidad inválida o sin configuración → null
            var r3 = agg.ObtenerPrecioVigente(P(5), U(), hoy, 0);
            Assert.That(r3, Is.Null);
        }

        [Test]
        public void Upsert_cambia_de_matriz_a_fijo_y_viceversa_en_la_misma_columna()
        {
            var agg = PrecioProducto.CrearNuevo(EMP(), PID());

            // Primero matriz en P2
            agg.UpsertMatrizVolumen(P(2), U(), Mat((1, 10, 15m)));

            // Luego fijo en P2 -> debe eliminar la matriz
            agg.UpsertPrecioFijo(P(2), U(), Vp(12m), Vigencia(DateTimeOffset.UtcNow.AddDays(-1)));

            var registro = agg.PreciosPorUnidad.Single(x => x.ColumnaId.Numero == 2 && x.UnidadDeMedida == U());
            Assert.That(registro.TieneMatrizVolumen, Is.False);
            Assert.That(registro.TienePrecioFijo, Is.True);
        }

        [Test]
        public void UpsertPrecioFijo_por_unidad_crea_registros_independientes()
        {
            var agg = PrecioProducto.CrearNuevo(EMP(), PID());
            var fecha = DateTimeOffset.UtcNow;

            agg.UpsertPrecioFijo(P(2), U("NIU"), Vp(10m), Vigencia(fecha.AddDays(-1), fecha.AddDays(1)));
            agg.UpsertPrecioFijo(P(2), U("KGM"), Vp(25m), Vigencia(fecha.AddDays(-1), fecha.AddDays(1)));

            Assert.That(agg.PreciosPorUnidad.Count, Is.EqualTo(2));

            var resNiu = agg.ObtenerPrecioVigente(P(2), U("NIU"), fecha, 5)!;
            var resKgm = agg.ObtenerPrecioVigente(P(2), U("KGM"), fecha, 5)!;

            Assert.That(resNiu.Valor.Monto, Is.EqualTo(10m));
            Assert.That(resKgm.Valor.Monto, Is.EqualTo(25m));
        }

        [Test]
        public void Eliminar_precio_fijo_por_unidad_no_afecta_otros()
        {
            var agg = PrecioProducto.CrearNuevo(EMP(), PID());
            var fecha = DateTimeOffset.UtcNow;

            agg.UpsertPrecioFijo(P(3), U("NIU"), Vp(8m), Vigencia(fecha.AddDays(-1), fecha.AddDays(1)));
            agg.UpsertPrecioFijo(P(3), U("KGM"), Vp(30m), Vigencia(fecha.AddDays(-1), fecha.AddDays(1)));

            agg.EliminarPrecioFijo(P(3), U("KGM"));

            Assert.That(agg.ObtenerPrecioVigente(P(3), U("KGM"), fecha, 2), Is.Null);
            Assert.That(agg.ObtenerPrecioVigente(P(3), U("NIU"), fecha, 2), Is.Not.Null);
        }

        [Test]
        public void Matriz_por_unidad_se_resuelve_individualmente()
        {
            var agg = PrecioProducto.CrearNuevo(EMP(), PID());
            var fecha = DateTimeOffset.UtcNow;

            agg.UpsertMatrizVolumen(P(4), U("NIU"), Mat((1, 10, 50m), (11, null, 40m)));
            agg.UpsertMatrizVolumen(P(4), U("KGM"), Mat((1, null, 70m)));

            var resUnidad = agg.ObtenerPrecioVigente(P(4), U("NIU"), fecha, 12)!;
            var resKg = agg.ObtenerPrecioVigente(P(4), U("KGM"), fecha, 12)!;

            Assert.That(resUnidad.Valor.Monto, Is.EqualTo(40m));
            Assert.That(resKg.Valor.Monto, Is.EqualTo(70m));
        }
    }
}
