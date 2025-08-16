using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using SharedKernel.ValueObjects;

using ConfiguracionSistemaBC.Domain.Aggregates;
using ConfiguracionSistemaBC.Domain.ValueObjects;

namespace ConfiguracionSistemaBC.Tests.UnitTests.Aggregates
{
    [TestFixture]
    public class ConfiguracionEmpresaTests
    {
        // ------------------------- Helpers -------------------------
        private static DireccionPostal DirPrincipal() =>
            DireccionPostal.From(
                linea: "AV. PRINCIPAL 123",
                ubigeo: "150101",
                departamento: "LIMA",
                provincia: "LIMA",
                distrito: "LIMA"
            );

    private static Ruc UnRucPJ() => (Ruc)"20000000001"; // válido según el algoritmo del VO
    private static Ruc OtroRucPJ() => (Ruc)"20100070970"; // otro válido para actualización

        private static EmailEmpresa E(string s, bool visible = true) => EmailEmpresa.From(s, visible);

        // ------------------------- Registro y estado inicial -------------------------
        [Test]
        public void RegistrarNueva_InicializaEnPrueba_ConDatosLegalesYMoneda()
        {
            var ruc = UnRucPJ();
            var dir = DirPrincipal();

            var agg = ConfiguracionEmpresa.RegistrarNueva(
                tenantId: Guid.NewGuid(),
                ruc: ruc,
                razonSocial: "ACME S.A.C.",
                direccionFiscal: dir,
                monedaBase: Moneda.PEN()
            );

            Assert.That(agg.Ambiente, Is.EqualTo(AmbienteFe.PRUEBA));
            Assert.That(agg.Ruc, Is.EqualTo(ruc));
            Assert.That(agg.RazonSocial, Is.EqualTo("ACME S.A.C."));
            Assert.That(agg.DireccionFiscal, Is.EqualTo(dir));
            Assert.That(agg.MonedaBase, Is.EqualTo(Moneda.PEN()));
        }

        // ------------------------- Ambiente PRUEBA/PRODUCCIÓN -------------------------
        [Test]
        public void CambioDeAmbiente_PruebaAProduccion_Permitido_Y_NoReversible()
        {
            var agg = ConfiguracionEmpresa.RegistrarNueva(Guid.NewGuid(), UnRucPJ(), "ACME S.A.C.", DirPrincipal(), Moneda.PEN());

            // PRUEBA -> PRODUCCION
            agg.CambiarAmbiente(AmbienteFe.PRODUCCION);
            Assert.That(agg.Ambiente, Is.EqualTo(AmbienteFe.PRODUCCION));

            // PRODUCCION -> PRUEBA (debe fallar)
            var ex = Assert.Throws<InvalidOperationException>(() =>
                agg.CambiarAmbiente(AmbienteFe.PRUEBA));
            Assert.That(ex!.Message, Does.Contain("No es posible volver a PRUEBA"));
        }

        // ------------------------- Datos legales básicos -------------------------
        [Test]
        public void ActualizarDatosLegales_ModificaRuc_RazonSocial_Y_Direccion()
        {
            var agg = ConfiguracionEmpresa.RegistrarNueva(Guid.NewGuid(), UnRucPJ(), "ACME S.A.C.", DirPrincipal(), Moneda.PEN());

            var nuevoRuc = OtroRucPJ();
            var nuevaDir = DireccionPostal.From("JR. LIMA 456", "150101", "LIMA", "LIMA", "LIMA");
            agg.ActualizarDatosLegales(nuevoRuc, "ACME RENOVADA S.A.C.", nuevaDir, nombreComercial: "ACME");

            Assert.That(agg.Ruc, Is.EqualTo(nuevoRuc));
            Assert.That(agg.RazonSocial, Is.EqualTo("ACME RENOVADA S.A.C."));
            Assert.That(agg.NombreComercial, Is.EqualTo("ACME"));
            Assert.That(agg.DireccionFiscal, Is.EqualTo(nuevaDir));
        }

