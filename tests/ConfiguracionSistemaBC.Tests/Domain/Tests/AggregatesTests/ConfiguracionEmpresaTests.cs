using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ConfiguracionSistemaBC.Domain.Aggregates;
using ConfiguracionSistemaBC.Domain.ValueObjects;
using ConfiguracionSistemaBC.Domain.Events;
using SharedKernel.ValueObjects;

namespace ConfiguracionSistemaBC.Tests.Domain.Aggregates
{
    [TestFixture]
    public class ConfiguracionEmpresaTests
    {
        // ---------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------
        private static ConfiguracionEmpresa NuevaEmpresa()
        {
            var ruc = Ruc.From("20600893409");
            var dir = DomicilioFiscal.From(
                paisCodigoIso: "PE",
                departamento: "LIMA",
                provincia: "LIMA",
                distrito: "MIRAFLORES",
                linea: "Av. X 123",
                ubigeo: "150122"
            );
            return ConfiguracionEmpresa.RegistrarNueva(ruc, "EMPRESA S.A.C.", dir, Moneda.PEN());
        }

        private static SerieCodigo FE(string code) => SerieCodigo.From(code);
        private static SerieCodigo BE(string code) => SerieCodigo.From(code);

        // ---------------------------------------------------------------------
        // RegistrarNueva / Bootstrap básicos
        // ---------------------------------------------------------------------

        [Test]
        public void RegistrarNueva_inicializa_defaults_y_bootstrap_minimo()
        {
            var agg = NuevaEmpresa();

            Assert.Multiple(() =>
            {
                // Identidad y legales
                Assert.That(agg.Ruc, Is.Not.Null);
                Assert.That(agg.EmpresaId, Is.Not.Null);
                Assert.That(agg.RazonSocial, Is.EqualTo("EMPRESA S.A.C."));

                // Ambiente y moneda
                Assert.That(agg.Ambiente, Is.EqualTo(AmbienteFe.PRUEBA));
                Assert.That(agg.MonedaBase, Is.EqualTo(Moneda.PEN()));

                // Establecimiento principal
                var princ = agg.ObtenerEstablecimientoPrincipal();
                Assert.That(princ, Is.Not.Null);
                Assert.That(princ!.Codigo, Is.EqualTo("01"));

                // Series por defecto por tipo (Factura / Boleta)
                var defFac = agg.ObtenerSeriePorDefecto(TipoComprobanteCodigo.Factura);
                var defBol = agg.ObtenerSeriePorDefecto(TipoComprobanteCodigo.Boleta);
                Assert.That(defFac, Is.Not.Null);
                Assert.That(defBol, Is.Not.Null);
                Assert.That(defFac!.Serie, Is.EqualTo("FE01"));
                Assert.That(defBol!.Serie, Is.EqualTo("BE01"));

                // Forma de pago por defecto = Contado
                var fpDef = agg.ObtenerFormaDePagoPorDefecto();
                Assert.That(fpDef, Is.Not.Null);
                Assert.That(fpDef!.Valor.EsContado, Is.True);
                Assert.That(fpDef.Nombre, Is.EqualTo("Contado"));

                // Unidad por defecto = NIU (UNIDAD)
                var umDef = agg.ObtenerUnidadDeMedidaPorDefecto();
                Assert.That(umDef, Is.Not.Null);
                Assert.That(umDef!.Unidad.Codigo, Is.EqualTo("NIU"));
                Assert.That(umDef.Nombre, Is.EqualTo("UNIDAD"));

                // Se emite evento de configuración registrada
                Assert.That(agg.DomainEvents.Any(e => e is ConfiguracionEmpresaRegistrada), Is.True);
            });
        }

        // ---------------------------------------------------------------------
        // Cambios de ambiente
        // ---------------------------------------------------------------------

