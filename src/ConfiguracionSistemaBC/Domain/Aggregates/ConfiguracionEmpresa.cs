using System;
using System.Collections.Generic;
using System.Linq;
using ConfiguracionSistemaBC.Domain.Events;
using ConfiguracionSistemaBC.Domain.ValueObjects;
using ConfiguracionSistemaBC.Domain.Entities;
using SharedKernel.ValueObjects;

namespace ConfiguracionSistemaBC.Domain.Aggregates
{
    /// <summary>
    /// Aggregate raíz que centraliza la configuración de una empresa (tenant).
    /// 
    /// - Representa el punto único de acceso y modificación para parámetros relevantes de la empresa.
    /// - Encapsula datos legales, preferencias y establecimientos.
    /// - Mantiene consistencia de reglas de negocio mediante métodos controlados.
    /// - DDD puro: no conoce Series (fueron extraídas al aggregate SerieComprobante).
    /// </summary>
    public sealed class ConfiguracionEmpresa
    {
        // ========= PUBLIC READ MODELS =========

        public sealed record EstablecimientoRead(
            Guid Id,
            string EmpresaId, // opaco, canonizado desde RUC
            string Codigo,
            string Nombre,
            DomicilioFiscal Direccion,
            bool Habilitado
        );

        // ---- READ MODELS (otros catálogos locales) ----
        public sealed record FormaPagoRead(
            Guid Id,
            string EmpresaId,
            FormaDePago Valor,      // VO con código SUNAT "10"/"20" y método (solo en CONTADO)
            string Nombre,          // visible en UI
            bool Visible,
            bool EsPorDefecto,
            bool EsSistema,
            int Orden
        );

        public sealed record UnidadMedidaRead(
            Guid Id,
            string EmpresaId,
            UnidadDeMedida Unidad,  // VO con código (p.ej., "NIU", "KGM")
            string Nombre,          // visible (p.ej., "UNIDAD")
            bool Visible,
            bool EsPorDefecto,
            bool EsSistema,
            int Orden
        );

        // ========= STATE =========

        public Ruc Ruc { get; private set; } = null!;
        public EmpresaId EmpresaId { get; private set; } = null!;
        public int Version { get; private set; }

        // Datos legales
        public string RazonSocial { get; private set; } = string.Empty;
        public string? NombreComercial { get; private set; }
        public DomicilioFiscal DireccionFiscal { get; private set; } = null!;

        // Parámetros base
        public Moneda MonedaBase { get; private set; } = Moneda.PEN();
        public AmbienteFe Ambiente { get; private set; } = AmbienteFe.PRUEBA;

        // Preferencias opcionales
        public Telefono Telefono { get; private set; } = Telefono.Vacio;
        public List<Email> Emails { get; private set; } = new();
        public PieDePagina PieDePagina { get; private set; } = PieDePagina.Vacio;
        public LogoImagen? Logo { get; private set; }
        public bool MostrarImagenEnComprobanteImpresa { get; private set; } = false;

        // ----- Establecimientos -----
        private readonly Dictionary<Guid, Establecimiento> _estById = new();
        private readonly Dictionary<string, Guid> _estByCodigo = new(StringComparer.OrdinalIgnoreCase);
        private Guid? _principalEstablecimientoId;

        // ----- Formas de Pago -----
        internal sealed class FormaPagoState
        {
            public Guid Id { get; init; }
            public FormaDePago Valor { get; set; } = null!;
            public string Nombre { get; set; } = string.Empty;
            public bool Visible { get; set; } = true;
            public bool EsPorDefecto { get; set; }
            public bool EsSistema { get; init; }
            public int Orden { get; set; }
        }

        private readonly Dictionary<Guid, FormaPagoState> _fpById = new();
        private readonly HashSet<string> _indexFormaPago = new(StringComparer.Ordinal); // (code|metodo|NOMBRE)
        private Guid? _formaPagoDefaultId;

