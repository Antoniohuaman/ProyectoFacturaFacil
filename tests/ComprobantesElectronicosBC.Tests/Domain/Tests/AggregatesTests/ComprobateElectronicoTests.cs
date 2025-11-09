// Archivo sugerido: ComprobantesElectronicosBC.Domain.Tests/ComprobanteElectronicoTests.cs
using System;
using System.Linq;
using NUnit.Framework;
using SharedKernel.ValueObjects;
using SharedKernel.Events;
using ComprobantesElectronicosBC.Domain.Aggregates;
using ComprobantesElectronicosBC.Domain.Entities;
using ComprobantesElectronicosBC.Domain.ValueObjects;
using ComprobantesElectronicosBC.Domain.Events;
namespace ComprobantesElectronicosBC.Domain.Tests
{
    [TestFixture]
    public class ComprobanteElectronicoTests
    {
        // ----------------------- FIXTURE -----------------------
        private static Moneda PEN() => Moneda.PEN();
        private static Moneda USD() => Moneda.USD();

    private static TipoDeComprobante Factura() => TipoDeComprobante.Factura; // Usar propiedad estática
    private static TipoDeComprobante Boleta()  => TipoDeComprobante.Boleta;

        private static EmisorSnapshot Emisor() => EmisorSnapshot.Create(
            empresaId: EmpresaId.From(Guid.NewGuid().ToString()),
            tenantId: new TenantId(Guid.NewGuid()),
            establecimientoId: EstablecimientoId.From(Guid.NewGuid()),
            ruc: "20123456789",
            razonSocial: "MI EMPRESA SAC",
            domicilio: DomicilioFiscal.FromPeru("Av. Principal 123", "150101", "Lima", "Lima", "Lima", "0000"),
            email: null,
            telefono: null
        );

        private static UsuarioSnapshot Usuario() => new UsuarioSnapshot(
            codigo: Guid.NewGuid().ToString(),
            nombreCompleto: "Cajero Uno",
            rol: "Cajero"
        );

        private static ClienteSnapshot ClienteRuc() => ClienteSnapshot.Create(
            empresaId: EmpresaId.From(Guid.NewGuid().ToString()),
            tenantId: new TenantId(Guid.NewGuid()),
            documento: DocumentoIdentidad.Crear(TipoDocumento.Ruc, "10112233445"),
            nombre: "CLIENTE RUC SA"
        );
        private static ClienteSnapshot ClienteDni() => ClienteSnapshot.Create(
            empresaId: EmpresaId.From(Guid.NewGuid().ToString()),
            tenantId: new TenantId(Guid.NewGuid()),
            documento: DocumentoIdentidad.Crear(TipoDocumento.Dni, "44112233"),
            nombre: "CLIENTE DNI"
        );

    private static FechaEmision Hoy() => FechaEmision.Create(DateOnly.FromDateTime(DateTime.Now), Factura().Codigo);
    private static FormaDePago Contado() => FormaDePago.Contado();
    private static FechaVencimiento VenceHoy() => FechaVencimiento.Create(DateOnly.FromDateTime(DateTime.Now), DateOnly.FromDateTime(DateTime.Now));

    private static DescripcionProducto Prod(string nombre = "PROD") => DescripcionProducto.Create(nombre);
    private static UnidadDeMedida UM_NIU() => UnidadDeMedida.NIU;
    private static Cantidad Cant(decimal v) => Cantidad.Create(v);

    private static AfectacionImpuesto Gravado_IGV() => AfectacionImpuesto.Gravado_10;
    private static AfectacionImpuesto Exonerado()   => AfectacionImpuesto.Exonerado_20;
    private static TasaImpuesto IGV18() => TasaImpuesto.IGV18;

    private static ImporteMonetario Importe(decimal monto, Moneda m) => ImporteMonetario.Create(monto, m);

    private static DescuentoLinea DescPct(decimal p) => DescuentoLinea.FromPorcentaje(p);
    private static DescuentoLinea DescMonto(decimal m) => DescuentoLinea.FromMonto(m);
    private static DescuentoGlobal DgNone() => DescuentoGlobal.None;
    private static DescuentoGlobal DgPct(decimal p) => DescuentoGlobal.FromPorcentaje(p);
    private static DescuentoGlobal DgMonto(decimal m) => DescuentoGlobal.FromMonto(m);

