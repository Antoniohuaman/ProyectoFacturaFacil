using System;
using System.Linq;
using NUnit.Framework;
using ListaPreciosBC.Domain.ValueObjects;

namespace ListaPreciosBC.Tests.ValueObjects
{
    [TestFixture]
    public class ConfiguracionColumnaPrecioTests
    {
        private static IdentificadorColumnaPrecio P(byte n) => IdentificadorColumnaPrecio.DesdeNumero(n);
        private static NombreColumnaPrecio N(string s) => NombreColumnaPrecio.Crear(s);
        private static ModoValorizacionColumna Fijo => ModoValorizacionColumna.Fijo;
        private static ModoValorizacionColumna Vol  => ModoValorizacionColumna.PorVolumen;

        [Test]
        public void Crear_valido_con_defaults_y_orden_por_defecto_igual_a_Id()
        {
            var cfg = ConfiguracionColumnaPrecio.Crear(P(3), N("Mayorista"), Fijo);
            Assert.That(cfg.Id.Numero, Is.EqualTo(3));
            Assert.That(cfg.Nombre.Valor, Is.EqualTo("Mayorista"));
            Assert.That(cfg.Modo, Is.EqualTo(Fijo));
            Assert.That(cfg.EsBase, Is.False);
            Assert.That(cfg.Visible, Is.True);
            Assert.That(cfg.Orden, Is.EqualTo(3)); // por defecto usa Id.Numero
        }

        [Test]
        public void Crear_con_parametros_completos_funciona()
        {
            var cfg = ConfiguracionColumnaPrecio.Crear(P(1), N("Base"), Vol, esBase: true, visible: false, orden: 7);
            Assert.That(cfg.EsBase, Is.True);
            Assert.That(cfg.Visible, Is.False);
            Assert.That(cfg.Orden, Is.EqualTo(7));
            Assert.That(cfg.Modo, Is.EqualTo(Vol));
        }

        [Test]
        public void Crear_invalido_valida_nulos_y_rango_de_orden()
        {
            Assert.That(() => ConfiguracionColumnaPrecio.Crear(null!, N("X"), Fijo), Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => ConfiguracionColumnaPrecio.Crear(P(1), null!, Fijo), Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => ConfiguracionColumnaPrecio.Crear(P(1), N("X"), null!), Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => ConfiguracionColumnaPrecio.Crear(P(1), N("X"), Fijo, orden: 0),  Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => ConfiguracionColumnaPrecio.Crear(P(1), N("X"), Fijo, orden: 11), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void TryCrear_true_en_valido_false_en_invalido()
        {
            var ok = ConfiguracionColumnaPrecio.TryCrear(P(2), N("Promo"), Fijo, out var cfg, orden: 5);
            Assert.That(ok, Is.True);
            Assert.That(cfg, Is.Not.Null);
            Assert.That(cfg!.Orden, Is.EqualTo(5));

            var bad = ConfiguracionColumnaPrecio.TryCrear(P(2), N("X"), Fijo, out var cfg2, orden: 0);
            Assert.That(bad, Is.False);
            Assert.That(cfg2, Is.Null);

            var badNull = ConfiguracionColumnaPrecio.TryCrear(null, N("X"), Fijo, out var cfg3);
            Assert.That(badNull, Is.False);
            Assert.That(cfg3, Is.Null);
        }

        [Test]
        public void Withers_generan_nuevas_instancias_inmutables()
        {
            var original = ConfiguracionColumnaPrecio.Crear(P(4), N("Distribuidor"), Fijo, esBase: false, visible: true, orden: 4);

            var renombrada = original.Renombrar(N("Canal"));
            Assert.That(renombrada.Nombre.Valor, Is.EqualTo("Canal"));
            Assert.That(renombrada.Id, Is.EqualTo(original.Id));
            Assert.That(ReferenceEquals(original, renombrada), Is.False);

            var modo = original.CambiarModo(Vol);
            Assert.That(modo.Modo, Is.EqualTo(Vol));
            Assert.That(modo.Nombre, Is.EqualTo(original.Nombre));

            var baseOn = original.MarcarComoBase();
            Assert.That(baseOn.EsBase, Is.True);

            var baseOff = baseOn.DesmarcarComoBase();
            Assert.That(baseOff.EsBase, Is.False);

            var oculta = original.Ocultar();
            Assert.That(oculta.Visible, Is.False);

            var visible = oculta.Mostrar();
            Assert.That(visible.Visible, Is.True);

            var reorden = original.ConOrden(1);
            Assert.That(reorden.Orden, Is.EqualTo(1));
        }

        [Test]
        public void CompareTo_ordena_por_Orden_luego_por_Id()
        {
            var a = ConfiguracionColumnaPrecio.Crear(P(10), N("X"), Fijo, orden: 2);
            var b = ConfiguracionColumnaPrecio.Crear(P(1),  N("Y"), Fijo, orden: 1);
            var c = ConfiguracionColumnaPrecio.Crear(P(3),  N("Z"), Fijo, orden: 2); // mismo orden que 'a', distinto Id

            var arr = new[] { a, b, c }.OrderBy(x => x).ToArray();

            // Primero el de orden 1 (b), luego los de orden 2; entre a/c decide por Id.Numero (3 antes que 10)
            Assert.That(arr[0], Is.EqualTo(b));
            Assert.That(arr[1], Is.EqualTo(c));
            Assert.That(arr[2], Is.EqualTo(a));
        }

        [Test]
        public void Igualdad_y_hashcode_consideran_todas_las_propiedades()
        {
            var x1 = ConfiguracionColumnaPrecio.Crear(P(2), N("Mayorista"), Fijo, esBase: false, visible: true,  orden: 5);
            var x2 = ConfiguracionColumnaPrecio.Crear(P(2), N("Mayorista"), Fijo, esBase: false, visible: true,  orden: 5);
            var y  = ConfiguracionColumnaPrecio.Crear(P(2), N("Mayorista"), Fijo, esBase: true,  visible: true,  orden: 5);
            var z  = ConfiguracionColumnaPrecio.Crear(P(2), N("Mayorista"), Fijo, esBase: false, visible: true,  orden: 6);

            Assert.That(x1, Is.EqualTo(x2));
            Assert.That(x1.GetHashCode(), Is.EqualTo(x2.GetHashCode()));

            Assert.That(x1.Equals(y), Is.False); // EsBase distinto
            Assert.That(x1.Equals(z), Is.False); // Orden distinto
        }

        [Test]
        public void ToString_contiene_datos_relevantes()
        {
            var cfg = ConfiguracionColumnaPrecio.Crear(P(1), N("Base"), Fijo, esBase: true, visible: true, orden: 1);
            var s = cfg.ToString();
            Assert.That(s, Does.Contain("1:"));
            Assert.That(s, Does.Contain("P1"));
            Assert.That(s, Does.Contain("Base"));
            Assert.That(s, Does.Contain("Fijo"));
        }
    }
}