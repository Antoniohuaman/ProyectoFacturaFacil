using System;
using System.Linq;
using NUnit.Framework;
using ConfiguracionSistemaBC.Domain.Aggregates;
using ConfiguracionSistemaBC.Domain.Entities;
using ConfiguracionSistemaBC.Domain.ValueObjects;
using ConfiguracionSistemaBC.Domain.Events;

// Shared Kernel VOs
using SharedKernel.ValueObjects;

namespace ConfiguracionSistemaBC.Tests.Domain.Aggregates
{
    [TestFixture]
    public class ConfiguracionEmpresaTests
    {
        // ================= Helpers / Fixtures =================

    private static Ruc RucValido() => Ruc.From("20100070970"); // RUC válido SUNAT

        // Ajusta este helper si tu factoría de DomicilioFiscal tiene otro nombre/firma.
        private static DomicilioFiscal DfPeru(
            string ubigeo = "150101",
            string direccion = "AV. DEMO 123",
            string distrito = "LIMA",
            string provincia = "LIMA",
            string departamento = "LIMA")
        {
            return DomicilioFiscal.FromPeru(
                direccion,
                ubigeo,
                departamento,
                provincia,
                distrito,
                null
            );
        }

        private static Moneda PEN() => Moneda.PEN();

        private static ConfiguracionEmpresa NuevaEmpresaBaseline()
        {
            return ConfiguracionEmpresa.RegistrarNueva(
                ruc: RucValido(),
                razonSocial: "ACME S.A.C.",
                direccionFiscal: DfPeru(),
                monedaBase: PEN()
            );
        }

        // ================= Tests =================

        [Test]
        public void RegistrarNueva_CreaBootstrapBasicoYEvento()
        {
            var agg = NuevaEmpresaBaseline();

            // Identidad y datos base
            Assert.That(agg.EmpresaId, Is.Not.Null);
            Assert.That(agg.RazonSocial, Is.EqualTo("ACME S.A.C."));
            Assert.That(agg.MonedaBase, Is.EqualTo(PEN()));
            Assert.That(agg.Ambiente, Is.EqualTo(AmbienteFe.PRUEBA));

            // Establecimiento principal
            var principal = agg.ObtenerEstablecimientoPrincipal();
            Assert.That(principal, Is.Not.Null);
            Assert.That(principal!.Codigo, Is.EqualTo("01"));
            Assert.That(principal.Habilitado, Is.True);

            // Formas de pago (bootstrapped) → existe una default visible
            var fpDefault = agg.ObtenerFormaDePagoPorDefecto();
            Assert.That(fpDefault, Is.Not.Null);
            Assert.That(fpDefault!.Visible, Is.True);
            Assert.That(fpDefault.EsPorDefecto, Is.True);

            // Unidades de medida (bootstrapped) → NIU por defecto
            var umDefault = agg.ObtenerUnidadDeMedidaPorDefecto();
            Assert.That(umDefault, Is.Not.Null);
            Assert.That(umDefault!.Unidad, Is.EqualTo(UnidadDeMedida.NIU));
            Assert.That(umDefault.EsPorDefecto, Is.True);

            // Evento de dominio
            Assert.That(agg.DomainEvents.Any(e => e is ConfiguracionEmpresaRegistrada), Is.True);
        }

        [Test]
        public void CambiarAmbiente_TransicionValida_ActualizaVersionYEmiteEvento()
        {
            var agg = NuevaEmpresaBaseline();
            var version0 = agg.Version;

            agg.CambiarAmbiente(AmbienteFe.PRODUCCION);

            Assert.That(agg.Ambiente, Is.EqualTo(AmbienteFe.PRODUCCION));
            Assert.That(agg.Version, Is.GreaterThan(version0));
            Assert.That(agg.DomainEvents.Any(e => e is AmbienteCambiado), Is.True);
        }