        // ----- Unidades de Medida -----
        internal sealed class UnidadMedidaState
        {
            public Guid Id { get; init; }
            public UnidadDeMedida Unidad { get; set; } = null!;
            public string Nombre { get; set; } = string.Empty;
            public bool Visible { get; set; } = true;
            public bool EsPorDefecto { get; set; }
            public bool EsSistema { get; init; }
            public int Orden { get; set; }
        }

        private readonly Dictionary<Guid, UnidadMedidaState> _umById = new();
        private readonly HashSet<string> _indexUnidadCodigo = new(StringComparer.Ordinal);
        private Guid? _unidadMedidaDefaultId;

        // ========= CTOR PRIVADO =========
        private ConfiguracionEmpresa() { }

        /// <summary>Rehidratación desde persistencia.</summary>
        internal ConfiguracionEmpresa(
            Ruc ruc,
            EmpresaId empresaId,
            string razonSocial,
            string? nombreComercial,
            DomicilioFiscal direccionFiscal,
            Moneda monedaBase,
            AmbienteFe ambiente,
            Telefono telefono,
            List<Email> emails,
            PieDePagina pieDePagina,
            LogoImagen? logo,
            bool mostrarImagenEnComprobanteImpresa,
            Dictionary<Guid, Establecimiento> estById,
            Dictionary<string, Guid> estByCodigo,
            Guid? principalEstablecimientoId,
            // catálogos internos
            Dictionary<Guid, FormaPagoState> fpById,
            HashSet<string> indexFormaPago,
            Guid? formaPagoDefaultId,
            Dictionary<Guid, UnidadMedidaState> umById,
            HashSet<string> indexUnidadCodigo,
            Guid? unidadMedidaDefaultId
        )
        {
            Ruc = ruc;
            EmpresaId = empresaId;
            RazonSocial = razonSocial;
            NombreComercial = nombreComercial;
            DireccionFiscal = direccionFiscal;
            MonedaBase = monedaBase;
            Ambiente = ambiente;
            Telefono = telefono;
            Emails = emails;
            PieDePagina = pieDePagina;
            Logo = logo;
            MostrarImagenEnComprobanteImpresa = mostrarImagenEnComprobanteImpresa;

            _estById = new(estById);
            _estByCodigo = new(estByCodigo, StringComparer.OrdinalIgnoreCase);
            _principalEstablecimientoId = principalEstablecimientoId;

            _fpById = new(fpById);
            _indexFormaPago = new(indexFormaPago, StringComparer.Ordinal);
            _formaPagoDefaultId = formaPagoDefaultId;

            _umById = new(umById);
            _indexUnidadCodigo = new(indexUnidadCodigo, StringComparer.Ordinal);
            _unidadMedidaDefaultId = unidadMedidaDefaultId;
        }

        // ========= FACTORY =========

        public static ConfiguracionEmpresa RegistrarNueva(
            Ruc ruc,
            string razonSocial,
            DomicilioFiscal direccionFiscal,
            Moneda monedaBase)
        {
            if (ruc is null) throw new ArgumentNullException(nameof(ruc));
            if (string.IsNullOrWhiteSpace(razonSocial)) throw new ArgumentNullException(nameof(razonSocial));
            if (direccionFiscal is null) throw new ArgumentNullException(nameof(direccionFiscal));
            if (!direccionFiscal.EsPeru)
                throw new ArgumentException("Solo se soporta domicilio fiscal de Perú (PE).", nameof(direccionFiscal));

            var empresa = new ConfiguracionEmpresa
            {
                Ruc = ruc,
                EmpresaId = EmpresaId.From(ruc.Canonizado),
                RazonSocial = razonSocial.Trim(),
                DireccionFiscal = direccionFiscal,
                MonedaBase = monedaBase,
                Ambiente = AmbienteFe.PRUEBA,
                Telefono = Telefono.Vacio,
                Emails = new List<Email>(),
                PieDePagina = PieDePagina.FromTextoPlano("Gracias Por su Preferencia"),
                Logo = null
            };

            // Bootstrap: Establecimiento principal
            var estPrincipalId = empresa.RegistrarEstablecimiento("01", "Establecimiento Principal", direccionFiscal);
            empresa.EstablecerComoPrincipal(estPrincipalId);

            // Bootstrap: Formas de pago y Unidades (preconfiguradas en este aggregate)
            empresa.BootstrapFormasDePago();
            empresa.BootstrapUnidadesDeMedida();

            // Nota: ya NO se crean series aquí. Usa una política de aplicación
            // que reaccione a este evento para crear FE01/BE01 en SerieComprobante.
            empresa.AddDomainEvent(new ConfiguracionEmpresaRegistrada(
                empresa.EmpresaId,
                ruc,
                razonSocial.Trim(),
                direccionFiscal,
                monedaBase,
                DateTime.UtcNow
            ));

            return empresa;
        }

