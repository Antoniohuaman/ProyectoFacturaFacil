using System;
using System.Linq;
using NUnit.Framework;
using ListaPreciosBC.Domain.Aggregates;
using ListaPreciosBC.Domain.Events;
using ListaPreciosBC.Domain.ValueObjects;

namespace ListaPreciosBC.Tests.UnitTests.Aggregates
{
    [TestFixture]
    public class ListaPrecioTests
    {
        // -------------------- Helpers --------------------
        private static IdentificadorColumnaPrecio P(byte n) => IdentificadorColumnaPrecio.DesdeNumero(n);
        private static NombreColumnaPrecio N(string s)      => NombreColumnaPrecio.Crear(s);
        private static ModoValorizacionColumna Fijo         => ModoValorizacionColumna.Fijo;
        private static ModoValorizacionColumna Vol          => ModoValorizacionColumna.PorVolumen;

        private static ConfiguracionColumnaPrecio Cfg(byte p, string nombre, bool @base, byte orden, bool visible = true, bool vol = false)
            => ConfiguracionColumnaPrecio.Crear(P(p), N(nombre), vol ? Vol : Fijo, esBase: @base, visible: visible, orden: orden);

        private static PlantillaColumnasPrecio PlantillaBasica()
            => PlantillaColumnasPrecio.Crear(new[]
            {
                Cfg(1, "Precio base", @base:true,  orden:1),
                Cfg(2, "Mayorista",   @base:false, orden:2),
                Cfg(3, "Distrib.",    @base:false, orden:3, vol:true),
            });

        private static ListaPrecio NuevaLista(PlantillaColumnasPrecio? plantilla = null, string usuario = "sys", DateTimeOffset? cuando = null)
            => ListaPrecio.CrearNueva(Guid.NewGuid(), plantilla ?? PlantillaBasica(), usuario, cuando ?? new DateTimeOffset(2025,1,1,12,0,0,TimeSpan.Zero));

        private static PlantillaDeColumnasActualizada UltimoEvento(ListaPrecio agg)
            => agg.DomainEvents.Last() as PlantillaDeColumnasActualizada
               ?? throw new AssertionException("No se encontró PlantillaDeColumnasActualizada.");

        // -------------------- Tests --------------------

        [Test]
        public void CrearConPlantillaPorDefecto_emite_evento_y_deja_base_correcta()
        {
            var cuando = new DateTimeOffset(2025,1,1,8,0,0, TimeSpan.Zero);
            var agg = ListaPrecio.CrearConPlantillaPorDefecto(Guid.NewGuid(), "sys", cuando);

            Assert.That(agg.Version, Is.EqualTo(1)); // creación ya emite un evento
            Assert.That(agg.Plantilla.Base.Id.Numero, Is.EqualTo(1));
            Assert.That(agg.DomainEvents.Count, Is.EqualTo(1));

            var ev = UltimoEvento(agg);
            Assert.That(ev.ListaPrecioId, Is.EqualTo(agg.Id));
            Assert.That(ev.Version, Is.EqualTo(1));
            Assert.That(ev.Usuario, Is.EqualTo("sys"));
            Assert.That(ev.OcurrioEn, Is.EqualTo(cuando));
            Assert.That(ev.NuevaPlantilla.Base.Id.Numero, Is.EqualTo(1));
        }

        [Test]
        public void RenombrarColumna_actualiza_plantilla_incrementa_version_y_emite_evento()
        {
            var agg = NuevaLista();
            agg.ClearDomainEvents(); // solo medimos esta operación
            var v0 = agg.Version;

            var cuando = new DateTimeOffset(2025, 1, 2, 10, 0, 0, TimeSpan.Zero);
            agg.RenombrarColumna(P(2), N("Mayorista Plus"), "u1", cuando);

            Assert.That(agg.Plantilla.Obtener(P(2)).Nombre.Valor, Is.EqualTo("Mayorista Plus"));
            Assert.That(agg.Version, Is.EqualTo(v0 + 1));
            Assert.That(agg.DomainEvents.Count, Is.EqualTo(1));

            var ev = UltimoEvento(agg);
            Assert.That(ev.Usuario, Is.EqualTo("u1"));
            Assert.That(ev.OcurrioEn, Is.EqualTo(cuando));
            Assert.That(ev.NuevaPlantilla.Obtener(P(2)).Nombre.Valor, Is.EqualTo("Mayorista Plus"));
        }