        [Test]
        public void CambiarAmbiente_emite_evento_y_actualiza_estado()
        {
            var agg = NuevaEmpresa();
            agg.ClearDomainEvents();
            var v0 = agg.Version;

            agg.CambiarAmbiente(AmbienteFe.PRODUCCION);

            Assert.Multiple(() =>
            {
                Assert.That(agg.Ambiente, Is.EqualTo(AmbienteFe.PRODUCCION));
                Assert.That(agg.Version, Is.GreaterThan(v0));
                Assert.That(agg.DomainEvents.OfType<AmbienteCambiado>().Count(), Is.EqualTo(1));
            });
        }

        // ---------------------------------------------------------------------
        // Datos legales / preferencias
        // ---------------------------------------------------------------------

        [Test]
        public void ActualizarDatosLegales_actualiza_y_emite_evento()
        {
            var agg = NuevaEmpresa();
            agg.ClearDomainEvents();
            var dir = DomicilioFiscal.From(
                paisCodigoIso: "PE",
                departamento: "CUSCO",
                provincia: "CUSCO",
                distrito: "WANCHAQ",
                linea: "Jr. Q 456",
                ubigeo: "080101"
            );

            agg.ActualizarDatosLegales(Ruc.From("20600893409"), "OTRA EMPRESA S.A.", dir, "OTRA");

            Assert.Multiple(() =>
            {
                Assert.That(agg.Ruc.Canonizado, Is.EqualTo("20600893409"));
                Assert.That(agg.RazonSocial, Is.EqualTo("OTRA EMPRESA S.A."));
                Assert.That(agg.NombreComercial, Is.EqualTo("OTRA"));
                Assert.That(agg.DireccionFiscal, Is.EqualTo(dir));
                Assert.That(agg.DomainEvents.OfType<ConfiguracionEmpresaActualizada>().Any(), Is.True);
            });
        }

        [Test]
        public void Preferencias_varias_incrementan_version()
        {
            var agg = NuevaEmpresa();
            var v0 = agg.Version;

            agg.ReemplazarTelefono(Telefono.FromTexto("+51 987 654 321"));
            agg.ReemplazarEmails(new[] { Email.Create("a@acme.com"), Email.Create("b@acme.com") });
            agg.ActualizarPieDePagina(PieDePagina.FromTextoPlano("Gracias por su preferencia"));
            agg.EstablecerLogo(null);
            agg.CambiarMonedaBase(Moneda.USD());
            agg.ConfigurarMostrarImagenEnComprobanteImpresa(true);

            Assert.That(agg.Version, Is.GreaterThan(v0));
        }

        // ---------------------------------------------------------------------
        // Establecimientos
        // ---------------------------------------------------------------------

        [Test]
        public void Establecimientos_registro_recodificacion_busqueda_actualizacion_y_eliminacion()
        {
            var agg = NuevaEmpresa();

            // Registrar otro
            var dir2 = DomicilioFiscal.From(
                paisCodigoIso: "PE",
                departamento: "AREQUIPA",
                provincia: "AREQUIPA",
                distrito: "YANAHUARA",
                linea: "Av. Z 789",
                ubigeo: "040101"
            );
            var id2 = agg.RegistrarEstablecimiento("02", "Sucursal AQP", dir2);

            // Búsqueda por código
            var e2 = agg.BuscarEstablecimientoPorCodigo("02");
            Assert.Multiple(() =>
            {
                Assert.That(e2, Is.Not.Null);
                Assert.That(e2!.Nombre, Is.EqualTo("Sucursal AQP"));
            });

            // Recodificar y validar unicidad
            agg.RecodificarEstablecimiento(id2, "03");
            Assert.That(agg.BuscarEstablecimientoPorCodigo("03"), Is.Not.Null);
            Assert.That(() => agg.RecodificarEstablecimiento(id2, "01"), // ya existe principal con 01
                Throws.TypeOf<InvalidOperationException>());

            // Actualizar datos
            var dir3 = DomicilioFiscal.From(
                paisCodigoIso: "PE",
                departamento: "AREQUIPA",
                provincia: "AREQUIPA",
                distrito: "YANAHUARA",
                linea: "Av. Z 999",
                ubigeo: "040101"
            );
            agg.ActualizarEstablecimiento(id2, "Sucursal AQP Centro", dir3, Telefono.FromTexto("054-111111"), Email.Create("a@b.com"));

            var e3 = agg.ListarEstablecimientos().Single(x => x.Id == id2);
            Assert.Multiple(() =>
            {
                Assert.That(e3.Nombre, Is.EqualTo("Sucursal AQP Centro"));
                Assert.That(e3.Direccion, Is.EqualTo(dir3));
            });

            // Eliminar: no puede dejar a la empresa sin establecimientos
            // (ya hay al menos el principal, así que se permite eliminar id2)
            agg.EliminarEstablecimiento(id2);
            Assert.That(agg.ListarEstablecimientos().Any(x => x.Id == id2), Is.False);
        }