        // ========= CAMBIOS DE AMBIENTE =========

        public void CambiarAmbiente(AmbienteFe destino)
        {
            if (destino is null) throw new ArgumentNullException(nameof(destino));
            AmbienteFe.ValidarTransicion(Ambiente, destino);
            var ambienteAnterior = Ambiente;
            Ambiente = destino;
            Version++;
            AddDomainEvent(new ConfiguracionSistemaBC.Domain.Events.AmbienteCambiado(
                EmpresaId,
                ambienteAnterior.ToString(),
                destino.ToString()
            ));
        }

        // ========= DATOS LEGALES =========

        public void ActualizarDatosLegales(Ruc ruc, string razonSocial, DomicilioFiscal direccionFiscal, string? nombreComercial = null)
        {
            if (ruc is null) throw new ArgumentNullException(nameof(ruc));
            if (string.IsNullOrWhiteSpace(razonSocial)) throw new ArgumentNullException(nameof(razonSocial));
            if (direccionFiscal is null) throw new ArgumentNullException(nameof(direccionFiscal));
            if (!direccionFiscal.EsPeru)
                throw new ArgumentException("Solo se soporta domicilio fiscal de Perú (PE).", nameof(direccionFiscal));

            Ruc = ruc;
            EmpresaId = EmpresaId.From(ruc.Canonizado); // actualiza EmpresaId si cambia RUC
            RazonSocial = razonSocial.Trim();
            NombreComercial = string.IsNullOrWhiteSpace(nombreComercial) ? null : nombreComercial.Trim();
            DireccionFiscal = direccionFiscal;

            Version++;
            AddDomainEvent(new ConfiguracionEmpresaActualizada(
                EmpresaId,
                Ruc,
                RazonSocial,
                DireccionFiscal,
                NombreComercial,
                MonedaBase,
                Ambiente,
                DateTime.UtcNow
            ));
        }

        // ========= PREFERENCIAS OPCIONALES =========

        public void ReemplazarEmails(IEnumerable<Email> emails)
        {
            if (emails is null) throw new ArgumentNullException(nameof(emails));
            Emails = emails.ToList();
            Version++;
        }

        public void ReemplazarTelefono(Telefono telefono)
        {
            Telefono = telefono ?? Telefono.Vacio;
            Version++;
        }

        public void ActualizarPieDePagina(PieDePagina pie)
        {
            PieDePagina = pie ?? PieDePagina.Vacio;
            Version++;
        }

        public void EstablecerLogo(LogoImagen? logo)
        {
            Logo = logo;
            Version++;
        }

        public void CambiarMonedaBase(Moneda monedaBase)
        {
            MonedaBase = monedaBase ?? Moneda.PEN();
            Version++;
        }

        /// <summary>Mostrar imágenes de productos en impresión.</summary>
        public void ConfigurarMostrarImagenEnComprobanteImpresa(bool mostrar)
        {
            MostrarImagenEnComprobanteImpresa = mostrar;
            Version++;
        }

        // ========= ESTABLECIMIENTOS =========

