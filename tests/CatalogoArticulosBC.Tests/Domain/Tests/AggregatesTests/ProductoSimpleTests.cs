using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;

using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.Entities;
using CatalogoArticulosBC.Domain.Exceptions;
using CatalogoArticulosBC.Domain.Events;
using CatalogoArticulosBC.Domain.Services;
using CatalogoArticulosBC.Domain.ValueObjects;

using SharedKernel.ValueObjects;

namespace CatalogoArticulosBC.Tests.Domain.Aggregates
{
    [TestFixture]
    public class ProductoSimpleTests
    {
        // -------------------------------
        // Helpers de fábrica de datos
        // -------------------------------
        private static Moneda PEN() => Moneda.PEN();
        private static Sku SKU(string v = "ABC-001") => Sku.Crear(v);
        private static NombreProducto NOMBRE(string v = "Agua Mineral 600ml") => new NombreProducto(v);
        private static UnidadDeMedida UDM() => SharedKernel.ValueObjects.UnidadDeMedida.NIU;
        private static AfectacionImpuesto AfectG() => AfectacionImpuesto.Gravado_10;
        private static AfectacionImpuesto AfectNoG() => AfectacionImpuesto.Exonerado_20;
        private static TasaImpuesto Tasa18() => TasaImpuesto.IGV18;
        private static TasaImpuesto Tasa10() => TasaImpuesto.IGV10;
        private static TasaImpuesto Tasa0() => TasaImpuesto.Cero;
        private static Categoria CAT(string v = "Bebidas") => new Categoria(v);
        private static Marca MARCA(string v = "ACME") => new Marca(v);
        private static List<EstablecimientoId> Estabs1() => new() { EstablecimientoId.New() };
        private static CentroDeCosto CC() => CentroDeCosto.Create("CC01", "Ventas");
        private static Peso PESO(decimal v = 1.5m) => new Peso(v);
        private static CodigoSUNAT SUNAT(string v = "12345678") => new CodigoSUNAT(v);
        private static CodigoFabrica CF(string? v = "F-001") => new CodigoFabrica(v);
        private static CodigoBarras CB(string v = "5901234123457") => new CodigoBarras(v); // EAN-13 válido clásico

        private static MultimediaProducto Media(string mime = "image/jpeg")
            => new MultimediaProducto(Guid.NewGuid(), mime, "ImagenPrincipal", "foto.jpg", "/ruta/foto.jpg", "ok", 1000);

        private static ProductoSimple NewProducto(
            Moneda? moneda = null,
            Sku? sku = null,
            NombreProducto? nombre = null,
            UnidadDeMedida? udm = null,
            AfectacionImpuesto? afect = null,
            TasaImpuesto? tasa = null,
            Categoria? categoria = null,
            List<EstablecimientoId>? ests = null,
            string? descripcion = "   descripción con trim   ",
            Marca? marca = null,
            PrecioVenta? precio = null,
            CodigoSUNAT? codigoSunat = null,
            CentroDeCosto? cc = null,
            Peso? peso = null,
            CodigoBarras? barras = null,
            CodigoFabrica? fabrica = null,
            TipoProducto tipo = TipoProducto.Bien,
            TipoExistencia tipoExistencia = TipoExistencia.ProductosTerminados,
            bool asignarATodos = false,
            Guid? imgId = null
        )
        {
            return new ProductoSimple(
                empresaId: EmpresaId.From("20123456789"),
                moneda: moneda ?? PEN(),
                sku: sku ?? SKU(),
                nombre: nombre ?? NOMBRE(),
                unidadMedida: udm ?? UDM(),
                afectacionImpuesto: afect ?? AfectG(),
                tasaImpuesto: tasa ?? Tasa18(),
                categoria: categoria ?? CAT(),
                establecimientosAsignados: ests ?? Estabs1(),
                descripcion: descripcion,
                marca: marca,
                precioVenta: precio,
                codigoSunat: codigoSunat,
                centroDeCosto: cc,
                peso: peso,
                codigoBarras: barras,
                codigoFabrica: fabrica,
                tipo: tipo,
                tipoExistencia: tipoExistencia,
                asignarATodosLosEstablecimientos: asignarATodos,
                imagenPrincipalId: imgId
            );
        }

