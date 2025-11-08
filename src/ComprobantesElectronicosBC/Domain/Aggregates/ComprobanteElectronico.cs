using System;
using System.Collections.Generic;
using System.Linq;
using SharedKernel.Events;
using SharedKernel.ValueObjects;
using ComprobantesElectronicosBC.Domain.Events;
using ComprobantesElectronicosBC.Domain.ValueObjects;
using ComprobantesElectronicosBC.Domain.Exceptions;
using ComprobantesElectronicosBC.Domain.Entities;
namespace ComprobantesElectronicosBC.Domain.Aggregates
{
    /// <summary>
    /// INFORMACIÓN DE NEGOCIO:
    /// - Al emitir el comprobante, el sistema solo asigna el correlativo (número).
    /// - La serie puede estar preconfigurada o ser seleccionada por el usuario antes de emitir.
    /// - La asignación de serie no vive aquí; solo validamos compatibilidad.
    /// - Si la moneda del comprobante es distinta a la local, se exige TipoCambio al emitir.
    /// - Retroactividad de emisión: Factura (01) hasta 3 días; Boleta (03) hasta 5 días (validación en <see cref="FechaEmision"/>).
    /// - Precios/cantidades son editables y recalculan en línea; descuentos por línea y global son opcionales.
    /// </summary>
    public partial class ComprobanteElectronico
    {
        /// <summary>Tipo de cambio aplicado al comprobante (solo si la moneda es extranjera).</summary>
        public TipoCambio? TipoCambio { get; private set; }

        /// <summary>
        /// Establece el tipo de cambio (p.ej., cuando usuario cambia la moneda del formulario a USD).
        /// No convierte montos por sí mismo; para conversión usa <see cref="CambiarMoneda"/>.
        /// </summary>
        public void EstablecerTipoCambio(TipoCambio tipoCambio)
        {
            EnsureEditable();
            if (tipoCambio is null) throw new ArgumentNullException(nameof(tipoCambio));
            TipoCambio = tipoCambio;
        }

        /// <summary>Quita el tipo de cambio (p.ej., si regresa a moneda local).</summary>
        public void QuitarTipoCambio()
        {
            EnsureEditable();
            TipoCambio = null;
        }
    }

    /// <summary>Ciclo de vida del CPE dentro de ComprobantesElectronicosBC.</summary>
    public enum EstadoComprobante : short
    {
        Borrador = 0,
        Enviado = 1,
        Corregir = 2,
        Aceptado = 3,
        Rechazado = 4,
        Anulado = 5
    }

    public static class EstadoComprobanteInfo
    {
        public static string Codigo(this EstadoComprobante e) => e switch
        {
            EstadoComprobante.Borrador  => "DRAFT",
            EstadoComprobante.Enviado   => "SENT",
            EstadoComprobante.Corregir  => "NEEDS_CORRECTION",
            EstadoComprobante.Aceptado  => "ACCEPTED",
            EstadoComprobante.Rechazado => "REJECTED",
            EstadoComprobante.Anulado   => "CANCELLED",
            _ => "UNKNOWN"
        };

        public static bool PuedeEditar(this EstadoComprobante e)
            => e is EstadoComprobante.Borrador or EstadoComprobante.Corregir;

        public static bool PuedeEmitir(this EstadoComprobante e)
            => e is EstadoComprobante.Borrador or EstadoComprobante.Corregir;

        public static bool EsFinal(this EstadoComprobante e)
            => e is EstadoComprobante.Aceptado or EstadoComprobante.Rechazado or EstadoComprobante.Anulado;
    }

    /// <summary>Aggregate raíz del Bounded Context ComprobantesElectronicosBC.</summary>
    public sealed partial class ComprobanteElectronico
    {
        // --------------------- Domain events ---------------------
        private readonly List<IDomainEvent> _domainEvents = new();
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        // --------------------- Identidad y estado ----------------
        public Guid ComprobanteId { get; }
        /// <summary>Versión para control de concurrencia optimista.</summary>
        public int Version { get; internal set; }
        public EstadoComprobante Estado { get; private set; } = EstadoComprobante.Borrador;
        public string EstadoCodigo => Estado.Codigo();