        [Test]
        public void CambiarModoColumna_y_MarcarComoBase_funcionan_y_emiten_evento()
        {
            var agg = NuevaLista();
            agg.ClearDomainEvents();
            var v0 = agg.Version;

            // Cambiar modo
            var t1 = new DateTimeOffset(2025, 1, 3, 9, 0, 0, TimeSpan.Zero);
            agg.CambiarModoColumna(P(2), Vol, "u2", t1);
            Assert.That(agg.Plantilla.Obtener(P(2)).Modo, Is.EqualTo(Vol));
            Assert.That(agg.Version, Is.EqualTo(v0 + 1));

            // Marcar base
            var t2 = t1.AddHours(1);
            agg.MarcarColumnaComoBase(P(2), "u2", t2);
            Assert.That(agg.Plantilla.Base.Id.Numero, Is.EqualTo(2));
            Assert.That(agg.Version, Is.EqualTo(v0 + 2));
            Assert.That(agg.DomainEvents.Count, Is.EqualTo(2));

            var ev = UltimoEvento(agg);
            Assert.That(ev.Version, Is.EqualTo(v0 + 2));
            Assert.That(ev.NuevaPlantilla.Base.Id.Numero, Is.EqualTo(2));
        }

        [Test]
        public void Mostrar_y_Ocultar_respetan_regla_de_no_quedarse_sin_visibles()
        {
            var plantilla = PlantillaColumnasPrecio.Crear(new[]
            {
                Cfg(1, "Base", @base:true,  orden:1, visible:true),
                Cfg(2, "Aux",  @base:false, orden:2, visible:false),
            });
            var agg = NuevaLista(plantilla);
            agg.ClearDomainEvents();
            var v0 = agg.Version;

            // No puede ocultar la última visible
            Assert.That(() => agg.OcultarColumna(P(1), "u3", DateTimeOffset.UtcNow),
                Throws.TypeOf<InvalidOperationException>());

            // Mostrar P2 y luego ocultar P1 (dos operaciones válidas)
            agg.MostrarColumna(P(2), "u3", new DateTimeOffset(2025,1,4,11,0,0,TimeSpan.Zero));
            Assert.That(agg.Version, Is.EqualTo(v0 + 1));

            agg.OcultarColumna(P(1), "u3", new DateTimeOffset(2025,1,4,11,5,0,TimeSpan.Zero));
            Assert.That(agg.Plantilla.Obtener(P(2)).Visible, Is.True);
            Assert.That(agg.Plantilla.Obtener(P(1)).Visible, Is.False);
            Assert.That(agg.Version, Is.EqualTo(v0 + 2));
            Assert.That(agg.DomainEvents.Count, Is.EqualTo(2));
        }

        [Test]
        public void CambiarOrden_hace_swap_si_esta_ocupado_y_emite_evento()
        {
            var agg = NuevaLista();
            agg.ClearDomainEvents();
            var v0 = agg.Version;

            // Orden actual: P1=1, P2=2, P3=3. Movemos P1 al orden 3 (swap con P3)
            agg.CambiarOrdenColumna(P(1), 3, "u4", new DateTimeOffset(2025,1,5,9,0,0,TimeSpan.Zero));

            Assert.That(agg.Plantilla.Columnas[0].Id.Numero, Is.EqualTo(3)); // ahora P3 está en 1
            Assert.That(agg.Plantilla.Columnas[2].Id.Numero, Is.EqualTo(1)); // P1 pasó a 3
            Assert.That(agg.Version, Is.EqualTo(v0 + 1));
            Assert.That(agg.DomainEvents.Count, Is.EqualTo(1));
        }

