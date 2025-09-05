using System;
using System.Linq;
using NUnit.Framework;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;
using ConfiguracionSistemaBC.Domain.Aggregates;
using ConfiguracionSistemaBC.Domain.ValueObjects;

namespace ConfiguracionSistemaBC.Domain.Tests.Aggregates
{
    [TestFixture]
    [Category("Domain")]
    public class SerieComprobanteTests
    {
        // Se inicializan en [SetUp] → evitamos CS8618
        private EmpresaId _empresa = null!;
        private EstablecimientoId _est1 = null!;
        private EstablecimientoId _est2 = null!;

        [SetUp]
        public void SetUp()
        {
            // En tu SharedKernel, EmpresaId es opaco; usamos el RUC canonizado como string
            _empresa = EmpresaId.From("20600893409");
            _est1 = EstablecimientoId.New();
            _est2 = EstablecimientoId.New();
        }

        private static SerieCodigo F(string s) => SerieCodigo.ForTipo(s, TipoComprobanteCodigo.Factura);
        private static SerieCodigo B(string s) => SerieCodigo.ForTipo(s, TipoComprobanteCodigo.Boleta);

        private SerieComprobante NuevaFacturaFE01(
            EstablecimientoId est,
            int correlativoInicial = 1,
            bool porDefecto = false,
            bool habilitada = true)
            => SerieComprobante.Crear(
                _empresa,
                TipoComprobanteCodigo.Factura,
                F("F001"),
                est,
                TipoOperacion.Default,
                Correlativo.From(correlativoInicial),
                porDefecto,
                habilitada);

        // ---------------------------------------------------------------------
        // Creación / Validaciones
        // ---------------------------------------------------------------------

        [Test]
        public void Crear_Factura_FE01_Con_Correlativo_1_Ok()
        {
            var s = NuevaFacturaFE01(_est1, correlativoInicial: 1);

            Assert.That(s.EmpresaId, Is.EqualTo(_empresa));
            Assert.That(s.Tipo, Is.EqualTo(TipoComprobanteCodigo.Factura));
            Assert.That((string)s.Serie, Is.EqualTo("F001"));
            Assert.That(s.EstablecimientoId, Is.EqualTo(_est1));
            Assert.That(s.TipoOperacion, Is.EqualTo(TipoOperacion.Default));
            Assert.That((int)s.Siguiente, Is.EqualTo(1));
            Assert.That(s.Habilitada, Is.True);
            Assert.That(s.EsPorDefecto, Is.False);
            Assert.That(s.FueUsada, Is.False);
            Assert.That(s.PuedeEliminar, Is.True);
            Assert.That(s.Version, Is.EqualTo(0));
            Assert.That(s.DomainEvents.Count, Is.GreaterThan(0)); // Se emite SerieComprobanteCreada
        }

        [Test]
        public void Crear_Boleta_con_Serie_Que_Empieza_Con_F_Lanza_ArgumentException()
        {
            // Serie "F001" no es válida para Boleta (prefijo debe ser 'B')
            Assert.Throws<ArgumentException>(() =>
            {
                _ = SerieComprobante.Crear(
                    _empresa,
                    TipoComprobanteCodigo.Boleta,
                    SerieCodigo.From("F001"), // formato ok, pero prefijo inválido para Boleta
                    _est1,
                    TipoOperacion.Default,
                    Correlativo.From(1));
            });
        }

        // ---------------------------------------------------------------------
        // Edición: cambiar serie / establecimiento / tipo de operación
        // ---------------------------------------------------------------------

        [Test]
        public void CambiarSerie_AntesDeUso_Ok()
        {
            var s = NuevaFacturaFE01(_est1);
            s.ClearDomainEvents();

            s.CambiarSerie(F("F002"));

            Assert.That((string)s.Serie, Is.EqualTo("F002"));
            Assert.That(s.Version, Is.EqualTo(1));
            Assert.That(s.DomainEvents.Any(), Is.True);
        }

        [Test]
        public void CambiarSerie_DespuesDeUso_Falla()
        {
            var s = NuevaFacturaFE01(_est1);
            _ = s.ReservarSiguiente(); // la marca como usada

            Assert.Throws<BusinessRuleException>(() => s.CambiarSerie(F("F002")));
        }

        [Test]
        public void CambiarEstablecimiento_AntesDeUso_Ok()
        {
            var s = NuevaFacturaFE01(_est1);
            s.CambiarEstablecimiento(_est2);

            Assert.That(s.EstablecimientoId, Is.EqualTo(_est2));
            Assert.That(s.Version, Is.EqualTo(1));
        }