        // Identidad multi-tenant (copiada desde EmisorSnapshot para facilitar queries/seguridad)
        public EmpresaId EmpresaId { get; }
        public TenantId TenantId { get; }
        public EstablecimientoId EstablecimientoId { get; }

        // --------------------- Cabecera (Value Objects) ----------
        public TipoDeComprobante Tipo { get; private set; }
        public SerieYNumero? SerieNumero { get; private set; } // El correlativo se asigna al emitir
        public FechaEmision Emision { get; private set; }
        public FechaVencimiento Vencimiento { get; private set; }
    public FormaDePago FormaDePago { get; private set; }
        public Moneda Moneda { get; private set; }

        public EmisorSnapshot Emisor { get; }
        public UsuarioSnapshot UsuarioEmisor { get; }
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

        // --------------------- Detalle (líneas) ------------------
        private readonly List<ComprobanteLinea> _lineas = new();
        public IReadOnlyList<ComprobanteLinea> Lineas => _lineas.AsReadOnly();

        // --------------------- Descuentos y totales --------------
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

        // --------------------- Auditoría mínima ------------------
        public DateTimeOffset CreadoEnUtc { get; }
        public DateTimeOffset? EnviadoEnUtc { get; private set; }
        public DateTimeOffset? AceptadoEnUtc { get; private set; }
        public DateTimeOffset? AnuladoEnUtc { get; private set; }

        public string? UltimoErrorTecnico { get; private set; }
        public string? UltimoCdrCodigo { get; private set; }
        public string? UltimoCdrDescripcion { get; private set; }

        // --------------------- Constructores / fábricas ----------
        private ComprobanteElectronico(
            Guid id,
            TipoDeComprobante tipo,
            EmisorSnapshot emisor,
            ClienteSnapshot cliente,
            Moneda moneda,
            FechaEmision emision,
            FormaDePago formaDePago,
            FechaVencimiento vencimiento,
            UsuarioSnapshot usuarioEmisor,
            DateTimeOffset creadoUtc)
        {
            ComprobanteId = id == Guid.Empty ? Guid.NewGuid() : id;

            Tipo          = tipo ?? throw new ArgumentNullException(nameof(tipo));
            Emisor        = emisor ?? throw new ArgumentNullException(nameof(emisor));
            Cliente       = cliente ?? throw new ArgumentNullException(nameof(cliente));
            Moneda        = moneda ?? throw new ArgumentNullException(nameof(moneda));
            Emision       = emision ?? throw new ArgumentNullException(nameof(emision));
            FormaDePago   = formaDePago ?? throw new ArgumentNullException(nameof(formaDePago));
            Vencimiento   = vencimiento ?? throw new ArgumentNullException(nameof(vencimiento));
            UsuarioEmisor = usuarioEmisor ?? throw new ArgumentNullException(nameof(usuarioEmisor));

            // Copia de identidad multi-tenant para facilitar queries/policies
            EmpresaId         = Emisor.EmpresaId;
            TenantId          = Emisor.TenantId;
            EstablecimientoId = Emisor.EstablecimientoId;

            // Regla mínima: en CONTADO, Vencimiento == Emision
            if (FormaDePago.EsContado && !Vencimiento.EsMismoDiaQue(Emision.Fecha))
                throw new ComprobantesElectronicosBC.Domain.Exceptions.EstadoInvalidoException("En CONTADO el vencimiento debe ser el mismo día de la emisión.");

            CreadoEnUtc = creadoUtc;
            Version = 0;
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
            UsuarioSnapshot usuarioEmisor,
            DateTimeOffset? ahoraUtc = null)
        {
            var now = ahoraUtc ?? DateTimeOffset.UtcNow;
            return new ComprobanteElectronico(Guid.NewGuid(), tipo, emisor, cliente, moneda, emision, formaDePago, vencimiento, usuarioEmisor, now);
        }

        // --------------------- Mutaciones de cabecera -----------
        public void AsignarSerieYNumero(SerieYNumero serieNumero)
        {
            EnsureEditable();
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
            EnsureEditable();
            Cliente = nuevo ?? throw new ArgumentNullException(nameof(nuevo));
        }