        // ------------------------- Establecimientos -------------------------
        [Test]
        public void Establecimientos_CRUD_Basico_Y_UnicidadCodigo()
        {
            var agg = ConfiguracionEmpresa.RegistrarNueva(Guid.NewGuid(), UnRucPJ(), "ACME", DirPrincipal(), Moneda.PEN());

            var id1 = agg.RegistrarEstablecimiento("001", "TIENDA CENTRAL", DireccionPostal.From("AV. UNO 999", "150101", "LIMA", "LIMA", "LIMA"));
            var id2 = agg.RegistrarEstablecimiento("002", "TIENDA SUR", DireccionPostal.From("AV. DOS 100", "150101", "LIMA", "LIMA", "LIMA"));

            // Unicidad por código
            Assert.Throws<InvalidOperationException>(() =>
                agg.RegistrarEstablecimiento("001", "DUPLICADO", DireccionPostal.From("CALLE X", "150101", "LIMA", "LIMA", "LIMA")));

            // Lectura
            var e1 = agg.BuscarEstablecimientoPorCodigo("001");
            Assert.That(e1, Is.Not.Null);
            Assert.That(e1!.Nombre, Is.EqualTo("TIENDA CENTRAL"));

            // Actualizar
            agg.ActualizarEstablecimiento(id1, "TIENDA CENTRAL - EDIT", DireccionPostal.From("AV. UNO EDIT 1000", "150101", "LIMA", "LIMA", "LIMA"));
            var e1b = agg.BuscarEstablecimientoPorCodigo("001");
            Assert.That(e1b!.Nombre, Is.EqualTo("TIENDA CENTRAL - EDIT"));

            // Deshabilitar
            agg.DeshabilitarEstablecimiento(id2);
            var e2 = agg.BuscarEstablecimientoPorCodigo("002");
            Assert.That(e2!.Habilitado, Is.False);

            // Listado
            var list = agg.ListarEstablecimientos();
            Assert.That(list.Count, Is.EqualTo(2));
        }

        // ------------------------- Series: agregar / listar / default -------------------------
        [Test]
        public void Series_Agregar_Validaciones_Unicidad_Y_DefaultPorTipo()
        {
            var agg = ConfiguracionEmpresa.RegistrarNueva(Guid.NewGuid(), UnRucPJ(), "ACME", DirPrincipal(), Moneda.PEN());
            var est = agg.RegistrarEstablecimiento("001", "TIENDA", DireccionPostal.From("AV. UNO 1", "150101", "LIMA", "LIMA", "LIMA"));

            // Serie válida para FACTURA (Fxxx)
            var serieF1 = SerieCodigo.ForTipo("F001", TipoComprobanteCodigo.Factura);
            var serieF2 = SerieCodigo.ForTipo("F002", TipoComprobanteCodigo.Factura);
            var serieB1 = SerieCodigo.ForTipo("B001", TipoComprobanteCodigo.Boleta);

            var s1 = agg.AgregarSerie(TipoComprobanteCodigo.Factura, serieF1, est, correlativoInicial: Correlativo.From(1), esPorDefecto: true);
            var s2 = agg.AgregarSerie(TipoComprobanteCodigo.Factura, serieF2, est, correlativoInicial: Correlativo.From(200));
            var s3 = agg.AgregarSerie(TipoComprobanteCodigo.Boleta,  serieB1, est, correlativoInicial: Correlativo.From(1), esPorDefecto: true);

            // Unicidad (tipo+serie)
            Assert.Throws<InvalidOperationException>(() =>
                agg.AgregarSerie(TipoComprobanteCodigo.Factura, serieF1, est, Correlativo.From(1)));

            // Default por tipo
            Assert.That(agg.ObtenerSeriePorDefecto(TipoComprobanteCodigo.Factura)!.Serie, Is.EqualTo("F001"));
            Assert.That(agg.ObtenerSeriePorDefecto(TipoComprobanteCodigo.Boleta)!.Serie, Is.EqualTo("B001"));

            // Listado por tipo
            var facturas = agg.ListarSeriesPorTipo(TipoComprobanteCodigo.Factura);
            Assert.That(facturas.Count, Is.EqualTo(2));
            var boletas = agg.ListarSeriesPorTipo(TipoComprobanteCodigo.Boleta);
            Assert.That(boletas.Count, Is.EqualTo(1));
        }