    public Guid RegistrarEstablecimiento(string codigo, string nombre, DomicilioFiscal direccion)
        {
            if (string.IsNullOrWhiteSpace(codigo)) throw new ArgumentNullException(nameof(codigo));
            if (string.IsNullOrWhiteSpace(nombre)) throw new ArgumentNullException(nameof(nombre));
            if (direccion is null) throw new ArgumentNullException(nameof(direccion));
            if (!direccion.EsPeru)
                throw new ArgumentException("Solo se soporta domicilio fiscal de Perú (PE) para establecimientos.", nameof(direccion));
            if (_estByCodigo.ContainsKey(codigo.Trim()))
                throw new InvalidOperationException($"Ya existe un establecimiento con código \"{codigo}\".");

            var id = Guid.NewGuid();
            var canonCodigo = codigo.Trim();
            var est = new Establecimiento(
                EstablecimientoId.From(id),
                EmpresaId,
                nombre.Trim(),
                canonCodigo,
                direccion
            );
            _estById[id] = est;
            _estByCodigo[est.Codigo] = id;
            Version++;
            AddDomainEvent(new ConfiguracionSistemaBC.Domain.Events.EstablecimientoRegistrado(
                EmpresaId,
                canonCodigo
            ));
            return id;
        }

        public void EstablecerComoPrincipal(Guid id)
        {
            if (!_estById.ContainsKey(id))
                throw new KeyNotFoundException("Establecimiento no encontrado.");
            if (_principalEstablecimientoId is Guid prev && _estById.TryGetValue(prev, out var prevEst))
                prevEst.MarcarComoSecundario();

            _principalEstablecimientoId = id;
            _estById[id].MarcarComoPrincipal();
            Version++;
        }

        public EstablecimientoRead? ObtenerEstablecimientoPrincipal()
        {
            if (_principalEstablecimientoId is null) return null;
            return _estById.TryGetValue(_principalEstablecimientoId.Value, out var est)
                ? ToRead(est)
                : null;
        }

        public void RecodificarEstablecimiento(Guid id, string nuevoCodigo)
        {
            if (string.IsNullOrWhiteSpace(nuevoCodigo))
                throw new ArgumentNullException(nameof(nuevoCodigo));
            if (!_estById.TryGetValue(id, out var est))
                throw new KeyNotFoundException("Establecimiento no encontrado.");
            var canon = nuevoCodigo.Trim();
            if (_estByCodigo.TryGetValue(canon, out var otroId) && otroId != id)
                throw new InvalidOperationException($"Ya existe un establecimiento con código \"{canon}\".");
            _estByCodigo.Remove(est.Codigo);
            est.ActualizarDatos(est.Nombre, canon, est.Direccion);
            _estByCodigo[canon] = id;
            Version++;
        }

    public void ActualizarEstablecimiento(Guid id, string nombre, DomicilioFiscal direccion)
        {
            if (!_estById.TryGetValue(id, out var est))
                throw new KeyNotFoundException("Establecimiento no encontrado.");
            if (string.IsNullOrWhiteSpace(nombre)) throw new ArgumentNullException(nameof(nombre));
            if (direccion is null) throw new ArgumentNullException(nameof(direccion));
            if (!direccion.EsPeru)
                throw new ArgumentException("Solo se soporta domicilio fiscal de Perú (PE) para establecimientos.", nameof(direccion));
            est.ActualizarDatos(nombre.Trim(), est.Codigo, direccion);
            Version++;
        }

        public void EliminarEstablecimiento(Guid id)
        {
            if (!_estById.TryGetValue(id, out var est))
                throw new KeyNotFoundException("Establecimiento no encontrado.");

            // Si solo queda uno, lanzar excepción
            if (_estById.Count == 1)
                throw new InvalidOperationException("No se puede eliminar el único establecimiento restante.");

            // Regla local: si tiene gestiones vinculadas, no permitir.
            if (est.TieneGestionesVinculadas())
                throw new InvalidOperationException("No se puede eliminar el establecimiento porque tiene gestiones vinculadas.");

            // NOTA: La relación con Series está fuera del aggregate.
            // Una política de aplicación debe verificar/validar y eliminar/inhabilitar series del establecimiento.

            _estByCodigo.Remove(est.Codigo);
            _estById.Remove(id);
            Version++;
        }