        [Test]
        public void ActualizarDatosLegales_CambiaRucRazonSocialDireccion_EmiteEventoYVersiona()
        {
            var agg = NuevaEmpresaBaseline();
            var version0 = agg.Version;

            var nuevoRuc = Ruc.From("20100070970"); // Usar un RUC válido SUNAT
            var nuevaDir = DfPeru(direccion: "JR. NUEVA 456");

            agg.ActualizarDatosLegales(nuevoRuc, "ACME RENOVADA S.A.C.", nuevaDir, "ACME");

            Assert.That(agg.Ruc, Is.EqualTo(nuevoRuc));
            Assert.That(agg.RazonSocial, Is.EqualTo("ACME RENOVADA S.A.C."));
            Assert.That(agg.NombreComercial, Is.EqualTo("ACME"));
            Assert.That(agg.Version, Is.GreaterThan(version0));
            Assert.That(agg.DomainEvents.Any(e => e is ConfiguracionEmpresaActualizada), Is.True);
        }

        [Test]
        public void Preferencias_TelefonoEmailsPieLogoImagen_SeActualizanCorrectamente()
        {
            var agg = NuevaEmpresaBaseline();

            agg.ReemplazarTelefono(Telefono.FromTexto("+51 999 999 999"));
            agg.ReemplazarEmails(new[]
            {
                Email.Create("ventas@acme.test"),
                Email.Create("facturacion@acme.test")
            });
            agg.ActualizarPieDePagina(PieDePagina.FromTextoPlano("¡Gracias por su preferencia!"));
            agg.EstablecerLogo(null); // permitido (sin logo)
            agg.ConfigurarMostrarImagenEnComprobanteImpresa(true);

            Assert.That(agg.Telefono.ToString(), Is.EqualTo("+51 999 999 999"));
            Assert.That(agg.Emails.Count, Is.EqualTo(2));
            Assert.That(agg.PieDePagina, Is.Not.Null);
            Assert.That(agg.MostrarImagenEnComprobanteImpresa, Is.True);
        }

        [Test]
        public void Establecimientos_CRUD_BasicoYRestricciones()
        {
            var agg = NuevaEmpresaBaseline();

            // Registrar otro establecimiento
            var id2 = agg.RegistrarEstablecimiento("02", "Sucursal Centro", DfPeru(direccion: "AV. CENTRO 100"));
            var todos = agg.ListarEstablecimientos();
            Assert.That(todos.Any(e => e.Id == id2), Is.True);

            // Establecer como principal el nuevo
            agg.EstablecerComoPrincipal(id2);
            Assert.That(agg.ObtenerEstablecimientoPrincipal()!.Id, Is.EqualTo(id2));

            // Recodificar
            agg.RecodificarEstablecimiento(id2, "10");
            var buscado = agg.BuscarEstablecimientoPorCodigo("10");
            Assert.That(buscado, Is.Not.Null);
            Assert.That(buscado!.Codigo, Is.EqualTo("10"));

            // Actualizar datos
            agg.ActualizarEstablecimiento(id2, "Sucursal Central", DfPeru(direccion: "AV. CENTRAL 101"));
            var actualizado = agg.ListarEstablecimientos().First(x => x.Id == id2);
            Assert.That(actualizado.Nombre, Is.EqualTo("Sucursal Central"));

            // Evento de dominio por registro de establecimiento
            Assert.That(agg.DomainEvents.Any(e => e is EstablecimientoRegistrado), Is.True);

            // Eliminar uno (deben quedar al menos 1)
            var principal = agg.ObtenerEstablecimientoPrincipal()!;
            var otroId = agg.ListarEstablecimientos().First(e => e.Id != principal.Id).Id;
            agg.EliminarEstablecimiento(otroId);

            // Ahora debe poder eliminar el último establecimiento sin excepción
            agg.EliminarEstablecimiento(principal.Id);
            Assert.That(agg.ListarEstablecimientos().Count, Is.EqualTo(0));
        }