        [Test]
        public void Series_PrefijoDebeCoincidirConTipo()
        {
            var agg = ConfiguracionEmpresa.RegistrarNueva(Guid.NewGuid(), UnRucPJ(), "ACME", DirPrincipal(), Moneda.PEN());
            var est = agg.RegistrarEstablecimiento("001", "TIENDA", DireccionPostal.From("AV. UNO 1", "150101", "LIMA", "LIMA", "LIMA"));

            // Intentar Boleta con prefijo de Factura
            Assert.Throws<ArgumentException>(() =>
                agg.AgregarSerie(TipoComprobanteCodigo.Boleta,
                    SerieCodigo.ForTipo("F001", TipoComprobanteCodigo.Factura), // validada para FACTURA, no para Boleta
                    est,
                    Correlativo.From(1)));
        }

        // ------------------------- Series: actualizar -------------------------
        [Test]
        public void Series_Actualizar_PuedeCambiarSerie_Establecimiento_TipoOperacion_Y_Default_SiNoBloqueada()
        {
            var agg = ConfiguracionEmpresa.RegistrarNueva(Guid.NewGuid(), UnRucPJ(), "ACME", DirPrincipal(), Moneda.PEN());
            var est1 = agg.RegistrarEstablecimiento("001", "TIENDA 1", DireccionPostal.From("AV. 1", "150101", "LIMA", "LIMA", "LIMA"));
            var est2 = agg.RegistrarEstablecimiento("002", "TIENDA 2", DireccionPostal.From("AV. 2", "150101", "LIMA", "LIMA", "LIMA"));

            var id = agg.AgregarSerie(TipoComprobanteCodigo.Factura, SerieCodigo.ForTipo("F001", TipoComprobanteCodigo.Factura), est1, Correlativo.From(1), esPorDefecto: true);

            // Cambiar a F010, mover a est2, cambiar tipo de operación y quitar default
            agg.ActualizarSerie(id,
                nuevaSerie: SerieCodigo.ForTipo("F010", TipoComprobanteCodigo.Factura),
                nuevoEstablecimientoId: est2,
                nuevoTipoOperacion: TipoOperacion.ExportacionBienes,
                esPorDefecto: false);

            var info = agg.ObtenerSeriePorId(id);
            Assert.That(info!.Serie, Is.EqualTo("F010"));
            Assert.That(info.EstablecimientoId, Is.EqualTo(est2));
            Assert.That(info.TipoOperacion, Is.EqualTo(TipoOperacion.ExportacionBienes));
            Assert.That(agg.ObtenerSeriePorDefecto(TipoComprobanteCodigo.Factura), Is.Null);
        }

        [Test]
        public void Series_Actualizar_NoPermiteDuplicarNiUsarEstablecimientoDeshabilitado()
        {
            var agg = ConfiguracionEmpresa.RegistrarNueva(Guid.NewGuid(), UnRucPJ(), "ACME", DirPrincipal(), Moneda.PEN());
            var est1 = agg.RegistrarEstablecimiento("001", "TIENDA 1", DireccionPostal.From("AV. 1", "150101", "LIMA", "LIMA", "LIMA"));
            var est2 = agg.RegistrarEstablecimiento("002", "TIENDA 2", DireccionPostal.From("AV. 2", "150101", "LIMA", "LIMA", "LIMA"));

            var a = agg.AgregarSerie(TipoComprobanteCodigo.Factura, SerieCodigo.ForTipo("F001", TipoComprobanteCodigo.Factura), est1, Correlativo.From(1));
            var b = agg.AgregarSerie(TipoComprobanteCodigo.Factura, SerieCodigo.ForTipo("F002", TipoComprobanteCodigo.Factura), est1, Correlativo.From(1));

            // No duplicar serie (tipo+serie)
            Assert.Throws<InvalidOperationException>(() =>
                agg.ActualizarSerie(b, nuevaSerie: SerieCodigo.ForTipo("F001", TipoComprobanteCodigo.Factura)));

            // Deshabilitar establecimiento y tratar de mover allí
            agg.DeshabilitarEstablecimiento(est2);
            Assert.Throws<InvalidOperationException>(() =>
                agg.ActualizarSerie(a, nuevoEstablecimientoId: est2));
        }