        [Test]
        public void CambiarEstablecimiento_DespuesDeUso_Falla()
        {
            var s = NuevaFacturaFE01(_est1);
            _ = s.ReservarSiguiente();

            Assert.Throws<BusinessRuleException>(() => s.CambiarEstablecimiento(_est2));
        }

        [Test]
        public void CambiarTipoOperacion_Aun_Usada_Ok()
        {
            var s = NuevaFacturaFE01(_est1);
            _ = s.ReservarSiguiente(); // usada

            s.CambiarTipoOperacion(TipoOperacion.ExportacionBienes);

            Assert.That(s.TipoOperacion, Is.EqualTo(TipoOperacion.ExportacionBienes));
            Assert.That(s.Version, Is.EqualTo(2)); // 1 por reservar, 1 por cambiar tipo de operación
        }

        // ---------------------------------------------------------------------
        // Habilitar / Inhabilitar / Por defecto
        // ---------------------------------------------------------------------

        [Test]
        public void EstablecerPorDefecto_Solo_Si_Habilitada()
        {
            var s = NuevaFacturaFE01(_est1, habilitada: true);
            s.EstablecerPorDefecto(true);

            Assert.That(s.EsPorDefecto, Is.True);

            // No se puede inhabilitar una serie por defecto
            Assert.Throws<BusinessRuleException>(() => s.Inhabilitar());
        }

        [Test]
        public void Inhabilitar_No_Permite_Si_EsPorDefecto()
        {
            var s = NuevaFacturaFE01(_est1, porDefecto: true);

            Assert.Throws<BusinessRuleException>(() => s.Inhabilitar());

            // Si quitamos el default, ya permite inhabilitar
            s.EstablecerPorDefecto(false);
            s.Inhabilitar();

            Assert.That(s.Habilitada, Is.False);
        }

        [Test]
        public void Habilitar_Restituye_Seleccion()
        {
            var s = NuevaFacturaFE01(_est1);
            s.Inhabilitar();
            s.Habilitar();

            Assert.That(s.Habilitada, Is.True);
        }

        // ---------------------------------------------------------------------
        // Numeración / Reserva / Ajuste
        // ---------------------------------------------------------------------

        [Test]
        public void ReservarSiguiente_AvanzaNumerador_MarcaUsada_Y_EmiteEventos()
        {
            var s = NuevaFacturaFE01(_est1, correlativoInicial: 14);
            s.ClearDomainEvents();

            var reservado = s.ReservarSiguiente();

            Assert.That((int)reservado, Is.EqualTo(14));
            Assert.That((int)s.Siguiente, Is.EqualTo(15));
            Assert.That(s.FueUsada, Is.True);
            Assert.That(s.PuedeEliminar, Is.False);
            Assert.That(s.Version, Is.EqualTo(1));
            Assert.That(s.DomainEvents.Count, Is.GreaterThanOrEqualTo(2)); // SerieUsadaPrimeraVez + CorrelativoReservado
        }

        [Test]
        public void ReservarSiguiente_En_Maximo_Lanza_InvalidOperationException()
        {
            var s = NuevaFacturaFE01(_est1, correlativoInicial: Correlativo.Max);

            Assert.Throws<InvalidOperationException>(() => s.ReservarSiguiente());
        }

        [Test]
        public void AjustarNumerador_Solo_Hacia_Adelante()
        {
            var s = NuevaFacturaFE01(_est1, correlativoInicial: 100);
            s.AjustarNumeradorHaciaAdelante(Correlativo.From(150));

            Assert.That((int)s.Siguiente, Is.EqualTo(150));
            Assert.That(s.Version, Is.EqualTo(1));

            Assert.Throws<BusinessRuleException>(() =>
                s.AjustarNumeradorHaciaAdelante(Correlativo.From(149)));
        }

        // ---------------------------------------------------------------------
        // Eliminación (política)
        // ---------------------------------------------------------------------

        [Test]
        public void PuedeEliminar_True_Si_No_FueUsada_False_Si_Ya_Usada()
        {
            var s = NuevaFacturaFE01(_est1);

            Assert.That(s.PuedeEliminar, Is.True);

            _ = s.ReservarSiguiente();

            Assert.That(s.PuedeEliminar, Is.False);
        }

        // ---------------------------------------------------------------------
        // Eventos utilitarios
        // ---------------------------------------------------------------------

        [Test]
        public void ClearDomainEvents_Deja_Lista_Vacia()
        {
            var s = NuevaFacturaFE01(_est1);
            Assert.That(s.DomainEvents.Any(), Is.True);

            s.ClearDomainEvents();
            Assert.That(s.DomainEvents.Any(), Is.False);
        }
    }
}