    public void CambiarFormaDePago(FormaDePago forma, int? diasCredito = null)
        {
            EnsureEditable();
            if (forma is null) throw new ArgumentNullException(nameof(forma));
            Vencimiento = FechaVencimiento.ParaFormaDePago(forma, Emision.Fecha, diasCredito);
            FormaDePago = forma;
        }

        public void CambiarVencimiento(FechaVencimiento nuevo)
        {
            EnsureEditable();
            if (nuevo is null) throw new ArgumentNullException(nameof(nuevo));
            if (FormaDePago.EsContado && !nuevo.EsMismoDiaQue(Emision.Fecha))
                throw new ComprobantesElectronicosBC.Domain.Exceptions.EstadoInvalidoException("En CONTADO el vencimiento debe ser igual a la emisión.");
            Vencimiento = nuevo;
        }

        public void CambiarObservaciones(Observaciones? obs)
        {
            EnsureEditable();
            Observaciones = obs;
        }

        public void CambiarCentroDeCosto(CentroDeCosto? cc)
        {
            EnsureEditable();
            CentroDeCosto = cc;
        }

        public void CambiarNumeroGuia(NumeroGuiaRemision? guia)
        {
            EnsureEditable();
            NumeroGuiaRemision = guia;
        }

        public void CambiarNumeroOrdenCompra(NumeroOrdenCompra? oc)
        {
            EnsureEditable();
            NumeroOrdenCompra = oc;
        }

        /// <summary>Reemplaza correos de envío (máx. 5).</summary>
        public void ReemplazarCorreosEnvio(IReadOnlyList<Email> correos)
        {
            EnsureEditable();
            _correosEnvio.Clear();
            if (correos is { Count: > 0 })
            {
                if (correos.Count > 5)
                    throw new ComprobantesElectronicosBC.Domain.Exceptions.ReglaDeNegocioException("Máximo 5 correos de envío.");
                _correosEnvio.AddRange(correos);
            }
        }

        /// <summary>Agrega una nota interna (append-only). Puede permitirse en cualquier estado si así lo decides.</summary>
        public void AgregarNotaInterna(NotaInterna nota)
        {
            // Si deseas restringir a estados editables, descomenta:
            // EnsureEditable();
            if (nota is null) throw new ArgumentNullException(nameof(nota));
            _notas.Add(nota);
        }

        /// <summary>
        /// Cambia la moneda del comprobante. Si <paramref name="convertirPreciosDeLineas"/> es true,
        /// aplica el factor de conversión a los precios unitarios de todas las líneas.
        /// 
        /// NOTA: Para evitar suposiciones sobre el VO TipoCambio, aquí usamos un <paramref name="factorConversion"/>.
        /// Si el <paramref name="factorEsDeMonedaActualAHaciaNueva"/> es true, se interpreta que:
        ///     precioNuevo = precioActual * factorConversion
        /// En caso contrario:
        ///     precioNuevo = precioActual / factorConversion
        /// 
        /// Ejemplo estándar (PEN↔USD): si factor = 3.78 y estás pasando de PEN→USD, usa factorEsDeMonedaActualAHaciaNueva=false (divide).
        /// Si vas de USD→PEN, usa factorEsDeMonedaActualAHaciaNueva=true (multiplica).
        /// </summary>
        public void CambiarMoneda(
            Moneda nueva,
            decimal factorConversion,
            bool factorEsDeMonedaActualAHaciaNueva,
            bool convertirPreciosDeLineas = true)
        {
            EnsureEditable();
            if (nueva is null) throw new ArgumentNullException(nameof(nueva));
            if (nueva.Codigo == Moneda.Codigo) return;

            // Si se requiere conversión de precios de líneas, aplicarla
            if (convertirPreciosDeLineas)
            {
                if (factorConversion <= 0m)
                    throw new ArgumentOutOfRangeException(nameof(factorConversion), "El factor de conversión debe ser positivo.");

                foreach (var ln in _lineas)
                {
                    // Validar consistencia
                    if (!ln.PrecioUnitario.Moneda.Equals(Moneda))
                        throw new ComprobantesElectronicosBC.Domain.Exceptions.ReglaDeNegocioException("La moneda de una línea no coincide con la del comprobante antes de convertir.");

                    var actual = ln.PrecioUnitario.Monto;
                    var convertido = factorEsDeMonedaActualAHaciaNueva ? actual * factorConversion : actual / factorConversion;
                    var nuevoPrecio = ImporteMonetario.Create(Round2(convertido), nueva);
                    ln.CambiarPrecio(nuevoPrecio, ln.PrecioIncluyeIgv, permitirCambioDeMoneda: true); // permite cambio de moneda
                }
            }
            else
            {
                // Si no conviertes, asegúrate de que las líneas ya vengan en la nueva moneda
                if (_lineas.Any(l => !l.PrecioUnitario.Moneda.Equals(nueva)))
                    throw new ComprobantesElectronicosBC.Domain.Exceptions.ReglaDeNegocioException("Las líneas deben tener la misma moneda que el comprobante.");
            }

            Moneda = nueva;
            RecalcularTotales();
        }