        // ------------------------- Series: bloqueo por uso -------------------------
        [Test]
        public void Series_BloqueoPorUso_ImpideActualizarYEliminar()
        {
            var agg = ConfiguracionEmpresa.RegistrarNueva(Guid.NewGuid(), UnRucPJ(), "ACME", DirPrincipal(), Moneda.PEN());
            var est = agg.RegistrarEstablecimiento("001", "TIENDA", DireccionPostal.From("AV. UNO 1", "150101", "LIMA", "LIMA", "LIMA"));

            var id = agg.AgregarSerie(TipoComprobanteCodigo.Factura, SerieCodigo.ForTipo("F001", TipoComprobanteCodigo.Factura), est, Correlativo.From(1));

            // Bloqueo (simula que ya se emitió un documento)
            agg.BloquearSeriePorUso(id);

            Assert.Throws<InvalidOperationException>(() =>
                agg.ActualizarSerie(id, nuevaSerie: SerieCodigo.ForTipo("F010", TipoComprobanteCodigo.Factura)));

            Assert.Throws<InvalidOperationException>(() =>
                agg.EliminarSerie(id));
        }

        [Test]
        public void Series_Eliminar_PermitidoSiNoBloqueada_QuitaDefaultSiEraDefault()
        {
            var agg = ConfiguracionEmpresa.RegistrarNueva(Guid.NewGuid(), UnRucPJ(), "ACME", DirPrincipal(), Moneda.PEN());
            var est = agg.RegistrarEstablecimiento("001", "TIENDA", DireccionPostal.From("AV. UNO 1", "150101", "LIMA", "LIMA", "LIMA"));

            var id = agg.AgregarSerie(TipoComprobanteCodigo.Factura, SerieCodigo.ForTipo("F001", TipoComprobanteCodigo.Factura), est, Correlativo.From(1), esPorDefecto: true);

            Assert.That(agg.ObtenerSeriePorDefecto(TipoComprobanteCodigo.Factura), Is.Not.Null);

            agg.EliminarSerie(id);

            Assert.That(agg.ObtenerSeriePorId(id), Is.Null);
            Assert.That(agg.ObtenerSeriePorDefecto(TipoComprobanteCodigo.Factura), Is.Null);
        }

        // ------------------------- Campos opcionales: email, teléfono, pie, logo, moneda -------------------------
        [Test]
        public void CamposOpcionales_SePuedenActualizarYPersisten()
        {
            var agg = ConfiguracionEmpresa.RegistrarNueva(Guid.NewGuid(), UnRucPJ(), "ACME", DirPrincipal(), Moneda.PEN());

            agg.ReemplazarEmails(new[] { E("ventas@acme.com"), E("info@acme.com", visible: false) });
            agg.ReemplazarTelefonos(Telefono.FromTexto("+51 999 888 777 / (01) 234 5678"));
            agg.ActualizarPieDePagina(PieDePagina.FromTextoPlano("Gracias por su preferencia."));
            agg.EstablecerLogo(LogoImagen.FromUpload("logo.png", "image/png", bytesLength: 50_000, anchoPx: 300, altoPx: 100));
            agg.CambiarMonedaBase(Moneda.USD());

            Assert.That(agg.Emails.Count, Is.EqualTo(2));
            Assert.That(agg.Telefonos.EsVacio, Is.False);
            Assert.That(agg.PieDePagina.EsVacio, Is.False);
            Assert.That(agg.Logo, Is.Not.Null);
            Assert.That(agg.MonedaBase, Is.EqualTo(Moneda.USD()));
        }
    }
}