        public EstablecimientoRead? BuscarEstablecimientoPorCodigo(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return null;
            return _estByCodigo.TryGetValue(codigo.Trim(), out var id)
                ? ToRead(_estById[id])
                : null;
        }

        public IReadOnlyList<EstablecimientoRead> ListarEstablecimientos()
            => _estById.Values.Select(ToRead).ToList();

        private EstablecimientoRead ToRead(Establecimiento est)
            => new(est.Id.Value, EmpresaId.Value, est.Codigo, est.Nombre, est.Direccion, est.Habilitado);

        // ========= FORMAS DE PAGO =========

        private static string FpIndexKey(FormaDePago fp, string nombre)
            => $"{fp.PaymentMeansCode}|{(fp.MetodoCodigo ?? "")}|{nombre.Trim().ToUpperInvariant()}";

        private FormaPagoRead ToRead(FormaPagoState st)
            => new(st.Id, EmpresaId.Value, st.Valor, st.Nombre, st.Visible, st.EsPorDefecto, st.EsSistema, st.Orden);

        private void BootstrapFormasDePago()
        {
            var orden = 1;
            AddFormaPagoSistema(FormaDePago.Contado(),            "Contado",               visible: true,  orden++, esPorDefecto: true);
            AddFormaPagoSistema(FormaDePago.ContadoEfectivo(),    "Efectivo",              visible: true,  orden++, esPorDefecto: false);
            AddFormaPagoSistema(FormaDePago.ContadoTarjeta(),     "Tarjeta",               visible: true,  orden++, esPorDefecto: false);
            AddFormaPagoSistema(FormaDePago.ContadoTransferencia(),"Transferencia",        visible: true,  orden++, esPorDefecto: false);
            AddFormaPagoSistema(FormaDePago.ContadoYape(),        "Yape",                  visible: true,  orden++, esPorDefecto: false);
            AddFormaPagoSistema(FormaDePago.ContadoPlin(),        "Plin",                  visible: true,  orden++, esPorDefecto: false);
            AddFormaPagoSistema(FormaDePago.ContadoDeposito(),    "Depósito en cuenta",    visible: true,  orden++, esPorDefecto: false);

            AddFormaPagoSistema(FormaDePago.Credito(), "Crédito 7 días",        visible: true,  orden++, esPorDefecto: false);
            AddFormaPagoSistema(FormaDePago.Credito(), "Crédito 15 días",       visible: true,  orden++, esPorDefecto: false);
            AddFormaPagoSistema(FormaDePago.Credito(), "Crédito 30 días",       visible: true,  orden++, esPorDefecto: false);
            AddFormaPagoSistema(FormaDePago.Credito(), "Crédito 45 días",       visible: true,  orden++, esPorDefecto: false);
            AddFormaPagoSistema(FormaDePago.Credito(), "Crédito 60 días",       visible: true,  orden++, esPorDefecto: false);
            AddFormaPagoSistema(FormaDePago.Credito(), "Crédito 30-60-90 días", visible: true,  orden++, esPorDefecto: false);
        }

        private Guid AddFormaPagoSistema(FormaDePago valor, string nombre, bool visible, int orden, bool esPorDefecto)
        {
            var id = Guid.NewGuid();
            var st = new FormaPagoState
            {
                Id = id,
                Valor = valor,
                Nombre = nombre.Trim(),
                Visible = visible,
                EsPorDefecto = false,
                EsSistema = true,
                Orden = orden
            };

            var key = FpIndexKey(st.Valor, st.Nombre);
            if (_indexFormaPago.Contains(key))
                throw new InvalidOperationException($"La forma de pago \"{st.Nombre}\" ya existe.");

            _fpById[id] = st;
            _indexFormaPago.Add(key);

            if (esPorDefecto) EstablecerFormaPagoPorDefecto(id);
            return id;
        }

        public IReadOnlyList<FormaPagoRead> ListarFormasDePago()
            => _fpById.Values.OrderBy(v => v.Orden).Select(ToRead).ToList();