        [Test]
        public void EliminarEstablecimiento_no_permite_si_queda_sin_establecimientos()
        {
            var agg = NuevaEmpresa();

            // Intentar borrar el único (primero crea otro y borra el otro, luego intenta borrar el último)
            var dir2 = DomicilioFiscal.From(
                paisCodigoIso: "PE",
                departamento: "PIURA",
                provincia: "PIURA",
                distrito: "CASTILLA",
                linea: "Mz A Lt 1",
                ubigeo: "200101"
            );
            var id2 = agg.RegistrarEstablecimiento("02", "Sucursal Piura", dir2);

            // Agrego serie en sucursal 02, la bloqueo y luego intento eliminar el establecimiento
            var serieId = agg.AgregarSerie(TipoComprobanteCodigo.Factura, FE("FE02"), id2, Correlativo.From(1));
            agg.BloquearSeriePorUso(serieId);
            // Ahora se permite eliminar el establecimiento aunque tenga series usadas (bloqueadas)
            Assert.DoesNotThrow(() => agg.EliminarEstablecimiento(id2));

            // Ahora elimino serie 02 (no se puede por bloqueada), muevo default y elimino sucursal 02 restaurando estado:
            // Ya no existe la serie, no se debe intentar eliminar nuevamente.

            // Sigo: elimino sucursal 02 no procede; elimino entonces la serie no (bloqueada), así que dejo sucursal 02 y
            // pruebo que no me deje eliminar el único quedando sin ninguno:
            // (elimino sucursal 02 primero para tener solo el principal y verificar restricción)
            // Desbloquear no existe en dominio; simplemente pruebo la regla de "al menos uno":
            // Borro la sucursal 02 creando antes una nueva 03 sin series para poder borrar 02:
            var id3 = agg.RegistrarEstablecimiento("03", "Temporal", dir2);
            // Borrar 03 (ok)
            agg.EliminarEstablecimiento(id3);
            // Ahora solo queda el principal, intentar eliminarlo (debe lanzar excepción)
            var principal = agg.ObtenerEstablecimientoPrincipal()!;
            Assert.That(agg.ListarEstablecimientos().Count, Is.EqualTo(1));
            Assert.That(() => agg.EliminarEstablecimiento(principal.Id), Throws.TypeOf<InvalidOperationException>());
        }

        // ---------------------------------------------------------------------
        // Series
        // ---------------------------------------------------------------------

        [Test]
        public void Series_agregar_actualizar_default_bloqueo_y_eliminar_con_restricciones()
        {
            var agg = NuevaEmpresa();

            var princ = agg.ObtenerEstablecimientoPrincipal()!;
            var serieId = agg.AgregarSerie(TipoComprobanteCodigo.Factura, FE("FE02"), princ.Id, Correlativo.From(100));

            // Evento SerieAgregada
            Assert.That(agg.DomainEvents.OfType<SerieAgregada>().Any(e => e is SerieAgregada), Is.True);
            agg.ClearDomainEvents();

            // Duplicado por (tipo, serie) => error
            Assert.That(() => agg.AgregarSerie(TipoComprobanteCodigo.Factura, FE("FE02"), princ.Id, Correlativo.From(1)),
                Throws.TypeOf<InvalidOperationException>());

            // Prefijo inválido vs tipo => debe lanzar (BExx con Factura, por ejemplo)
            Assert.That(() => agg.AgregarSerie(TipoComprobanteCodigo.Factura, BE("BE99"), princ.Id, Correlativo.From(1)),
                Throws.Exception);

            // Cambiar serie y marcar como default
            agg.ActualizarSerie(serieId, nuevaSerie: FE("FE20"), esPorDefecto: true);
            var def = agg.ObtenerSeriePorDefecto(TipoComprobanteCodigo.Factura);
            Assert.That(def!.Id, Is.EqualTo(serieId));

            // Bloquear por uso => no permite más actualizaciones ni eliminación
            agg.BloquearSeriePorUso(serieId);
            Assert.That(() => agg.ActualizarSerie(serieId, nuevaSerie: FE("FE21")), Throws.TypeOf<InvalidOperationException>());
            Assert.That(() => agg.EliminarSerie(serieId), Throws.TypeOf<InvalidOperationException>());
        }