        private static ComprobanteElectronico NuevoBorradorFacturaPEN_Contado_ClienteRuc()
            => ComprobanteElectronico.CrearBorrador(
                tipo: Factura(), emisor: Emisor(), cliente: ClienteRuc(),
                moneda: PEN(), emision: Hoy(), formaDePago: Contado(),
                vencimiento: VenceHoy(), usuarioEmisor: Usuario()
            );

        private static ComprobanteElectronico NuevoBorradorBoletaPEN_Contado_ClienteDni()
            => ComprobanteElectronico.CrearBorrador(
                tipo: Boleta(), emisor: Emisor(), cliente: ClienteDni(),
                moneda: PEN(), emision: Hoy(), formaDePago: Contado(),
                vencimiento: VenceHoy(), usuarioEmisor: Usuario()
            );

        private static void AgregarLineaSimple(ComprobanteElectronico c, decimal precio, bool incluyeIgv = false, decimal cant = 1m, DescuentoLinea? dsc = null)
        {
            c.AgregarLinea(
                descripcion: Prod("ITEM"),
                unidad: UM_NIU(),
                cantidad: Cant(cant),
                precioUnitario: Importe(precio, c.Moneda),
                afectacion: Gravado_IGV(),
                tasa: IGV18(),
                precioIncluyeIgv: incluyeIgv,
                descuento: dsc
            );
        }

        // ----------------------- TESTS --------------------------