        public FormaPagoRead? ObtenerFormaDePagoPorDefecto()
            => _formaPagoDefaultId is Guid id && _fpById.TryGetValue(id, out var st) ? ToRead(st) : null;

        public Guid AgregarFormaDePagoPersonalizada(FormaDePago valor, string nombre, bool visible = true, int? orden = null, bool esPorDefecto = false)
        {
            if (string.IsNullOrWhiteSpace(nombre)) throw new ArgumentNullException(nameof(nombre));

            var id = Guid.NewGuid();
            var st = new FormaPagoState
            {
                Id = id,
                Valor = valor,
                Nombre = nombre.Trim(),
                Visible = visible,
                EsPorDefecto = false,
                EsSistema = false,
                Orden = orden ?? (_fpById.Count + 1)
            };

            var key = FpIndexKey(st.Valor, st.Nombre);
            if (_indexFormaPago.Contains(key))
                throw new InvalidOperationException($"La forma de pago \"{st.Nombre}\" ya existe.");

            _fpById[id] = st;
            _indexFormaPago.Add(key);

            if (esPorDefecto) EstablecerFormaPagoPorDefecto(id);
            Version++;
            return id;
        }

        public void ActualizarFormaDePago(
            Guid id,
            FormaDePago? nuevoValor = null,
            string? nuevoNombre = null,
            bool? visible = null,
            int? nuevoOrden = null,
            bool? esPorDefecto = null)
        {
            if (!_fpById.TryGetValue(id, out var st))
                throw new KeyNotFoundException("Forma de pago no encontrada.");

            if (st.EsSistema && (nuevoValor is not null || !string.IsNullOrWhiteSpace(nuevoNombre)))
                throw new InvalidOperationException("No se puede editar valor o nombre de una forma de pago del sistema.");

            if (visible.HasValue && st.EsPorDefecto && visible.Value == false)
                throw new InvalidOperationException("No se puede ocultar la forma de pago por defecto.");

            var oldKey = FpIndexKey(st.Valor, st.Nombre);
            if (nuevoValor is not null || !string.IsNullOrWhiteSpace(nuevoNombre))
            {
                var newValor = nuevoValor ?? st.Valor;
                var newNombre = string.IsNullOrWhiteSpace(nuevoNombre) ? st.Nombre : nuevoNombre!.Trim();
                var newKey = FpIndexKey(newValor, newNombre);

                if (newKey != oldKey && _indexFormaPago.Contains(newKey))
                    throw new InvalidOperationException($"La forma de pago \"{newNombre}\" ya existe.");

                _indexFormaPago.Remove(oldKey);
                st.Valor = newValor;
                st.Nombre = newNombre;
                _indexFormaPago.Add(newKey);
            }

            if (visible.HasValue) st.Visible = visible.Value;
            if (nuevoOrden.HasValue) st.Orden = nuevoOrden.Value;

            if (esPorDefecto.HasValue)
            {
                if (esPorDefecto.Value) EstablecerFormaPagoPorDefecto(id);
                else if (st.EsPorDefecto) throw new InvalidOperationException("Para quitar el defecto, establece otra forma como defecto.");
            }

            Version++;
        }

        public void EstablecerFormaPagoPorDefecto(Guid id)
        {
            if (!_fpById.TryGetValue(id, out var st))
                throw new KeyNotFoundException("Forma de pago no encontrada.");
            if (!st.Visible)
                throw new InvalidOperationException("No se puede establecer por defecto una forma de pago oculta.");

            if (_formaPagoDefaultId is Guid prevId && _fpById.TryGetValue(prevId, out var prev))
                prev.EsPorDefecto = false;

            st.EsPorDefecto = true;
            _formaPagoDefaultId = id;
            Version++;
        }