        // ---------------------------------------------------------------------
        // Formas de pago
        // ---------------------------------------------------------------------

        [Test]
        public void FormasPago_bootstrap_listado_y_default()
        {
            var agg = NuevaEmpresa();

            var lista = agg.ListarFormasDePago();
            Assert.Multiple(() =>
            {
                // Default Contado
                var def = agg.ObtenerFormaDePagoPorDefecto();
                Assert.That(def, Is.Not.Null);
                Assert.That(def!.Valor.EsContado, Is.True);
                Assert.That(def.Nombre, Is.EqualTo("Contado"));

                // Algunas visibles del sistema
                Assert.That(lista.Any(x => x.Nombre == "Efectivo" && x.EsSistema && x.Visible), Is.True);
                Assert.That(lista.Any(x => x.Nombre == "Tarjeta" && x.EsSistema && x.Visible), Is.True);

                // Orden ascendente
                Assert.That(lista.Select(x => x.Orden).ToArray(), Is.Ordered.Ascending);
            });
        }

        [Test]
        public void FormasPago_personalizadas_crud_restricciones_y_default()
        {
            var agg = NuevaEmpresa();

            // Crear personalizada
            var id = agg.AgregarFormaDePagoPersonalizada(FormaDePago.ContadoPredefinido("BCP", "BCP"), "BCP Caja", visible: true, orden: 999);

            var creada = agg.ListarFormasDePago().Single(x => x.Id == id);
            Assert.Multiple(() =>
            {
                Assert.That(creada.EsSistema, Is.False);
                Assert.That(creada.Visible, Is.True);
                Assert.That(creada.Orden, Is.EqualTo(999));
            });

            // Editar (permitido en personalizadas)
            agg.ActualizarFormaDePago(id,
                nuevoValor: FormaDePago.ContadoPredefinido("INTERBANK", "INTERBANK"),
                nuevoNombre: "Interbank Caja",
                nuevoOrden: 500);

            var editada = agg.ListarFormasDePago().Single(x => x.Id == id);
            Assert.Multiple(() =>
            {
                Assert.That(editada.Nombre, Is.EqualTo("Interbank Caja"));
                Assert.That(editada.Valor.MetodoCodigo, Is.EqualTo("INTERBANK"));
                Assert.That(editada.Orden, Is.EqualTo(500));
            });

            // Poner como default
            agg.EstablecerFormaPagoPorDefecto(id);
            var def = agg.ObtenerFormaDePagoPorDefecto();
            Assert.That(def!.Id, Is.EqualTo(id));

            // No se puede eliminar la default
            Assert.That(() => agg.EliminarFormaDePago(id), Throws.TypeOf<InvalidOperationException>());

            // Cambiar default a Efectivo y eliminar la personalizada
            var efectivo = agg.ListarFormasDePago().First(x => x.Nombre == "Efectivo");
            agg.EstablecerFormaPagoPorDefecto(efectivo.Id);
            agg.EliminarFormaDePago(id);
            Assert.That(agg.ListarFormasDePago().Any(x => x.Id == id), Is.False);
        }