        [Test]
        public void CrearBorrador_IniciaEnBorrador_ConIdentidadesCopiadas()
        {
            var c = NuevoBorradorBoletaPEN_Contado_ClienteDni();

            Assert.That(c.Estado, Is.EqualTo(EstadoComprobante.Borrador));
            Assert.That(c.ComprobanteId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(c.EmpresaId, Is.Not.Null);
            Assert.That(c.TenantId, Is.Not.Null);
            Assert.That(c.EstablecimientoId, Is.Not.Null);
        }

        [Test]
        public void AsignarSerieYNumero_ValidaCompatibilidadConTipo()
        {
            var c = NuevoBorradorBoletaPEN_Contado_ClienteDni();

            c.AsignarSerieYNumero("B001", 123);

            Assert.That(c.SerieNumero, Is.Not.Null);
        }

        [Test]
        public void AgregarEditarEliminarLineas_RecalculaTotales_CorrigeEnumeracion()
        {
            var c = NuevoBorradorBoletaPEN_Contado_ClienteDni();

            // Dos líneas gravadas
            AgregarLineaSimple(c, precio: 100m, incluyeIgv: false, cant: 2m); // base 200, IGV 36
            AgregarLineaSimple(c, precio: 50m,  incluyeIgv: true,  cant: 1m); // unit sin ~42.37, IGV ~7.63

            var subtotalAntes = c.SubtotalBase;
            var igvAntes = c.IgvTotal;
            var totalAntes = c.Total;

            Assert.That(subtotalAntes, Is.GreaterThan(0m));
            Assert.That(igvAntes, Is.GreaterThan(0m));
            Assert.That(Math.Round(subtotalAntes + igvAntes, 2, MidpointRounding.AwayFromZero), Is.EqualTo(totalAntes));

            // Editamos cantidad: se pasa VO Cantidad (no decimal)
            c.EditarLinea(1, cantidad: Cant(3m)); // base 300

            Assert.That(c.SubtotalBase, Is.GreaterThan(subtotalAntes));

            // Cambios parciales de impuesto (solo tasa)
            c.EditarLinea(2, tasa: TasaImpuesto.FromPercent(10m)); // baja IGV línea 2
            var igvDespuesTasa = c.IgvTotal;

                // El IGV debe cambiar al modificar la tasa de impuesto
                Assert.That(Math.Round(igvDespuesTasa, 2, MidpointRounding.AwayFromZero), Is.Not.EqualTo(Math.Round(igvAntes, 2, MidpointRounding.AwayFromZero)), "El IGV debe cambiar al modificar la tasa de impuesto");

            // Eliminamos 1ra línea, se reenumera
            c.EliminarLinea(1);

            Assert.That(c.Lineas, Has.Exactly(1).Items);
            Assert.That(c.Lineas[0].NumeroLinea, Is.EqualTo(1));
        }

        [Test]
        public void DescuentoGlobal_Porcentaje_ProrrateaYRecalculaIgv()
        {
            var c = NuevoBorradorBoletaPEN_Contado_ClienteDni();

            AgregarLineaSimple(c, 100m, incluyeIgv: false, cant: 1m); // base 100 igv 18
            AgregarLineaSimple(c, 200m, incluyeIgv: false, cant: 1m); // base 200 igv 36

            var subtotal = c.SubtotalBase; // 300 aprox
            var igvAntes = c.IgvTotal;

            c.CambiarDescuentoGlobal(DgPct(10m)); // 10%

            var esperadoDcto = Math.Round(subtotal * 0.10m, 2, MidpointRounding.AwayFromZero);
            Assert.That(c.DescuentoGlobalMonto, Is.EqualTo(esperadoDcto));

            Assert.That(c.IgvTotal, Is.LessThan(igvAntes)); // IGV debe bajar
            Assert.That(c.Total, Is.EqualTo(Math.Round((c.SubtotalBase - c.DescuentoGlobalMonto) + c.IgvTotal, 2, MidpointRounding.AwayFromZero)));
        }

        [Test]
        public void Boleta_Total_NoPuedeExceder_700_Soles()
        {
            // Si la boleta tiene cliente SIN DNI (p. ej. RUC u otro), y total > 700 PEN, debe fallar
            var c = ComprobanteElectronico.CrearBorrador(
                tipo: Boleta(), emisor: Emisor(), cliente: ClienteRuc(),
                moneda: PEN(), emision: Hoy(), formaDePago: Contado(),
                vencimiento: VenceHoy(), usuarioEmisor: Usuario()
            );

            // Base 600, IGV 108, total 708 → debe fallar al recalcular totales
            Assert.That(() => AgregarLineaSimple(c, 600m, incluyeIgv: false, cant: 1m),
                Throws.TypeOf<ComprobantesElectronicosBC.Domain.Exceptions.ReglaDeNegocioException>());
        }

        [Test]
        public void Monto_Total_NoPuedeExceder_Maximo_Global()
        {
            var c = NuevoBorradorFacturaPEN_Contado_ClienteRuc();

            // Busca exceder 1'000,000 con IGV incluido
            Assert.That(() => AgregarLineaSimple(c, 900000m, incluyeIgv: false, cant: 1m),
                Throws.TypeOf<ComprobantesElectronicosBC.Domain.Exceptions.ReglaDeNegocioException>());
        }

        [Test]
        public void CorreosEnvio_MaximoCinco()
        {
            var c = NuevoBorradorBoletaPEN_Contado_ClienteDni();

            var cinco = new[]
            {
                Email.Create("a@x.com"), Email.Create("b@x.com"), Email.Create("c@x.com"),
                Email.Create("d@x.com"), Email.Create("e@x.com")
            };

            c.ReemplazarCorreosEnvio(cinco);
            Assert.That(c.CorreosEnvio.Count, Is.EqualTo(5));

            var seis = cinco.Concat(new[] { Email.Create("f@x.com") }).ToArray();
            Assert.That(() => c.ReemplazarCorreosEnvio(seis), Throws.TypeOf<ComprobantesElectronicosBC.Domain.Exceptions.ReglaDeNegocioException>());
        }

        [Test]
        public void Emitir_RequiereSerieNumero_Lineas_YReglasNormativas()
        {
            var c = NuevoBorradorFacturaPEN_Contado_ClienteRuc();

            c.AsignarSerieYNumero("F001", 1);
            Assert.That(() => c.Emitir(), Throws.TypeOf<ComprobantesElectronicosBC.Domain.Exceptions.ReglaDeNegocioException>()); // sin líneas

            AgregarLineaSimple(c, 100m);
            c.Emitir();

            Assert.That(c.Estado, Is.EqualTo(EstadoComprobante.Enviado));
            Assert.That(c.EnviadoEnUtc, Is.Not.Null);
            Assert.That(c.DomainEvents, Has.Some.InstanceOf<ComprobanteEnviadoDomainEvent>());
        }

        [Test]
        public void Emitir_EnMonedaExtranjera_RequiereTipoCambio()
        {
            var c = NuevoBorradorFacturaPEN_Contado_ClienteRuc();

            // Cambiamos a USD sin convertir precios (líneas deberán agregarse en USD luego)
            c.CambiarMoneda(USD(), factorConversion: 3.70m, factorEsDeMonedaActualAHaciaNueva: false, convertirPreciosDeLineas: false);

            // Agregamos línea en USD (coherente con moneda actual)
            AgregarLineaSimple(c, 100m);

            c.AsignarSerieYNumero("F001", 1);

            // Sin TC → error
            Assert.That(() => c.Emitir(), Throws.TypeOf<ComprobantesElectronicosBC.Domain.Exceptions.ReglaDeNegocioException>());

            // Con TC → OK
            c.EstablecerTipoCambio(TipoCambio.Create(PEN(), USD(), 3.70m, DateOnly.FromDateTime(DateTime.Now)));
            Assert.That(() => c.Emitir(), Throws.Nothing);
            Assert.That(c.Estado, Is.EqualTo(EstadoComprobante.Enviado));
        }

        [Test]
        public void CambiarMoneda_ConviertePrecios_DePENaUSD_DividendoYRecalcula()
        {
            var c = NuevoBorradorBoletaPEN_Contado_ClienteDni();

            AgregarLineaSimple(c, 100m, incluyeIgv: false, cant: 1m);
            var subtotalPen = c.SubtotalBase;
            var totalPen = c.Total;

            // PEN → USD (divide por TC)
            c.CambiarMoneda(
                nueva: USD(),
                factorConversion: 3.80m,
                factorEsDeMonedaActualAHaciaNueva: false,
                convertirPreciosDeLineas: true
            );

            Assert.That(c.Moneda.Codigo, Is.EqualTo("USD"));
            Assert.That(c.SubtotalBase, Is.Not.EqualTo(subtotalPen));
            Assert.That(c.Total, Is.Not.EqualTo(totalPen));
        }

        [Test]
        public void Transiciones_Corregir_Aceptado_Rechazado_Anulado_EmitenEventos()
        {
            var c = NuevoBorradorBoletaPEN_Contado_ClienteDni();

            AgregarLineaSimple(c, 50m);
            c.AsignarSerieYNumero("B001", 99);
            c.Emitir();

            Assert.That(c.DomainEvents, Has.Some.InstanceOf<ComprobanteEnviadoDomainEvent>());

            c.MarcarCorregir("Falta campo X");
            Assert.That(c.Estado, Is.EqualTo(EstadoComprobante.Corregir));
            Assert.That(c.DomainEvents, Has.Some.InstanceOf<ComprobanteObservadoDomainEvent>());

            // Re-emitimos
            c.Emitir();

            c.MarcarAceptado();
            Assert.That(c.Estado, Is.EqualTo(EstadoComprobante.Aceptado));
            Assert.That(c.DomainEvents.Any(e => e is ComprobanteAceptadoDomainEvent), Is.True);

            var fechaBaja = DateTimeOffset.UtcNow.AddMinutes(5);
            c.MarcarAnulado(fechaBaja);
            Assert.That(c.Estado, Is.EqualTo(EstadoComprobante.Anulado));
            Assert.That(c.DomainEvents, Has.Some.InstanceOf<ComprobanteAnuladoDomainEvent>());
        }

        [Test]
        public void ComandosDeEdicion_RestringidosFueraDeBorradorOCorregir()
        {
            var c = NuevoBorradorBoletaPEN_Contado_ClienteDni();

            AgregarLineaSimple(c, 10m);
            c.AsignarSerieYNumero("B001", 1);
            c.Emitir();

            // En ENVIADO no se edita (EnsureEditable)
            Assert.That(() => c.CambiarCliente(ClienteDni()), Throws.TypeOf<ComprobantesElectronicosBC.Domain.Exceptions.EstadoInvalidoException>());
            Assert.That(() => c.AgregarLinea(Prod(), UM_NIU(), Cant(1m), Importe(1m, c.Moneda), Gravado_IGV(), IGV18(), false),
                Throws.TypeOf<ComprobantesElectronicosBC.Domain.Exceptions.EstadoInvalidoException>());

            // Pasamos a corregir
            c.MarcarCorregir("ajuste");
            Assert.That(() => c.CambiarCliente(ClienteDni()), Throws.Nothing);
            Assert.That(() => c.AgregarLinea(Prod("NUEVO"), UM_NIU(), Cant(1m), Importe(12m, c.Moneda), Gravado_IGV(), IGV18(), false),
                Throws.Nothing);
        }
    }
}