        [Test]
        public void FormasDePago_ReglasDeSistema_Default_Visibilidad_Personalizadas()
        {
            var agg = NuevaEmpresaBaseline();

            var todas = agg.ListarFormasDePago();
            Assert.That(todas.Count, Is.GreaterThanOrEqualTo(3));

            var contado = todas.First(fp => fp.Nombre.ToUpperInvariant().Contains("CONTADO"));
            var efectivo = todas.First(fp => fp.Nombre.ToUpperInvariant().Contains("EFECTIVO"));

            // 1) No se puede editar nombre/valor de una FP del sistema
            Assert.That(() => agg.ActualizarFormaDePago(contado.Id, nuevoNombre: "Contado X"),
                Throws.TypeOf<InvalidOperationException>());

            // 2) No se puede ocultar la default actual
            Assert.That(contado.EsPorDefecto, Is.True);
            Assert.That(() => agg.ActualizarFormaDePago(contado.Id, visible: false),
                Throws.TypeOf<InvalidOperationException>());

            // 3) Puedo marcar otra como default (la default anterior se desmarca)
            agg.EstablecerFormaPagoPorDefecto(efectivo.Id);
            var def = agg.ObtenerFormaDePagoPorDefecto();
            Assert.That(def, Is.Not.Null);
            Assert.That(def!.Id, Is.EqualTo(efectivo.Id));

            // 4) Agregar personalizada y luego eliminar (no default)
            var personalizadaId = agg.AgregarFormaDePagoPersonalizada(
                FormaDePago.ContadoPersonalizado("TRANSFER_APP", "Transfer App"),
                nombre: "Mi App",
                visible: true,
                orden: 999,
                esPorDefecto: false);

            // Eliminación de personalizada (válida)
            agg.EliminarFormaDePago(personalizadaId);

            // 5) No se puede eliminar una del sistema ni la default
            Assert.That(() => agg.EliminarFormaDePago(efectivo.Id),
                Throws.TypeOf<InvalidOperationException>());

            var ahoraDefault = agg.ListarFormasDePago().First(x => x.EsPorDefecto);
            Assert.That(() => agg.EliminarFormaDePago(ahoraDefault.Id),
                Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void UnidadesDeMedida_ReglasSistema_Default_YPersonalizadas()
        {
            var agg = NuevaEmpresaBaseline();

            // Default NIU
            var def = agg.ObtenerUnidadDeMedidaPorDefecto();
            Assert.That(def, Is.Not.Null);
            Assert.That(def!.Unidad, Is.EqualTo(UnidadDeMedida.NIU));

            // No se puede editar código/nombre de una unidad de sistema
            Assert.That(() => agg.ActualizarUnidadDeMedida(def.Id, nuevaUnidad: UnidadDeMedida.KGM),
                Throws.TypeOf<InvalidOperationException>());

            // No se puede ocultar la default
            Assert.That(() => agg.ActualizarUnidadDeMedida(def.Id, visible: false),
                Throws.TypeOf<InvalidOperationException>());

            // Agregar personalizada y setearla como default
            var persId = agg.AgregarUnidadDeMedidaPersonalizada(
                unidad: (UnidadDeMedida)"CJ",
                nombre: "CAJA",
                visible: true,
                orden: 999,
                esPorDefecto: false);

            agg.EstablecerUnidadDeMedidaPorDefecto(persId);
            var nuevaDef = agg.ObtenerUnidadDeMedidaPorDefecto();
            Assert.That(nuevaDef, Is.Not.Null);
            Assert.That(nuevaDef!.Id, Is.EqualTo(persId));

            // No se puede eliminar la default actual
            Assert.That(() => agg.EliminarUnidadDeMedida(persId),
                Throws.TypeOf<InvalidOperationException>());

            // Tampoco se puede eliminar una de sistema
            Assert.That(() => agg.EliminarUnidadDeMedida(def.Id),
                Throws.TypeOf<InvalidOperationException>());
        }
    }
}
