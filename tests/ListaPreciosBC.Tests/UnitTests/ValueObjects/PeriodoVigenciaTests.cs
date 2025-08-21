using System;
using NUnit.Framework;
using ListaPreciosBC.Domain.ValueObjects;

namespace ListaPreciosBC.Tests.ValueObjects
{
    [TestFixture]
    public class PeriodoVigenciaTests
    {
        [Test]
        public void Crear_normaliza_a_fecha_y_valida_invariante()
        {
            var d1 = new DateTime(2025, 1, 10, 23, 59, 59);
            var d2 = new DateTime(2025, 1, 15, 5, 0, 0);
            var p = PeriodoVigencia.Crear(d1, d2);

            Assert.That(p.Desde, Is.EqualTo(new DateTime(2025, 1, 10)));
            Assert.That(p.Hasta, Is.EqualTo(new DateTime(2025, 1, 15)));
        }

        [Test]
        public void Crear_con_hasta_anterior_a_desde_lanza()
        {
            var desde = new DateTime(2025, 1, 10);
            var hasta = new DateTime(2025, 1, 9);
            Assert.That(() => PeriodoVigencia.Crear(desde, hasta), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void DesdeFecha_crea_periodo_abierto_y_SoloDia_cierra_mismo_dia()
        {
            var pAbierto = PeriodoVigencia.DesdeFecha(new DateTime(2025, 2, 1, 8, 0, 0));
            Assert.That(pAbierto.Desde, Is.EqualTo(new DateTime(2025, 2, 1)));
            Assert.That(pAbierto.Hasta, Is.Null);

            var pDia = PeriodoVigencia.SoloDia(new DateTime(2025, 3, 5, 23, 0, 0));
            Assert.That(pDia.Desde, Is.EqualTo(new DateTime(2025, 3, 5)));
            Assert.That(pDia.Hasta, Is.EqualTo(new DateTime(2025, 3, 5)));
        }

        [Test]
        public void TryCrear_true_para_valido_false_para_invalido()
        {
            var ok = PeriodoVigencia.TryCrear(new DateTime(2025, 1, 1), new DateTime(2025, 1, 31), out var p1);
            Assert.That(ok, Is.True);
            Assert.That(p1, Is.Not.Null);

            var bad = PeriodoVigencia.TryCrear(new DateTime(2025, 1, 31), new DateTime(2025, 1, 1), out var p2);
            Assert.That(bad, Is.False);
            Assert.That(p2, Is.Null);
        }

        [Test]
        public void EstaVigenteEn_respeta_inclusion_de_bordes_y_abierto()
        {
            var pCerrado = PeriodoVigencia.Crear(new DateTime(2025, 1, 10), new DateTime(2025, 1, 20));

            Assert.That(pCerrado.EstaVigenteEn(new DateTime(2025, 1, 9)),  Is.False);
            Assert.That(pCerrado.EstaVigenteEn(new DateTime(2025, 1, 10)), Is.True);
            Assert.That(pCerrado.EstaVigenteEn(new DateTime(2025, 1, 15)), Is.True);
            Assert.That(pCerrado.EstaVigenteEn(new DateTime(2025, 1, 20)), Is.True);
            Assert.That(pCerrado.EstaVigenteEn(new DateTime(2025, 1, 21)), Is.False);

            var pAbierto = PeriodoVigencia.DesdeFecha(new DateTime(2025, 1, 10));
            Assert.That(pAbierto.EstaVigenteEn(new DateTime(2025, 1, 9)),  Is.False);
            Assert.That(pAbierto.EstaVigenteEn(new DateTime(2025, 1, 10)), Is.True);
            Assert.That(pAbierto.EstaVigenteEn(new DateTime(2026, 5, 1)),  Is.True);
        }

        [Test]
        public void Expirado_y_AunNoVigente_funcionan_correctamente()
        {
            var p = PeriodoVigencia.Crear(new DateTime(2025, 1, 10), new DateTime(2025, 1, 20));
            Assert.That(p.ExpiradoEn(new DateTime(2025, 1, 21)), Is.True);
            Assert.That(p.ExpiradoEn(new DateTime(2025, 1, 20)), Is.False);

            Assert.That(p.AunNoVigenteEn(new DateTime(2025, 1, 9)),  Is.True);
            Assert.That(p.AunNoVigenteEn(new DateTime(2025, 1, 10)), Is.False);
        }

        [Test]
        public void VigenteHoy_depende_de_DateTime_Today()
        {
            // No podemos forzar Today sin fakes; probamos que no explota y retorna bool coherente.
            var hoy = DateTime.Today;
            var p = PeriodoVigencia.Crear(hoy.AddDays(-1), hoy.AddDays(1));
            Assert.That(p.VigenteHoy, Is.True);

            var p2 = PeriodoVigencia.Crear(hoy.AddDays(-10), hoy.AddDays(-1));
            Assert.That(p2.VigenteHoy, Is.False);
        }

        [Test]
        public void SeSuperponeCon_y_Interseccion_cubren_cerrados_y_abiertos()
        {
            var a = PeriodoVigencia.Crear(new DateTime(2025, 1, 10), new DateTime(2025, 1, 20));
            var b = PeriodoVigencia.Crear(new DateTime(2025, 1, 15), new DateTime(2025, 1, 25));
            var c = PeriodoVigencia.Crear(new DateTime(2025, 1, 21), new DateTime(2025, 1, 30));
            var d = PeriodoVigencia.DesdeFecha(new DateTime(2025, 1, 18)); // abierto

            Assert.That(a.SeSuperponeCon(b), Is.True);
            Assert.That(a.SeSuperponeCon(c), Is.False);
            Assert.That(a.SeSuperponeCon(d), Is.True);  // [10..20] con [18..∞] solapan

            var interAB = a.Interseccion(b);
            Assert.That(interAB, Is.Not.Null);
            Assert.That(interAB!.Desde, Is.EqualTo(new DateTime(2025, 1, 15)));
            Assert.That(interAB.Hasta, Is.EqualTo(new DateTime(2025, 1, 20)));

            var interAD = a.Interseccion(d);
            Assert.That(interAD, Is.Not.Null);
            Assert.That(interAD!.Desde, Is.EqualTo(new DateTime(2025, 1, 18)));
            Assert.That(interAD.Hasta, Is.EqualTo(new DateTime(2025, 1, 20)));

            var interAC = a.Interseccion(c);
            Assert.That(interAC, Is.Null);
        }

        [Test]
        public void EsContiguoCon_detecta_fronteras_adyacentes()
        {
            var a = PeriodoVigencia.Crear(new DateTime(2025, 1, 1), new DateTime(2025, 1, 10));
            var b = PeriodoVigencia.Crear(new DateTime(2025, 1, 11), new DateTime(2025, 1, 20));
            var c = PeriodoVigencia.Crear(new DateTime(2025, 1, 12), new DateTime(2025, 1, 15));

            Assert.That(a.EsContiguoCon(b), Is.True);
            Assert.That(b.EsContiguoCon(a), Is.True);
            Assert.That(a.EsContiguoCon(c), Is.False);
        }

        [Test]
        public void UnionSiSolapanOContiguos_une_correctamente_y_lanza_si_separados()
        {
            var a = PeriodoVigencia.Crear(new DateTime(2025, 1, 1),  new DateTime(2025, 1, 10));
            var b = PeriodoVigencia.Crear(new DateTime(2025, 1, 11), new DateTime(2025, 1, 20));
            var c = PeriodoVigencia.Crear(new DateTime(2025, 2,  1),  new DateTime(2025, 2,  5));

            var ab = a.UnionSiSolapanOContiguos(b);
            Assert.That(ab.Desde, Is.EqualTo(new DateTime(2025, 1, 1)));
            Assert.That(ab.Hasta, Is.EqualTo(new DateTime(2025, 1, 20)));

            Assert.That(() => a.UnionSiSolapanOContiguos(c), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void Recortar_y_Extender_respetan_invariantes()
        {
            var p = PeriodoVigencia.Crear(new DateTime(2025, 1, 1), new DateTime(2025, 1, 31));

            var rec = p.RecortarHasta(new DateTime(2025, 1, 15));
            Assert.That(rec.Hasta, Is.EqualTo(new DateTime(2025, 1, 15)));

            var ext = rec.ExtenderHasta(new DateTime(2025, 2, 10));
            Assert.That(ext.Hasta, Is.EqualTo(new DateTime(2025, 2, 10)));

            // Extender no permite reducir
            Assert.That(() => ext.ExtenderHasta(new DateTime(2025, 2, 1)),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Igualdad_hashcode_y_orden_natural()
        {
            var a = PeriodoVigencia.Crear(new DateTime(2025, 1, 1), new DateTime(2025, 1, 10));
            var b = PeriodoVigencia.Crear(new DateTime(2025, 1, 1), new DateTime(2025, 1, 10));
            var c = PeriodoVigencia.Crear(new DateTime(2025, 1, 1), null);

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
            Assert.That(a.Equals(c), Is.False);

            // Orden: por Desde ascendente; luego Hasta (null como ∞, va después)
            var arr = new[] { c, a };
            Array.Sort(arr);
            Assert.That(arr[0], Is.EqualTo(a));
            Assert.That(arr[1], Is.EqualTo(c));
        }

        [Test]
        public void ToString_muestra_formato_legible()
        {
            var a = PeriodoVigencia.Crear(new DateTime(2025, 1, 1), new DateTime(2025, 1, 10));
            var b = PeriodoVigencia.DesdeFecha(new DateTime(2025, 1, 1));

            Assert.That(a.ToString(), Is.EqualTo("2025-01-01..2025-01-10"));
            Assert.That(b.ToString(), Is.EqualTo("2025-01-01..∞"));
        }
    }
}