        [Test]
        public void FormasPago_restricciones_sobre_sistema_visibilidad_y_unicidad()
        {
            var agg = NuevaEmpresa();

            var sistema = agg.ListarFormasDePago().First(x => x.EsSistema && !x.EsPorDefecto);

            // No puedo cambiar nombre ni valor en sistema
            Assert.That(() => agg.ActualizarFormaDePago(sistema.Id, nuevoNombre: "Otro"),
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(() => agg.ActualizarFormaDePago(sistema.Id, nuevoValor: FormaDePago.Credito()),
                Throws.TypeOf<InvalidOperationException>());

            // Visibilidad y orden sí
            agg.ActualizarFormaDePago(sistema.Id, visible: false, nuevoOrden: sistema.Orden + 10);
            var rec = agg.ListarFormasDePago().First(x => x.Id == sistema.Id);
            Assert.Multiple(() =>
            {
                Assert.That(rec.Visible, Is.False);
                Assert.That(rec.Orden, Is.EqualTo(sistema.Orden + 10));
            });

            // Unicidad por (code|metodo|nombre)
            agg.AgregarFormaDePagoPersonalizada(FormaDePago.ContadoYape(), "Yape Caja");
            Assert.That(() => agg.AgregarFormaDePagoPersonalizada(FormaDePago.ContadoYape(), "Yape Caja"),
                Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void FormasPago_no_puedo_ocultar_default_ni_definir_default_oculta()
        {
            var agg = NuevaEmpresa();

            var plin = agg.ListarFormasDePago().First(x => x.Nombre == "Plin");
            agg.ActualizarFormaDePago(plin.Id, visible: false);
            Assert.That(() => agg.EstablecerFormaPagoPorDefecto(plin.Id),
                Throws.TypeOf<InvalidOperationException>());

            var def = agg.ObtenerFormaDePagoPorDefecto()!;
            Assert.That(() => agg.ActualizarFormaDePago(def.Id, visible: false),
                Throws.TypeOf<InvalidOperationException>());
        }

        // ---------------------------------------------------------------------
        // Unidades de medida
        // ---------------------------------------------------------------------

        [Test]
        public void Unidades_bootstrap_listado_y_default()
        {
            var agg = NuevaEmpresa();

            var lista = agg.ListarUnidadesDeMedida();
            Assert.Multiple(() =>
            {
                var def = agg.ObtenerUnidadDeMedidaPorDefecto();
                Assert.That(def, Is.Not.Null);
                Assert.That(def!.Unidad.Codigo, Is.EqualTo("NIU"));
                Assert.That(def.Nombre, Is.EqualTo("UNIDAD"));

                // Algunas visibles del sistema
                Assert.That(lista.Any(x => x.Unidad.Codigo == "KGM" && x.EsSistema && x.Visible), Is.True);
                Assert.That(lista.Any(x => x.Unidad.Codigo == "ZZ"  && x.EsSistema && x.Visible), Is.True);

                // Orden ascendente
                Assert.That(lista.Select(x => x.Orden).ToArray(), Is.Ordered.Ascending);
            });
        }

        [Test]
        public void Unidades_personalizadas_crud_restricciones_y_default()
        {
            var agg = NuevaEmpresa();

            // Crear personalizada
            var id = agg.AgregarUnidadDeMedidaPersonalizada(UnidadDeMedida.From("CAJA"), "CAJA", visible: true, orden: 900);

            var creada = agg.ListarUnidadesDeMedida().Single(x => x.Id == id);
            Assert.Multiple(() =>
            {
                Assert.That(creada.EsSistema, Is.False);
                Assert.That(creada.Visible, Is.True);
                Assert.That(creada.Unidad.Codigo, Is.EqualTo("CAJA"));
                Assert.That(creada.Orden, Is.EqualTo(900));
            });

            // Editar (permitido en personalizadas)
            agg.ActualizarUnidadDeMedida(id, nuevaUnidad: UnidadDeMedida.From("C62"), nuevoNombre: "PIEZA", nuevoOrden: 500);
            var editada = agg.ListarUnidadesDeMedida().Single(x => x.Id == id);
            Assert.Multiple(() =>
            {
                Assert.That(editada.Unidad.Codigo, Is.EqualTo("C62"));
                Assert.That(editada.Nombre, Is.EqualTo("PIEZA"));
                Assert.That(editada.Orden, Is.EqualTo(500));
            });

            // Poner default y validar restricción de eliminación
            agg.EstablecerUnidadDeMedidaPorDefecto(id);
            var def = agg.ObtenerUnidadDeMedidaPorDefecto();
            Assert.That(def!.Id, Is.EqualTo(id));
            Assert.That(() => agg.EliminarUnidadDeMedida(id), Throws.TypeOf<InvalidOperationException>());

            // Cambiar default y eliminar
            var niu = agg.ListarUnidadesDeMedida().First(x => x.Unidad.Codigo == "NIU");
            agg.EstablecerUnidadDeMedidaPorDefecto(niu.Id);
            agg.EliminarUnidadDeMedida(id);
            Assert.That(agg.ListarUnidadesDeMedida().Any(x => x.Id == id), Is.False);
        }

        [Test]
        public void Unidades_restricciones_sobre_sistema_visibilidad_y_unicidad()
        {
            var agg = NuevaEmpresa();

            // No duplicar código existente
            Assert.That(() => agg.AgregarUnidadDeMedidaPersonalizada(UnidadDeMedida.From("NIU"), "UNIDAD X"),
                Throws.TypeOf<InvalidOperationException>());

            var sistema = agg.ListarUnidadesDeMedida().First(x => x.EsSistema && !x.EsPorDefecto);

            // No cambiar código ni nombre a sistema
            Assert.That(() => agg.ActualizarUnidadDeMedida(sistema.Id, nuevaUnidad: UnidadDeMedida.From("C62")),
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(() => agg.ActualizarUnidadDeMedida(sistema.Id, nuevoNombre: "OTRO"),
                Throws.TypeOf<InvalidOperationException>());

            // Visibilidad/orden sí
            agg.ActualizarUnidadDeMedida(sistema.Id, visible: false, nuevoOrden: sistema.Orden + 5);
            var rec = agg.ListarUnidadesDeMedida().First(x => x.Id == sistema.Id);
            Assert.Multiple(() =>
            {
                Assert.That(rec.Visible, Is.False);
                Assert.That(rec.Orden, Is.EqualTo(sistema.Orden + 5));
            });

            // No puedo hacer default una oculta
            Assert.That(() => agg.EstablecerUnidadDeMedidaPorDefecto(sistema.Id), Throws.TypeOf<InvalidOperationException>());
        }

        // ---------------------------------------------------------------------
        // Version y eventos (spot checks)
        // ---------------------------------------------------------------------

        [Test]
        public void Version_incrementa_en_mutaciones_relevantes_y_ClearDomainEvents_limpia()
        {
            var agg = NuevaEmpresa();
            var v0 = agg.Version;

            // Serie nueva
            var princ = agg.ObtenerEstablecimientoPrincipal()!;
            agg.AgregarSerie(TipoComprobanteCodigo.Factura, FE("FE10"), princ.Id, Correlativo.From(1));
            Assert.That(agg.Version, Is.GreaterThan(v0));
            Assert.That(agg.DomainEvents.Any(), Is.True);
            agg.ClearDomainEvents();
            Assert.That(agg.DomainEvents.Any(), Is.False);

            // Forma de pago personalizada
            var v1 = agg.Version;
            var fpId = agg.AgregarFormaDePagoPersonalizada(FormaDePago.ContadoBcp("BCP"), "BCP");
            Assert.That(agg.Version, Is.GreaterThan(v1));

            // Unidad personalizada
            var v2 = agg.Version;
            var umId = agg.AgregarUnidadDeMedidaPersonalizada(UnidadDeMedida.From("CJG"), "CAJA GRANDE");
            Assert.That(agg.Version, Is.GreaterThan(v2));

            // Ediciones
            var v3 = agg.Version;
            agg.ActualizarFormaDePago(fpId, nuevoNombre: "BCP Ventanilla");
            agg.ActualizarUnidadDeMedida(umId, nuevoNombre: "CAJA G.");
            Assert.That(agg.Version, Is.GreaterThan(v3));
        }
    }
}
