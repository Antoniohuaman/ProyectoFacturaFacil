using System;
using System.Linq;
using NUnit.Framework;
using ConfiguracionSistemaBC.Domain.Aggregates;
using ConfiguracionSistemaBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace ConfiguracionSistemaBC.Tests.Domain.Aggregates
{
    [TestFixture]
    public class ConfiguracionEmpresaTests
    {
        // Helpers de creación (ajusta a tus factorías reales si difiere algún nombre)
        private static Ruc RUC(string v = "20000000001") => Ruc.From(v);
        // Ubigeo válido: "150122" (Miraflores, Lima, Lima)
        private static DireccionPostal DirFiscal() => DireccionPostal.FromPeru(
            linea: "Av. Principal 123",
            ubigeo: "150122",
            departamento: "Lima",
            provincia: "Lima",
            distrito: "Miraflores"
        );
        private static Moneda PEN() => Moneda.PEN();

        [Test]
        public void RegistrarNueva_CreaEmpresaConBootstrap_PrincipalYSeriesPorDefecto()
        {
            var empresa = ConfiguracionEmpresa.RegistrarNueva(RUC(), "ACME S.A.C.", DirFiscal(), PEN());

            // EmpresaId opaco basado en RUC
            Assert.That(empresa.EmpresaId.Value, Is.EqualTo("20000000001"));

            // Pie de página por defecto
            Assert.That(empresa.PieDePagina.Html, Is.EqualTo("Gracias Por su Preferencia"));

            // Establecimiento principal
            var principal = empresa.ObtenerEstablecimientoPrincipal();
            Assert.That(principal, Is.Not.Null);
            Assert.That(principal!.Codigo, Is.EqualTo("01"));
            Assert.That(principal.Nombre, Is.EqualTo("Establecimiento Principal"));

            // Series FE01/BE01 por defecto
            var fe = empresa.ObtenerSeriePorDefecto(TipoComprobanteCodigo.Factura);
            var be = empresa.ObtenerSeriePorDefecto(TipoComprobanteCodigo.Boleta);

            Assert.That(fe, Is.Not.Null);
            Assert.That(be, Is.Not.Null);
            Assert.That(fe!.Serie, Is.EqualTo("FE01"));
            Assert.That(fe.CorrelativoActual.Valor, Is.EqualTo(1));
            Assert.That(be!.Serie, Is.EqualTo("BE01"));
            Assert.That(be.CorrelativoActual.Valor, Is.EqualTo(1));
        }

        [Test]
        public void RecodificarEstablecimiento_CambiaCodigo_ManteniendoUnicidad()
        {
            var empresa = ConfiguracionEmpresa.RegistrarNueva(RUC(), "ACME S.A.C.", DirFiscal(), PEN());
            var principal = empresa.ObtenerEstablecimientoPrincipal()!;
            empresa.RecodificarEstablecimiento(principal.Id, "A1");

            var byCode = empresa.BuscarEstablecimientoPorCodigo("A1");
            Assert.That(byCode, Is.Not.Null);
            Assert.That(byCode!.Id, Is.EqualTo(principal.Id));
        }

        [Test]
        public void EliminarEstablecimiento_NoPermiteDejarSinNinguno_PromueveOtroSiEsPrincipal()
        {
            var empresa = ConfiguracionEmpresa.RegistrarNueva(RUC(), "ACME S.A.C.", DirFiscal(), PEN());
            var p = empresa.ObtenerEstablecimientoPrincipal()!;
            var otroId = empresa.RegistrarEstablecimiento("02", "Sucursal", DirFiscal());

            // eliminar principal -> debe promover otro automáticamente
            empresa.EliminarEstablecimiento(p.Id);

            var principal = empresa.ObtenerEstablecimientoPrincipal();
            Assert.That(principal, Is.Not.Null);
            Assert.That(principal!.Id, Is.EqualTo(otroId));

            // no permitir dejar sin establecimientos
            Assert.Throws<InvalidOperationException>(() => empresa.EliminarEstablecimiento(otroId));
        }

        [Test]
        public void EliminarEstablecimiento_FallaSiTieneSerieBloqueada()
        {
            var empresa = ConfiguracionEmpresa.RegistrarNueva(RUC(), "ACME S.A.C.", DirFiscal(), PEN());
            var p = empresa.ObtenerEstablecimientoPrincipal()!;
            // crear serie adicional y bloquearla
            var sid = empresa.AgregarSerie(TipoComprobanteCodigo.Boleta, SerieCodigo.From("BE02"), p.Id, Correlativo.From(1));
            empresa.BloquearSeriePorUso(sid);

            Assert.Throws<InvalidOperationException>(() => empresa.EliminarEstablecimiento(p.Id));
        }

        [Test]
        public void ActualizarSerie_CambiaSerie_Establecimiento_Y_Default()
        {
            var empresa = ConfiguracionEmpresa.RegistrarNueva(RUC(), "ACME S.A.C.", DirFiscal(), PEN());
            var p = empresa.ObtenerEstablecimientoPrincipal()!;
            var s = empresa.ObtenerSeriePorDefecto(TipoComprobanteCodigo.Boleta)!;
            var nuevoEstId = empresa.RegistrarEstablecimiento("03", "Otra Sucursal", DirFiscal());

            empresa.ActualizarSerie(
                s.Id,
                nuevaSerie: SerieCodigo.From("BE09"),
                nuevoEstablecimientoId: nuevoEstId,
                esPorDefecto: true);

            var def = empresa.ObtenerSeriePorDefecto(TipoComprobanteCodigo.Boleta);
            Assert.That(def!.Serie, Is.EqualTo("BE09"));
            Assert.That(def.EstablecimientoId, Is.EqualTo(nuevoEstId));
        }
    }
}
