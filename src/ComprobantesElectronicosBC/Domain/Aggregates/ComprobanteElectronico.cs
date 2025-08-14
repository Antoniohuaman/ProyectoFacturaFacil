using System;
using System.Collections.Generic;
using System.Linq;
using ComprobantesElectronicosBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace ComprobantesElectronicosBC.Domain.Aggregates
{
    /// <summary>
    /// Ciclo de vida del CPE dentro de ComprobantesElectronicosBC.
    /// </summary>
    public enum EstadoComprobante : short
    {
        /// <summary>Fase de preparación. Editable. Sin envío ni correlativo obligatorio.</summary>
        Borrador = 0,

        /// <summary>Enviado al API Service (JSON construido y correlativo asignado); esperando CDR.</summary>
        Enviado = 1,

        /// <summary>Requiere corrección por error técnico o de validación. Editable para reenviar.</summary>
        Corregir = 2,

        /// <summary>SUNAT acepta (CDR OK). Estado final; no editable.</summary>
        Aceptado = 3,

        /// <summary>SUNAT rechaza (CDR 2000–3999). Estado final; no editable.</summary>
        Rechazado = 4,

        /// <summary>Baja confirmada por SUNAT (CDR de baja/RA). Estado final.</summary>
        Anulado = 5
    }

    /// <summary>Utilidades de reglas/etiquetas/códigos para persistencia y UI.</summary>
    public static class EstadoComprobanteInfo
    {
        // Códigos canónicos (útiles si prefieres guardar texto estable)
        public static string Codigo(this EstadoComprobante e) => e switch
        {
            EstadoComprobante.Borrador  => "DRAFT",
            EstadoComprobante.Enviado   => "SENT",               // PendingValidation
            EstadoComprobante.Corregir  => "NEEDS_CORRECTION",
            EstadoComprobante.Aceptado  => "ACCEPTED",
            EstadoComprobante.Rechazado => "REJECTED",
            EstadoComprobante.Anulado   => "CANCELLED",
            _ => "UNKNOWN"
        };

        // Reglas de UI
        public static bool PuedeEditar(this EstadoComprobante e)
            => e is EstadoComprobante.Borrador or EstadoComprobante.Corregir;

        public static bool PuedeEmitir(this EstadoComprobante e)
            => e is EstadoComprobante.Borrador or EstadoComprobante.Corregir;

        public static bool EsFinal(this EstadoComprobante e)
            => e is EstadoComprobante.Aceptado or EstadoComprobante.Rechazado or EstadoComprobante.Anulado;
    }

    /// <summary>
    /// Aggregate raíz del Bounded Context ComprobantesElectronicosBC.
    /// Modela la preparación del CPE (Factura/Boleta) listo para firmar/enviar por el servicio externo.
    /// </summary>
    public sealed partial class ComprobanteElectronico
    {
        #region Identidad y estado
        public Guid ComprobanteId { get; }
        public EstadoComprobante Estado { get; private set; } = EstadoComprobante.Borrador;
        public string EstadoCodigo => Estado.Codigo();
        #endregion

        #region Cabecera (Value Objects)
        public TipoDeComprobante Tipo { get; private set; }
        public SerieYNumero? SerieNumero { get; private set; } // se asigna al emitir
        public FechaEmision Emision { get; private set; }
        public FechaVencimiento Vencimiento { get; private set; }
        public FormaDePago FormaDePago { get; private set; }
        public Moneda Moneda { get; private set; }

        public EmisorSnapshot Emisor { get; }
        public ClienteSnapshot Cliente { get; private set; }

    public CentroDeCosto? CentroDeCosto { get; private set; }
        public Observaciones? Observaciones { get; private set; }
        public NumeroGuiaRemision? NumeroGuiaRemision { get; private set; }
        public NumeroOrdenCompra? NumeroOrdenCompra { get; private set; }

        /// <summary>Correos de envío (0..5). Se normalizan con <see cref="Email.ParseListOrEmpty"/>.</summary>
        public IReadOnlyList<Email> CorreosEnvio => _correosEnvio.AsReadOnly();
        private readonly List<Email> _correosEnvio = new();

        /// <summary>Notas internas (append-only).</summary>
        public IReadOnlyList<NotaInterna> NotasInternas => _notas.AsReadOnly();
        private readonly List<NotaInterna> _notas = new();
        #endregion

        #region Detalle (líneas)
        public IReadOnlyList<LineaDetalle> Lineas => _lineas.AsReadOnly();
        private readonly List<LineaDetalle> _lineas = new();
        #endregion

        #region Descuentos y totales (moneda del documento)
        public DescuentoGlobal DescuentoGlobal { get; private set; } = DescuentoGlobal.None;

        /// <summary>Suma de bases imponibles tras descuento de línea, antes de descuento global.</summary>
        public decimal SubtotalBase { get; private set; }

        /// <summary>Monto calculado del descuento global (0 si None).</summary>
        public decimal DescuentoGlobalMonto { get; private set; }

        /// <summary>IGV total tras aplicar descuento global prorrateado por línea.</summary>
        public decimal IgvTotal { get; private set; }

        /// <summary>Total = BaseNeta + IGV.</summary>
        public decimal Total { get; private set; }

        // Exposición monetaria cómoda
        public ImporteMonetario ImporteSubtotalBase => ImporteMonetario.Create(SubtotalBase, Moneda);
        public ImporteMonetario ImporteDescuentoGlobal => ImporteMonetario.Create(DescuentoGlobalMonto, Moneda);
        public ImporteMonetario ImporteIgvTotal => ImporteMonetario.Create(IgvTotal, Moneda);
        public ImporteMonetario ImporteTotal => ImporteMonetario.Create(Total, Moneda);
        #endregion

        #region Auditoría mínima
        public DateTimeOffset CreadoEnUtc { get; }
        public DateTimeOffset? EnviadoEnUtc { get; private set; }
        public DateTimeOffset? AceptadoEnUtc { get; private set; }
        public DateTimeOffset? AnuladoEnUtc { get; private set; }

        public string? UltimoErrorTecnico { get; private set; }
        public string? UltimoCdrCodigo { get; private set; }
        public string? UltimoCdrDescripcion { get; private set; }
        #endregion

        #region Constructores / fábricas
        private ComprobanteElectronico(
            Guid id,
            TipoDeComprobante tipo,
            EmisorSnapshot emisor,
            ClienteSnapshot cliente,
            Moneda moneda,
            FechaEmision emision,
            FormaDePago formaDePago,
            FechaVencimiento vencimiento,
            DateTimeOffset creadoUtc)
        {
            ComprobanteId = id == Guid.Empty ? Guid.NewGuid() : id;

            Tipo = tipo ?? throw new ArgumentNullException(nameof(tipo));
            Emisor = emisor ?? throw new ArgumentNullException(nameof(emisor));
            Cliente = cliente ?? throw new ArgumentNullException(nameof(cliente));
            Moneda = moneda ?? throw new ArgumentNullException(nameof(moneda));
            Emision = emision ?? throw new ArgumentNullException(nameof(emision));
            FormaDePago = formaDePago ?? throw new ArgumentNullException(nameof(formaDePago));
            Vencimiento = vencimiento ?? throw new ArgumentNullException(nameof(vencimiento));

            // Regla mínima: en CONTADO, Vencimiento == Emision
            if (FormaDePago.EsContado && !Vencimiento.EsMismoDiaQue(Emision.Fecha))
                throw new InvalidOperationException("En CONTADO el vencimiento debe ser el mismo día de la emisión.");

            CreadoEnUtc = creadoUtc;
        }

        /// <summary>Crea un comprobante en BORRADOR (sin envío).</summary>
        public static ComprobanteElectronico CrearBorrador(
            TipoDeComprobante tipo,
            EmisorSnapshot emisor,
            ClienteSnapshot cliente,
            Moneda moneda,
            FechaEmision emision,
            FormaDePago formaDePago,
            FechaVencimiento vencimiento,
            DateTimeOffset? ahoraUtc = null)
        {
            var now = ahoraUtc ?? DateTimeOffset.UtcNow;
            return new ComprobanteElectronico(Guid.NewGuid(), tipo, emisor, cliente, moneda, emision, formaDePago, vencimiento, now);
        }
        #endregion

        #region Mutaciones de cabecera
        public void AsignarSerieYNumero(SerieYNumero serieNumero)
        {
            if (serieNumero is null) throw new ArgumentNullException(nameof(serieNumero));
            // Convención F*/B* respecto al tipo
            Tipo.ValidarCompatibilidadConSerie(serieNumero.Serie);
            SerieNumero = serieNumero;
        }

        /// <summary>Wrapper de compatibilidad con tests: 2 argumentos.</summary>
        public void AsignarSerieYNumero(string serie, int numero)
            => AsignarSerieYNumero(SerieYNumero.Create(serie, numero));

        public void CambiarCliente(ClienteSnapshot nuevo)
        {
            Cliente = nuevo ?? throw new ArgumentNullException(nameof(nuevo));
        }

        public void CambiarFormaDePago(FormaDePago forma, int? diasCredito = null)
        {
            if (forma is null) throw new ArgumentNullException(nameof(forma));
            Vencimiento = FechaVencimiento.ParaFormaDePago(forma, Emision.Fecha, diasCredito);
            FormaDePago = forma;
        }

        public void CambiarVencimiento(FechaVencimiento nuevo)
        {
            if (nuevo is null) throw new ArgumentNullException(nameof(nuevo));
            if (FormaDePago.EsContado && !nuevo.EsMismoDiaQue(Emision.Fecha))
                throw new InvalidOperationException("En CONTADO el vencimiento debe ser igual a la emisión.");
            Vencimiento = nuevo;
        }

        public void CambiarObservaciones(Observaciones? obs) => Observaciones = obs;
    public void CambiarCentroDeCosto(CentroDeCosto? cc) => CentroDeCosto = cc;
        public void CambiarNumeroGuia(NumeroGuiaRemision? guia) => NumeroGuiaRemision = guia;
        public void CambiarNumeroOrdenCompra(NumeroOrdenCompra? oc) => NumeroOrdenCompra = oc;

        public void ReemplazarCorreosEnvio(IReadOnlyList<Email> correos)
        {
            _correosEnvio.Clear();
            if (correos is { Count: > 0 }) _correosEnvio.AddRange(correos);
        }

        public void AgregarNotaInterna(NotaInterna nota)
        {
            if (nota is null) throw new ArgumentNullException(nameof(nota));
            _notas.Add(nota);
        }
        #endregion

        #region Líneas (agregar/editar/quitar)
        public Guid AgregarLinea(
            DescripcionProducto descripcion,
            UnidadDeMedida unidad,
            Cantidad cantidad,
            ImporteMonetario precioUnitario,
            ImpuestoIGV impuesto,
            bool precioIncluyeIgv,
            DescuentoLinea? descuento = null)
        {
            if (descripcion is null) throw new ArgumentNullException(nameof(descripcion));
            if (unidad is null) throw new ArgumentNullException(nameof(unidad));
            if (impuesto is null) throw new ArgumentNullException(nameof(impuesto));
            if (!precioUnitario.Moneda.Equals(Moneda))
                throw new InvalidOperationException($"La moneda de la línea ({precioUnitario.Moneda.Codigo}) debe coincidir con la del documento ({Moneda.Codigo}).");

            var linea = new LineaDetalle(
                id: Guid.NewGuid(),
                descripcion: descripcion,
                unidad: unidad,
                cantidad: cantidad,
                precioUnitario: precioUnitario,
                impuesto: impuesto,
                precioIncluyeIgv: precioIncluyeIgv,
                descuento: descuento ?? DescuentoLinea.None
            );

            _lineas.Add(linea);
            RecalcularTotales();
            return linea.LineaId;
        }

        public void EditarLinea(
            Guid lineaId,
            DescripcionProducto? descripcion = null,
            UnidadDeMedida? unidad = null,
            Cantidad? cantidad = null,
            ImporteMonetario? precioUnitario = null,
            ImpuestoIGV? impuesto = null,
            bool? precioIncluyeIgv = null,
            DescuentoLinea? descuento = null)
        {
            var ln = _lineas.FirstOrDefault(l => l.LineaId == lineaId)
                     ?? throw new ArgumentException("No existe la línea indicada.", nameof(lineaId));

            if (precioUnitario is not null && !precioUnitario.Moneda.Equals(Moneda))
                throw new InvalidOperationException($"La moneda de la línea ({precioUnitario.Moneda.Codigo}) debe coincidir con la del documento ({Moneda.Codigo}).");

            ln.Editar(descripcion, unidad, cantidad, precioUnitario, impuesto, precioIncluyeIgv, descuento);
            RecalcularTotales();
        }

        public void EliminarLinea(Guid lineaId)
        {
            var removed = _lineas.RemoveAll(l => l.LineaId == lineaId);
            if (removed == 0)
                throw new ArgumentException("No existe la línea indicada.", nameof(lineaId));
            RecalcularTotales();
        }
        #endregion

        #region Descuento global y totales
        public void CambiarDescuentoGlobal(DescuentoGlobal nuevo)
        {
            DescuentoGlobal = nuevo ?? throw new ArgumentNullException(nameof(nuevo));
            RecalcularTotales();
        }

        private void RecalcularTotales()
        {
            if (_lineas.Count == 0)
            {
                SubtotalBase = 0m;
                DescuentoGlobalMonto = 0m;
                IgvTotal = 0m;
                Total = 0m;
                return;
            }

            // 1) Montos por línea aplicando DESCUENTO DE LÍNEA
            var montosPorLinea = _lineas.Select(l => l.CalcularMontos()).ToList();

            // Bases separadas (después del descuento de línea)
            var baseTotal = montosPorLinea.Sum(m => m.BaseDespues);

            // 2) Descuento global sobre la BASE
            SubtotalBase = Round2(baseTotal);
            DescuentoGlobalMonto = Round2(DescuentoGlobal.CalcularMontoDescuento(SubtotalBase));
            var baseNeta = Round2(SubtotalBase - DescuentoGlobalMonto);

            // 3) Prorrateo del descuento global a cada línea para recalcular IGV correctamente
            decimal igvTotal = 0m;

            if (DescuentoGlobal.EsNinguno)
            {
                igvTotal = montosPorLinea.Sum(m => m.Igv);
            }
            else
            {
                for (int i = 0; i < _lineas.Count; i++)
                {
                    var linea = _lineas[i];
                    var m = montosPorLinea[i];

                    // Porción del descuento global que afecta a la base de ESTA línea
                    decimal share = DescuentoGlobal.Modo switch
                    {
                        DescuentoGlobalModo.Porcentaje => Round6(m.BaseDespues * DescuentoGlobal.Valor),
                        DescuentoGlobalModo.Monto      => SubtotalBase == 0m ? 0m : Round6(DescuentoGlobalMonto * (m.BaseDespues / SubtotalBase)),
                        _                              => 0m
                    };

                    var baseLineaTrasGlobal = Round2(m.BaseDespues - share);
                    var igvLinea = linea.Impuesto.EsGravado
                        ? Round2(baseLineaTrasGlobal * linea.Impuesto.IgvRate!.Value)
                        : 0m;

                    igvTotal += igvLinea;
                }
            }

            IgvTotal = Round2(igvTotal);
            Total = Round2(baseNeta + IgvTotal);
        }
        #endregion

        #region Transiciones de estado
        /// <summary>
        /// Pasa de Borrador/Corregir → Enviado. Requiere Serie/Número y al menos una línea.
        /// </summary>
        public void Emitir()
        {
            if (!Estado.PuedeEmitir())
                throw new InvalidOperationException("Solo BORRADOR o CORREGIR pueden emitirse.");
            if (SerieNumero is null)
                throw new InvalidOperationException("Asigne Serie y Número antes de emitir.");
            if (_lineas.Count == 0)
                throw new InvalidOperationException("Debe existir al menos una línea antes de emitir.");

            Estado = EstadoComprobante.Enviado;
            EnviadoEnUtc = DateTimeOffset.UtcNow;
            UltimoErrorTecnico = null;
            UltimoCdrCodigo = null;
            UltimoCdrDescripcion = null;
        }

        /// <summary>Pasa de Enviado → Corregir (error recuperable).</summary>
        public void MarcarCorregir(string detalleError)
        {
            if (Estado != EstadoComprobante.Enviado)
                throw new InvalidOperationException("Solo un comprobante ENVIADO puede pasar a CORREGIR.");
            UltimoErrorTecnico = string.IsNullOrWhiteSpace(detalleError) ? "Error no especificado" : detalleError.Trim();
            Estado = EstadoComprobante.Corregir;
        }

        /// <summary>Pasa de Enviado → Aceptado.</summary>
        public void MarcarAceptado()
        {
            EnsurePuedeAceptar();
            Estado = EstadoComprobante.Aceptado;
            AceptadoEnUtc = DateTimeOffset.UtcNow;
        }

        /// <summary>Pasa de Enviado → Aceptado con fecha específica (wrapper de compatibilidad).</summary>
        public void MarcarAceptado(DateTimeOffset aceptadoEnUtc)
        {
            EnsurePuedeAceptar();
            Estado = EstadoComprobante.Aceptado;
            AceptadoEnUtc = aceptadoEnUtc;
        }

        /// <summary>Pasa de Enviado → Rechazado (CDR 2000–3999).</summary>
        public void MarcarRechazado(string codigoCdr, string descripcion)
        {
            if (Estado != EstadoComprobante.Enviado)
                throw new InvalidOperationException("Solo un comprobante ENVIADO puede marcarse RECHAZADO.");
            UltimoCdrCodigo = string.IsNullOrWhiteSpace(codigoCdr) ? null : codigoCdr.Trim();
            UltimoCdrDescripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim();
            Estado = EstadoComprobante.Rechazado;
        }

        /// <summary>Pasa de Aceptado → Anulado (RA aceptada).</summary>
        public void MarcarAnulado(DateTimeOffset cdrBajaEnUtc)
        {
            if (Estado != EstadoComprobante.Aceptado)
                throw new InvalidOperationException("Solo un comprobante ACEPTADO puede anularse por baja.");
            Estado = EstadoComprobante.Anulado;
            AnuladoEnUtc = cdrBajaEnUtc;
        }

        private void EnsurePuedeAceptar()
        {
            if (Estado != EstadoComprobante.Enviado)
                throw new InvalidOperationException("Solo un comprobante ENVIADO puede marcarse como Aceptado.");
            if (SerieNumero is null)
                throw new InvalidOperationException("Debe existir Serie y Número antes de la aceptación.");
            if (_lineas.Count == 0)
                throw new InvalidOperationException("Debe existir al menos una línea antes de la aceptación.");
        }
        #endregion

        #region Helpers
        private static decimal Round2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
        private static decimal Round6(decimal v) => Math.Round(v, 6, MidpointRounding.AwayFromZero);
        #endregion

        // ============================================================
        // Entidad interna de línea (en el mismo archivo por claridad)
        // ============================================================
        public sealed class LineaDetalle
        {
            public Guid LineaId { get; }
            public DescripcionProducto Descripcion { get; private set; }
            public UnidadDeMedida Unidad { get; private set; }
            public Cantidad Cantidad { get; private set; }
            public ImporteMonetario PrecioUnitario { get; private set; }
            public bool PrecioIncluyeIgv { get; private set; }
            public ImpuestoIGV Impuesto { get; private set; }
            public DescuentoLinea Descuento { get; private set; }

            internal LineaDetalle(
                Guid id,
                DescripcionProducto descripcion,
                UnidadDeMedida unidad,
                Cantidad cantidad,
                ImporteMonetario precioUnitario,
                ImpuestoIGV impuesto,
                bool precioIncluyeIgv,
                DescuentoLinea descuento)
            {
                LineaId = id == Guid.Empty ? Guid.NewGuid() : id;
                Descripcion = descripcion ?? throw new ArgumentNullException(nameof(descripcion));
                Unidad = unidad ?? throw new ArgumentNullException(nameof(unidad));
                Cantidad = cantidad;
                PrecioUnitario = precioUnitario ?? throw new ArgumentNullException(nameof(precioUnitario));
                Impuesto = impuesto ?? throw new ArgumentNullException(nameof(impuesto));
                PrecioIncluyeIgv = precioIncluyeIgv;
                Descuento = descuento ?? DescuentoLinea.None;
            }

            internal void Editar(
                DescripcionProducto? descripcion,
                UnidadDeMedida? unidad,
                Cantidad? cantidad,
                ImporteMonetario? precioUnitario,
                ImpuestoIGV? impuesto,
                bool? precioIncluyeIgv,
                DescuentoLinea? descuento)
            {
                if (descripcion is not null) Descripcion = descripcion;
                if (unidad is not null) Unidad = unidad;
                if (cantidad is not null) Cantidad = cantidad.Value;
                if (precioUnitario is not null) PrecioUnitario = precioUnitario;
                if (impuesto is not null) Impuesto = impuesto;
                if (precioIncluyeIgv.HasValue) PrecioIncluyeIgv = precioIncluyeIgv.Value;
                if (descuento is not null) Descuento = descuento;
            }

            /// <summary>Montos de la línea después de aplicar el descuento de línea.</summary>
            internal DescuentoLinea.Resultado CalcularMontos()
                => Descuento.Aplicar(Impuesto, PrecioUnitario.Monto, Cantidad, PrecioIncluyeIgv);
        }
    }
}