        public void EliminarFormaDePago(Guid id)
        {
            if (!_fpById.TryGetValue(id, out var st))
                throw new KeyNotFoundException("Forma de pago no encontrada.");
            if (st.EsSistema)
                throw new InvalidOperationException("No se puede eliminar una forma de pago del sistema.");
            if (st.EsPorDefecto)
                throw new InvalidOperationException("No se puede eliminar la forma de pago por defecto.");

            _indexFormaPago.Remove(FpIndexKey(st.Valor, st.Nombre));
            _fpById.Remove(id);
            Version++;
        }

        // ========= UNIDADES DE MEDIDA =========

        private UnidadMedidaRead ToRead(UnidadMedidaState st)
            => new(st.Id, EmpresaId.Value, st.Unidad, st.Nombre, st.Visible, st.EsPorDefecto, st.EsSistema, st.Orden);

        private void BootstrapUnidadesDeMedida()
        {
            var orden = 1;
            AddUnidadSistema(UnidadDeMedida.NIU, "UNIDAD",    visible: true,  orden++, esPorDefecto: true);
            AddUnidadSistema(UnidadDeMedida.ZZ,  "SERVICIO",  visible: true,  orden++, esPorDefecto: false);
            AddUnidadSistema(UnidadDeMedida.KGM, "KILOGRAMO", visible: true,  orden++, esPorDefecto: false);
            AddUnidadSistema(new UnidadDeMedida("GRM", null), "GRAMO",        visible: true,  orden++, esPorDefecto: false);
            AddUnidadSistema(UnidadDeMedida.LTR, "LITRO",     visible: true,  orden++, esPorDefecto: false);
            AddUnidadSistema(UnidadDeMedida.MTR, "METRO",     visible: true,  orden++, esPorDefecto: false);

            AddUnidadSistema((UnidadDeMedida)"DZN", "DOCENA",   visible: true,  orden++, esPorDefecto: false);
            AddUnidadSistema((UnidadDeMedida)"JR",  "FRASCO",   visible: true,  orden++, esPorDefecto: false);
            AddUnidadSistema((UnidadDeMedida)"RO",  "ROLLO",    visible: true,  orden++, esPorDefecto: false);
            AddUnidadSistema((UnidadDeMedida)"SET", "JUEGO",    visible: true,  orden++, esPorDefecto: false);
            AddUnidadSistema((UnidadDeMedida)"PA",  "PAQUETE",  visible: true,  orden++, esPorDefecto: false);
            AddUnidadSistema((UnidadDeMedida)"SA",  "SACO",     visible: true,  orden++, esPorDefecto: false);
        }

        private Guid AddUnidadSistema(UnidadDeMedida unidad, string nombre, bool visible, int orden, bool esPorDefecto)
        {
            var id = Guid.NewGuid();
            var st = new UnidadMedidaState
            {
                Id = id,
                Unidad = unidad,
                Nombre = nombre.Trim(),
                Visible = visible,
                EsPorDefecto = false,
                EsSistema = true,
                Orden = orden
            };

            if (_indexUnidadCodigo.Contains(st.Unidad.Codigo))
                throw new InvalidOperationException($"La unidad de medida \"{st.Unidad.Codigo}\" ya existe.");

            _umById[id] = st;
            _indexUnidadCodigo.Add(st.Unidad.Codigo);

            if (esPorDefecto) EstablecerUnidadDeMedidaPorDefecto(id);
            return id;
        }

        public IReadOnlyList<UnidadMedidaRead> ListarUnidadesDeMedida()
            => _umById.Values.OrderBy(v => v.Orden).Select(ToRead).ToList();

        public UnidadMedidaRead? ObtenerUnidadDeMedidaPorDefecto()
            => _unidadMedidaDefaultId is Guid id && _umById.TryGetValue(id, out var st) ? ToRead(st) : null;