        [Test]
        public void Agregar_y_Eliminar_aplican_reglas_y_emiten_eventos()
        {
            var agg = NuevaLista();
            agg.ClearDomainEvents();
            var v0 = agg.Version;

            // Agregar P4
            var nueva = Cfg(4, "VIP", @base:false, orden:4, visible:true);
            agg.AgregarColumna(nueva, "u5", new DateTimeOffset(2025,1,6,10,0,0,TimeSpan.Zero));
            Assert.That(agg.Plantilla.Existe(P(4)), Is.True);
            Assert.That(agg.Version, Is.EqualTo(v0 + 1));
            Assert.That(agg.DomainEvents.Count, Is.EqualTo(1));

            // No puede eliminar Base
            Assert.That(() => agg.EliminarColumna(P(1), "u5", DateTimeOffset.UtcNow),
                Throws.TypeOf<InvalidOperationException>());

            // Eliminar P4
            agg.EliminarColumna(P(4), "u5", new DateTimeOffset(2025,1,6,10,5,0,TimeSpan.Zero));
            Assert.That(agg.Plantilla.Existe(P(4)), Is.False);
            Assert.That(agg.Version, Is.EqualTo(v0 + 2));
            Assert.That(agg.DomainEvents.Count, Is.EqualTo(2));
        }

        [Test]
        public void ReemplazarColumna_y_EstablecerPlantilla_actualizan_y_versionan()
        {
            var agg = NuevaLista();
            agg.ClearDomainEvents();
            var v0 = agg.Version;

            // Reemplazar P2 (mismo Id) cambiando nombre
            var cfg2 = agg.Plantilla.Obtener(P(2)).Renombrar(N("MAY+"));
            agg.ReemplazarColumna(cfg2, "u6", new DateTimeOffset(2025,1,7,9,0,0,TimeSpan.Zero));

            Assert.That(agg.Plantilla.Obtener(P(2)).Nombre.Valor, Is.EqualTo("MAY+"));
            Assert.That(agg.Version, Is.EqualTo(v0 + 1));
            Assert.That(agg.DomainEvents.Count, Is.EqualTo(1));

            // Establecer plantilla completa nueva
            var nuevaPlantilla = PlantillaColumnasPrecio.Crear(new[]
            {
                Cfg(1, "Base", @base:true,  orden:1),
                Cfg(3, "Distrib.", @base:false, orden:2, vol:true),
            });

            var t2 = new DateTimeOffset(2025,1,7,9,5,0,TimeSpan.Zero);
            agg.EstablecerPlantilla(nuevaPlantilla, "u6", t2);

            Assert.That(agg.Plantilla.Count, Is.EqualTo(2));
            Assert.That(agg.Version, Is.EqualTo(v0 + 2));
            Assert.That(agg.UltimoUsuario, Is.EqualTo("u6"));
            Assert.That(agg.UltimaActualizacion, Is.EqualTo(t2));
            Assert.That(agg.DomainEvents.Count, Is.EqualTo(2));

            var ev = UltimoEvento(agg);
            Assert.That(ev.NuevaPlantilla.Columnas.Select(c => c.Id.Numero), Is.EqualTo(new byte[]{1,3}));
        }

        [Test]
        public void Eventos_contienen_la_plantilla_posterior_a_la_operacion()
        {
            var agg = NuevaLista();
            agg.ClearDomainEvents();
            var v0 = agg.Version;

            agg.RenombrarColumna(P(3), N("CANAL DIST"), "audit", new DateTimeOffset(2025,1,8,12,0,0,TimeSpan.Zero));

            var ev = UltimoEvento(agg);
            var col3 = ev.NuevaPlantilla.Obtener(P(3));
            Assert.That(col3.Nombre.Valor, Is.EqualTo("CANAL DIST"));
            Assert.That(ev.Version, Is.EqualTo(v0 + 1));
            Assert.That(ev.Usuario, Is.EqualTo("audit"));
        }
    }
}