        private static T GetSingleEvent<T>(ProductoSimple p) where T : class
            => p.DomainEvents.OfType<T>().Single();

        // ----------------------------------------
        // Constructor: OK y validaciones
        // ----------------------------------------
        [Test]
        public void Constructor_CreaProductoValido_SetProps_Y_Emite_ProductoCreado()
        {
            var p = NewProducto(marca: MARCA(), codigoSunat: SUNAT(), cc: CC(), barras: CB("5901234123457"), fabrica: CF());

            Assert.That(p.ProductoId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(p.Habilitado, Is.True);
            Assert.That(p.Sku.Valor, Is.EqualTo("ABC-001"));
            Assert.That(p.Nombre.Valor, Is.EqualTo("Agua Mineral 600ml"));
            Assert.That(p.UnidadMedida.Codigo, Is.EqualTo("NIU"));
            Assert.That(p.AfectacionImpuesto.GravaImpuesto, Is.True);
            Assert.That(p.TasaImpuesto.Fraccion, Is.EqualTo(0.18m));
            Assert.That(p.Categoria.Nombre, Is.EqualTo("BEBIDAS"));
            Assert.That(p.Descripcion, Is.EqualTo("descripción con trim")); // se hace Trim
            Assert.That(p.EstablecimientosAsignados.Count, Is.EqualTo(1));
            Assert.That(p.AsignarATodosLosEstablecimientos, Is.False);
            Assert.That(p.Tipo, Is.EqualTo(TipoProducto.Bien));
            Assert.That(p.TipoExistencia, Is.EqualTo(TipoExistencia.ProductosTerminados));

            // Evento
            Assert.That(p.DomainEvents.Count, Is.EqualTo(1));
            Assert.That(p.DomainEvents.First(), Is.TypeOf<ProductoCreado>());
            var ev = GetSingleEvent<ProductoCreado>(p);
            Assert.That(ev, Is.Not.Null);
        }

        [Test]
        public void Constructor_Falla_SinEstablecimientos()
        {
            Assert.That(() =>
                NewProducto(ests: new List<EstablecimientoId>()),
                Throws.ArgumentException.With.Message.Contains("al menos un establecimiento"));
        }

        [Test]
        public void Constructor_Falla_AfectacionNoGrava_PeroTasaNoCero()
        {
            Assert.That(() =>
                NewProducto(afect: AfectNoG(), tasa: Tasa18()),
                Throws.ArgumentException.With.Message.Contains("no grava impuesto"));
        }

        [Test]
        public void Constructor_Falla_AfectacionGrava_PeroTasaCero()
        {
            Assert.That(() =>
                NewProducto(afect: AfectG(), tasa: Tasa0()),
                Throws.ArgumentException.With.Message.Contains("no puede ser 0%"));
        }

        [Test]
        public void Constructor_Falla_AfectacionGrava_TasaNoPermitida_12()
        {
            Assert.That(() =>
                NewProducto(afect: AfectG(), tasa: TasaImpuesto.IGV12),
                Throws.ArgumentException.With.Message.Contains("Solo se permite IGV 18% o IGV 10%"));
        }

        [Test]
        public void Constructor_Falla_MonedaNula()
        {
            Assert.That(() =>
                NewProducto(moneda: null).Moneda, // fuerza construcción con null
                Throws.Nothing, "Helper siempre crea Moneda; probemos null directo");

            Assert.That(() =>
            {
                // Construcción explícita con moneda null para validar excepción real del ctor
                _ = new ProductoSimple(
                    empresaId: EmpresaId.From("20123456789"),
                    moneda: null!,
                    sku: SKU(),
                    nombre: NOMBRE(),
                    unidadMedida: UDM(),
                    afectacionImpuesto: AfectG(),
                    tasaImpuesto: Tasa18(),
                    categoria: CAT(),
                    establecimientosAsignados: Estabs1()
                );
            }, Throws.TypeOf<ArgumentNullException>());
        }

        // ----------------------------------------
        // EditarDatos: OK y validaciones
        // ----------------------------------------
        [Test]
        public void EditarDatos_ActualizaPropiedades_Y_Emite_ProductoActualizado()
        {
            var p = NewProducto();

            var nuevoNombre = new NombreProducto("Gaseosa Cola 500ml");
            var nuevaUdm = SharedKernel.ValueObjects.UnidadDeMedida.LTR;
            var nuevaAfect = AfectG();
            var nuevaTasa = Tasa10(); // permitido cuando grava
            var nuevaCat = new Categoria("Gaseosas");
            var nuevaMarca = new Marca("Coca-Cola");
            var nuevoPrecio = new PrecioVenta(5.49m, PEN(), nuevaAfect, incluyeIGV: true);
            var nuevoCC = CentroDeCosto.Create("VENTA", "Venta Mostrador");
            var nuevoPeso = new Peso(0.7m);
            var nuevoBarras = CB("5901234123457");
            var nuevoFabrica = new CodigoFabrica("CO-500");
            var nuevoTipo = TipoProducto.Bien;
            var nuevoCodSunat = new CodigoSUNAT("87654321");
            var nuevosEsts = new List<EstablecimientoId> { EstablecimientoId.New(), EstablecimientoId.New() };

            p.EditarDatos(
                nombre: nuevoNombre,
                unidadMedida: nuevaUdm,
                afectacionImpuesto: nuevaAfect,
                tasaImpuesto: nuevaTasa,
                categoria: nuevaCat,
                marca: nuevaMarca,
                precioVenta: nuevoPrecio,
                centroDeCosto: nuevoCC,
                peso: nuevoPeso,
                codigoBarras: nuevoBarras,
                codigoFabrica: nuevoFabrica,
                tipo: nuevoTipo,
                codigoSunat: nuevoCodSunat,
                establecimientosAsignados: nuevosEsts,
                asignarATodosLosEstablecimientos: true,
                imagenPrincipalId: Guid.NewGuid(),
                descripcion: "  Texto nuevo  ",
                tipoExistencia: TipoExistencia.Mercaderias
            );

            Assert.Multiple(() =>
            {
                Assert.That(p.Nombre, Is.EqualTo(nuevoNombre));
                Assert.That(p.UnidadMedida, Is.EqualTo(nuevaUdm));
                Assert.That(p.AfectacionImpuesto, Is.EqualTo(nuevaAfect));
                Assert.That(p.TasaImpuesto, Is.EqualTo(nuevaTasa));
                Assert.That(p.Categoria, Is.EqualTo(nuevaCat));
                Assert.That(p.Marca, Is.EqualTo(nuevaMarca));
                Assert.That(p.PrecioVenta, Is.EqualTo(nuevoPrecio));
                Assert.That(p.CentroDeCosto, Is.EqualTo(nuevoCC));
                Assert.That(p.Peso, Is.EqualTo(nuevoPeso));
                Assert.That(p.CodigoBarras, Is.EqualTo(nuevoBarras));
                Assert.That(p.CodigoFabrica, Is.EqualTo(nuevoFabrica));
                Assert.That(p.CodigoSunat, Is.EqualTo(nuevoCodSunat));
                Assert.That(p.EstablecimientosAsignados, Has.Count.EqualTo(2));
                Assert.That(p.AsignarATodosLosEstablecimientos, Is.True);
                Assert.That(p.Tipo, Is.EqualTo(TipoProducto.Bien));
                Assert.That(p.TipoExistencia, Is.EqualTo(TipoExistencia.Mercaderias));
                Assert.That(p.Descripcion, Is.EqualTo("Texto nuevo"));
            });

            Assert.That(p.DomainEvents.OfType<ProductoActualizado>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void EditarDatos_Falla_SinEstablecimientos()
        {
            var p = NewProducto();
            Assert.That(() =>
                p.EditarDatos(
                    NOMBRE("x"), UDM(), AfectG(), Tasa18(), CAT("OTROS"),
                    marca: null, precioVenta: null, centroDeCosto: null, peso: null,
                    codigoBarras: null, codigoFabrica: null, tipo: TipoProducto.Bien,
                    establecimientosAsignados: new List<EstablecimientoId>()),
                Throws.ArgumentException.With.Message.Contains("al menos un establecimiento"));
        }

        [Test]
        public void EditarDatos_Falla_CoherenciaAfectacion_Tasa()
        {
            var p = NewProducto();

            // no grava + tasa != 0
            Assert.That(() =>
                p.EditarDatos(
                    NOMBRE("x"), UDM(), AfectNoG(), Tasa18(), CAT("OTROS"),
                    marca: null, precioVenta: null, centroDeCosto: null, peso: null,
                    codigoBarras: null, codigoFabrica: null, tipo: TipoProducto.Bien,
                    establecimientosAsignados: Estabs1()),
                Throws.ArgumentException);

            // grava + tasa = 0
            Assert.That(() =>
                p.EditarDatos(
                    NOMBRE("x"), UDM(), AfectG(), Tasa0(), CAT("OTROS"),
                    marca: null, precioVenta: null, centroDeCosto: null, peso: null,
                    codigoBarras: null, codigoFabrica: null, tipo: TipoProducto.Bien,
                    establecimientosAsignados: Estabs1()),
                Throws.ArgumentException);

            // grava + tasa 12% no permitida
            Assert.That(() =>
                p.EditarDatos(
                    NOMBRE("x"), UDM(), AfectG(), TasaImpuesto.IGV12, CAT("OTROS"),
                    marca: null, precioVenta: null, centroDeCosto: null, peso: null,
                    codigoBarras: null, codigoFabrica: null, tipo: TipoProducto.Bien,
                    establecimientosAsignados: Estabs1()),
                Throws.ArgumentException.With.Message.Contains("Solo se permite IGV 18% o IGV 10%"));
        }

        // ----------------------------------------
        // Habilitar / Deshabilitar / CambiarCategoria
        // ----------------------------------------
        [Test]
        public void Deshabilitar_Y_Luego_Habilitar_ActualizaEstado_Y_EmiteEventos()
        {
            var p = NewProducto();

            p.Deshabilitar("stock negativo");
            Assert.That(p.Habilitado, Is.False);
            Assert.That(p.DomainEvents.OfType<ProductoInhabilitado>().Count(), Is.EqualTo(1));

            p.Habilitar("jdoe", "ajuste");
            Assert.That(p.Habilitado, Is.True);
            Assert.That(p.DomainEvents.OfType<ProductoHabilitado>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void CambiarCategoria_ActualizaCategoria_Y_EmiteEvento()
        {
            var p = NewProducto();
            var nueva = new Categoria("Lácteos");
            p.CambiarCategoria(nueva, "jdoe");

            Assert.That(p.Categoria, Is.EqualTo(nueva));
            Assert.That(p.DomainEvents.OfType<ProductoCategoriaCambiada>().Count(), Is.EqualTo(1));
        }

        // ----------------------------------------
        // Multimedia
        // ----------------------------------------
        [Test]
        public void AgregarMultimedia_TipoPermitido_Agrega_Y_EmiteEvento()
        {
            var p = NewProducto();
            var m = Media("image/png");

            p.AgregarMultimedia(m);

            Assert.That(p.Multimedia.Count, Is.EqualTo(1));
            Assert.That(p.DomainEvents.OfType<MultimediaAgregada>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void AgregarMultimedia_TipoNoPermitido_Lanza_MultimediaInvalidaException()
        {
            var p = NewProducto();
            var m = Media("image/gif");

            Assert.That(() => p.AgregarMultimedia(m), Throws.TypeOf<MultimediaInvalidaException>());
        }

        [Test]
        public void AgregarMultimedia_MasDeCinco_Lanza_LimiteMultimediaException()
        {
            var p = NewProducto();
            for (int i = 0; i < 5; i++)
                p.AgregarMultimedia(Media("application/pdf"));

            Assert.That(p.Multimedia.Count, Is.EqualTo(5));
            Assert.That(() => p.AgregarMultimedia(Media()), Throws.TypeOf<LimiteMultimediaException>());
        }

        [Test]
        public void AsignarImagenPrincipal_RequiereExistente()
        {
            var p = NewProducto();
            var m = Media();
            p.AgregarMultimedia(m);

            // OK cuando existe
            p.AsignarImagenPrincipal(m.MultimediaId);
            Assert.That(p.ImagenPrincipalId, Is.EqualTo(m.MultimediaId));

            // Falla cuando no existe
            Assert.That(() => p.AsignarImagenPrincipal(Guid.NewGuid()), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void EliminarMultimedia_QuitaElemento_Y_EmiteEvento()
        {
            var p = NewProducto();
            var m = Media();
            p.AgregarMultimedia(m);

            p.EliminarMultimedia(m.MultimediaId);

            Assert.That(p.Multimedia.Count, Is.EqualTo(0));
            Assert.That(p.DomainEvents.OfType<MultimediaEliminada>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void LimpiarMultimedia_EliminaTodo_Y_EmiteEventosPorCadaUno()
        {
            var p = NewProducto();
            var m1 = Media(); var m2 = Media("image/png"); var m3 = Media("application/pdf");
            p.AgregarMultimedia(m1);
            p.AgregarMultimedia(m2);
            p.AgregarMultimedia(m3);

            p.LimpiarMultimedia();

            Assert.That(p.Multimedia.Count, Is.EqualTo(0));
            Assert.That(p.DomainEvents.OfType<MultimediaEliminada>().Count(), Is.EqualTo(3));
        }

        // ----------------------------------------
        // SKU: asignación manual y generador
        // ----------------------------------------
        [Test]
        public void AsignarSku_Actualiza_Y_EmiteEvento()
        {
            var p = NewProducto();
            var nuevo = Sku.Crear("NEW-001");

            p.AsignarSku(nuevo);

            Assert.That(p.Sku, Is.EqualTo(nuevo));
            Assert.That(p.DomainEvents.OfType<SkuActualizado>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void AsignarSku_Null_Lanza()
        {
            var p = NewProducto();
            Assert.That(() => p.AsignarSku(null!), Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void GenerarSku_UsaServicio_Actualiza_Y_EmiteEvento()
        {
            var gen = new Mock<ISkuGenerator>();
            gen.Setup(g => g.Generar()).Returns(Sku.Crear("AUTO-999"));

            var p = NewProducto();
            p.GenerarSku(gen.Object);

            Assert.That(p.Sku.Valor, Is.EqualTo("AUTO-999"));
            Assert.That(p.DomainEvents.OfType<SkuActualizado>().Count(), Is.EqualTo(1));
            gen.Verify(g => g.Generar(), Times.Once);
        }

        [Test]
        public void GenerarSku_GeneratorNull_Lanza()
        {
            var p = NewProducto();
            Assert.That(() => p.GenerarSku(null!), Throws.TypeOf<ArgumentNullException>());
        }

        // ----------------------------------------
        // Consultas auxiliares y eventos
        // ----------------------------------------
        [Test]
        public void PesoValor_Retorna0_CuandoPesoNull_Y_ValorCuandoExiste()
        {
            var p = NewProducto(peso: null);
            Assert.That(p.PesoValor, Is.EqualTo(0m));

            // actualizar con peso
            p.EditarDatos(NOMBRE("x"), UDM(), AfectG(), Tasa18(), CAT("OTROS"),
                marca: null, precioVenta: null, centroDeCosto: null, peso: PESO(2.34m),
                codigoBarras: null, codigoFabrica: null, tipo: TipoProducto.Bien,
                establecimientosAsignados: Estabs1());

            Assert.That(p.PesoValor, Is.EqualTo(2.34m));
        }

        [Test]
        public void ClearDomainEvents_LimpiaColeccionDeEventos()
        {
            var p = NewProducto();
            p.Deshabilitar("x");
            p.Habilitar("u");
            p.AgregarMultimedia(Media());

            Assert.That(p.DomainEvents.Count, Is.GreaterThan(0));
            p.ClearDomainEvents();
            Assert.That(p.DomainEvents.Count, Is.EqualTo(0));
        }
    }
}