        // --------------------- Líneas (agregar/editar/quitar) ---
        public int AgregarLinea(
            DescripcionProducto descripcion,
            UnidadDeMedida unidad,
            Cantidad cantidad,
            ImporteMonetario precioUnitario,
            AfectacionImpuesto afectacion,
            TasaImpuesto tasa,
            bool precioIncluyeIgv,
            DescuentoLinea? descuento = null,
            CentroDeCosto? centroDeCosto = null)
        {
            EnsureEditable();

            if (descripcion is null) throw new ArgumentNullException(nameof(descripcion));
            if (unidad is null) throw new ArgumentNullException(nameof(unidad));
            if (afectacion is null) throw new ArgumentNullException(nameof(afectacion));
            if (tasa is null) throw new ArgumentNullException(nameof(tasa));
            if (!precioUnitario.Moneda.Equals(Moneda))
                throw new ComprobantesElectronicosBC.Domain.Exceptions.ReglaDeNegocioException($"La moneda de la línea ({precioUnitario.Moneda.Codigo}) debe coincidir con la del documento ({Moneda.Codigo}).");

            var numeroLinea = _lineas.Count + 1;
            var linea = ComprobanteLinea.Create(
                numeroLinea,
                descripcion,
                unidad,
                cantidad,
                precioUnitario,
                precioIncluyeIgv,
                afectacion,
                tasa,
                descuento,
                centroDeCosto
            );
            _lineas.Add(linea);
            RecalcularTotales();
            return numeroLinea;
        }

        public void EditarLinea(
            int numeroLinea,
            DescripcionProducto? descripcion = null,
            UnidadDeMedida? unidad = null,
            Cantidad? cantidad = null,
            ImporteMonetario? precioUnitario = null,
            AfectacionImpuesto? afectacion = null,
            TasaImpuesto? tasa = null,
            bool? precioIncluyeIgv = null,
            DescuentoLinea? descuento = null,
            CentroDeCosto? centroDeCosto = null)
        {
            EnsureEditable();

            var ln = _lineas.FirstOrDefault(l => l.NumeroLinea == numeroLinea)
                     ?? throw new ArgumentException("No existe la línea indicada.", nameof(numeroLinea));

            if (precioUnitario is not null && !precioUnitario.Moneda.Equals(Moneda))
                throw new ComprobantesElectronicosBC.Domain.Exceptions.ReglaDeNegocioException($"La moneda de la línea ({precioUnitario.Moneda.Codigo}) debe coincidir con la del documento ({Moneda.Codigo}).");

            if (descripcion is not null) ln.CambiarDescripcion(descripcion);
            if (unidad is not null) ln.CambiarUnidad(unidad);
            if (cantidad is not null) ln.CambiarCantidad(cantidad.Value);
            if (precioUnitario is not null) ln.CambiarPrecio(precioUnitario, precioIncluyeIgv);

            // Permite cambios parciales de impuesto
            if (afectacion is not null && tasa is not null) ln.CambiarImpuesto(afectacion, tasa);
            else if (afectacion is not null)               ln.CambiarImpuesto(afectacion, ln.TasaImpuesto);
            else if (tasa is not null)                     ln.CambiarImpuesto(ln.AfectacionImpuesto, tasa);

            if (descuento is not null) ln.CambiarDescuento(descuento);
            if (centroDeCosto is not null) ln.CambiarCentroDeCosto(centroDeCosto);

            RecalcularTotales();
        }