        public Guid AgregarUnidadDeMedidaPersonalizada(UnidadDeMedida unidad, string nombre, bool visible = true, int? orden = null, bool esPorDefecto = false)
        {
            if (string.IsNullOrWhiteSpace(nombre)) throw new ArgumentNullException(nameof(nombre));

            var id = Guid.NewGuid();
            var st = new UnidadMedidaState
            {
                Id = id,
                Unidad = unidad,
                Nombre = nombre.Trim(),
                Visible = visible,
                EsPorDefecto = false,
                EsSistema = false,
                Orden = orden ?? (_umById.Count + 1)
            };

            if (_indexUnidadCodigo.Contains(st.Unidad.Codigo))
                throw new InvalidOperationException($"La unidad de medida \"{st.Unidad.Codigo}\" ya existe.");

            _umById[id] = st;
            _indexUnidadCodigo.Add(st.Unidad.Codigo);

            if (esPorDefecto) EstablecerUnidadDeMedidaPorDefecto(id);
            Version++;
            return id;
        }

        public void ActualizarUnidadDeMedida(
            Guid id,
            UnidadDeMedida? nuevaUnidad = null,
            string? nuevoNombre = null,
            bool? visible = null,
            int? nuevoOrden = null,
            bool? esPorDefecto = null)
        {
            if (!_umById.TryGetValue(id, out var st))
                throw new KeyNotFoundException("Unidad de medida no encontrada.");

            if (st.EsSistema && (nuevaUnidad is not null || !string.IsNullOrWhiteSpace(nuevoNombre)))
                throw new InvalidOperationException("No se puede editar código o nombre de una unidad del sistema.");

            if (visible.HasValue && st.EsPorDefecto && visible.Value == false)
                throw new InvalidOperationException("No se puede ocultar la unidad de medida por defecto.");

            if (nuevaUnidad is not null && nuevaUnidad.Codigo != st.Unidad.Codigo)
            {
                if (_indexUnidadCodigo.Contains(nuevaUnidad.Codigo))
                    throw new InvalidOperationException($"La unidad de medida \"{nuevaUnidad.Codigo}\" ya existe.");
                _indexUnidadCodigo.Remove(st.Unidad.Codigo);
                st.Unidad = nuevaUnidad;
                _indexUnidadCodigo.Add(st.Unidad.Codigo);
            }

            if (!string.IsNullOrWhiteSpace(nuevoNombre)) st.Nombre = nuevoNombre.Trim();
            if (visible.HasValue) st.Visible = visible.Value;
            if (nuevoOrden.HasValue) st.Orden = nuevoOrden.Value;

            if (esPorDefecto.HasValue)
            {
                if (esPorDefecto.Value) EstablecerUnidadDeMedidaPorDefecto(id);
                else if (st.EsPorDefecto) throw new InvalidOperationException("Para quitar el defecto, establece otra unidad como defecto.");
            }

            Version++;
        }

        public void EstablecerUnidadDeMedidaPorDefecto(Guid id)
        {
            if (!_umById.TryGetValue(id, out var st))
                throw new KeyNotFoundException("Unidad de medida no encontrada.");
            if (!st.Visible)
                throw new InvalidOperationException("No se puede establecer por defecto una unidad oculta.");

            if (_unidadMedidaDefaultId is Guid prevId && _umById.TryGetValue(prevId, out var prev))
                prev.EsPorDefecto = false;

            st.EsPorDefecto = true;
            _unidadMedidaDefaultId = id;
            Version++;
        }

        public void EliminarUnidadDeMedida(Guid id)
        {
            if (!_umById.TryGetValue(id, out var st))
                throw new KeyNotFoundException("Unidad de medida no encontrada.");
            if (st.EsSistema)
                throw new InvalidOperationException("No se puede eliminar una unidad del sistema.");
            if (st.EsPorDefecto)
                throw new InvalidOperationException("No se puede eliminar la unidad por defecto.");

            _indexUnidadCodigo.Remove(st.Unidad.Codigo);
            _umById.Remove(id);
            Version++;
        }

        // ========= DOMAIN EVENTS =========
        private readonly List<SharedKernel.Events.IDomainEvent> _domainEvents = new();
        public IReadOnlyCollection<SharedKernel.Events.IDomainEvent> DomainEvents => _domainEvents;
        private void AddDomainEvent(SharedKernel.Events.IDomainEvent evt) => _domainEvents.Add(evt);
        public void ClearDomainEvents() => _domainEvents.Clear();
    }
}