        public void EliminarLinea(int numeroLinea)
        {
            EnsureEditable();

            var idx = _lineas.FindIndex(l => l.NumeroLinea == numeroLinea);
            if (idx == -1)
                throw new ArgumentException("No existe la línea indicada.", nameof(numeroLinea));

            _lineas.RemoveAt(idx);

            // Reasigna los números de línea para mantener la secuencia 1..N
            for (int i = 0; i < _lineas.Count; i++)
            {
                var src = _lineas[i];
                _lineas[i] = ComprobanteLinea.Create(
                    i + 1,
                    src.Descripcion,
                    src.UM,
                    src.Cantidad,
                    src.PrecioUnitario,
                    src.PrecioIncluyeIgv,
                    src.AfectacionImpuesto,
                    src.TasaImpuesto,
                    src.Descuento,
                    src.CentroDeCosto
                );
            }

            RecalcularTotales();
        }

        // --------------------- Descuento global y totales -------
        public void CambiarDescuentoGlobal(DescuentoGlobal nuevo)
        {
            EnsureEditable();
            DescuentoGlobal = nuevo ?? throw new ArgumentNullException(nameof(nuevo));
            RecalcularTotales();
        }

        private void RecalcularTotales()
        {
            var t = Services.ComprobanteTotalesService.Calcular(_lineas, DescuentoGlobal);
            SubtotalBase = t.SubtotalBase;
            DescuentoGlobalMonto = t.DescuentoGlobalMonto;
            IgvTotal = t.IgvTotal;
            Total = t.Total;
        }

        // --------------------- Transiciones de estado ----------
        /// <summary>
        /// Pasa de Borrador/Corregir → Enviado. Requiere Serie/Número y al menos una línea.
        /// También valida: Factura → RUC de cliente; moneda extranjera → TipoCambio obligatorio.
        /// </summary>
        public void Emitir()
        {
            if (!Estado.PuedeEmitir())
                throw new ComprobantesElectronicosBC.Domain.Exceptions.EstadoInvalidoException("Solo BORRADOR o CORREGIR pueden emitirse.");
            if (SerieNumero is null)
                throw new ComprobantesElectronicosBC.Domain.Exceptions.ReglaDeNegocioException("Asigne Serie y Número antes de emitir.");
            if (_lineas.Count == 0)
                throw new ComprobantesElectronicosBC.Domain.Exceptions.ReglaDeNegocioException("Debe existir al menos una línea antes de emitir.");

            // Guards normativos
            if (Tipo.RequiereRucCliente && !Cliente.EsRuc)
                throw new ComprobantesElectronicosBC.Domain.Exceptions.ReglaDeNegocioException("Para Factura (01) el cliente debe tener RUC.");
            if (Moneda.Codigo != "PEN" && TipoCambio is null)
                throw new ComprobantesElectronicosBC.Domain.Exceptions.ReglaDeNegocioException("Tipo de cambio obligatorio para moneda extranjera.");

            Estado = EstadoComprobante.Enviado;
            EnviadoEnUtc = DateTimeOffset.UtcNow;
            UltimoErrorTecnico = null;
            UltimoCdrCodigo = null;
            UltimoCdrDescripcion = null;

            // Emit domain event (enviado)
            _domainEvents.Add(new ComprobanteEnviadoDomainEvent(EmpresaId, EstablecimientoId, ComprobanteId, EnviadoEnUtc.Value.UtcDateTime));
        }

        /// <summary>Pasa de Enviado → Corregir (error recuperable).</summary>
        public void MarcarCorregir(string detalleError)
        {
            if (Estado != EstadoComprobante.Enviado)
                throw new ComprobantesElectronicosBC.Domain.Exceptions.EstadoInvalidoException("Solo un comprobante ENVIADO puede pasar a CORREGIR.");

            UltimoErrorTecnico = string.IsNullOrWhiteSpace(detalleError) ? "Error no especificado" : detalleError.Trim();
            Estado = EstadoComprobante.Corregir;

            // Evento: Observado (corrección requerida)
            _domainEvents.Add(new ComprobanteObservadoDomainEvent(EmpresaId, EstablecimientoId, ComprobanteId, UltimoErrorTecnico, DateTimeOffset.UtcNow.UtcDateTime));
        }

        /// <summary>Pasa de Enviado → Aceptado.</summary>
        public void MarcarAceptado()
        {
            EnsurePuedeAceptar();
            Estado = EstadoComprobante.Aceptado;
            AceptadoEnUtc = DateTimeOffset.UtcNow;

            // Evento: Aceptado (se respeta namespace/nombre que ya usas)
            _domainEvents.Add(new ComprobanteAceptadoDomainEvent(
                EmpresaId,
                EstablecimientoId,
                ComprobanteId,
                AceptadoEnUtc.Value.UtcDateTime,
                UltimoCdrDescripcion));
        }

        /// <summary>Pasa de Enviado → Aceptado con fecha específica (wrapper de compatibilidad).</summary>
        public void MarcarAceptado(DateTimeOffset aceptadoEnUtc)
        {
            EnsurePuedeAceptar();
            Estado = EstadoComprobante.Aceptado;
            AceptadoEnUtc = aceptadoEnUtc;

            _domainEvents.Add(new ComprobanteAceptadoDomainEvent(
                EmpresaId,
                EstablecimientoId,
                ComprobanteId,
                AceptadoEnUtc.Value.UtcDateTime,
                UltimoCdrDescripcion));
        }

        /// <summary>Pasa de Enviado → Rechazado (CDR 2000–3999).</summary>
        public void MarcarRechazado(string codigoCdr, string descripcion)
        {
            if (Estado != EstadoComprobante.Enviado)
                throw new ComprobantesElectronicosBC.Domain.Exceptions.EstadoInvalidoException("Solo un comprobante ENVIADO puede marcarse RECHAZADO.");

            UltimoCdrCodigo = string.IsNullOrWhiteSpace(codigoCdr) ? null : codigoCdr.Trim();
            UltimoCdrDescripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim();
            Estado = EstadoComprobante.Rechazado;

            _domainEvents.Add(new ComprobanteRechazadoDomainEvent(
                EmpresaId,
                EstablecimientoId,
                ComprobanteId,
                UltimoCdrCodigo ?? string.Empty,
                UltimoCdrDescripcion ?? string.Empty,
                DateTimeOffset.UtcNow.UtcDateTime));
        }

        /// <summary>Pasa de Aceptado → Anulado (RA aceptada).</summary>
        public void MarcarAnulado(DateTimeOffset cdrBajaEnUtc)
        {
            if (Estado != EstadoComprobante.Aceptado)
                throw new ComprobantesElectronicosBC.Domain.Exceptions.EstadoInvalidoException("Solo un comprobante ACEPTADO puede anularse por baja.");

            Estado = EstadoComprobante.Anulado;
            AnuladoEnUtc = cdrBajaEnUtc;

            _domainEvents.Add(new ComprobanteAnuladoDomainEvent(
                EmpresaId,
                EstablecimientoId,
                ComprobanteId,
                AnuladoEnUtc.Value.UtcDateTime));
        }

        private void EnsurePuedeAceptar()
        {
            if (Estado != EstadoComprobante.Enviado)
                throw new ComprobantesElectronicosBC.Domain.Exceptions.EstadoInvalidoException("Solo un comprobante ENVIADO puede marcarse como Aceptado.");
            if (SerieNumero is null)
                throw new ComprobantesElectronicosBC.Domain.Exceptions.ReglaDeNegocioException("Debe existir Serie y Número antes de la aceptación.");
            if (_lineas.Count == 0)
                throw new ComprobantesElectronicosBC.Domain.Exceptions.ReglaDeNegocioException("Debe existir al menos una línea antes de la aceptación.");
        }

        private void EnsureEditable()
        {
            if (!Estado.PuedeEditar())
                throw new ComprobantesElectronicosBC.Domain.Exceptions.EstadoInvalidoException("Solo en BORRADOR o CORREGIR puede editarse el comprobante.");
        }

        // --------------------- Helpers -------------------------
        private static decimal Round2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
        private static decimal Round6(decimal v) => Math.Round(v, 6, MidpointRounding.AwayFromZero);

        /// <summary>Permite al application service drenar eventos tras persistir/publicar.</summary>
        public IReadOnlyCollection<IDomainEvent> DrainDomainEvents()
        {
            var copy = _domainEvents.ToArray();
            _domainEvents.Clear();
            return copy;
        }
    }
}
