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
    /// - Representa el punto único de acceso y modificación para todos los parámetros relevantes de la empresa.
    /// - Encapsula datos legales, preferencias, establecimientos y series de comprobantes.
    /// - Garantiza la consistencia y las reglas de negocio mediante métodos controlados.
    /// - Facilita la reconstrucción desde persistencia y la integración entre bounded contexts.
    /// - Aplica el patrón DDD: solo se modifica a través de sus métodos públicos.
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

        public sealed record SerieRead(
            Guid Id,
            string EmpresaId, // opaco, canonizado desde RUC
            string Serie,
            TipoComprobanteCodigo Tipo,
            Guid EstablecimientoId,
            Correlativo CorrelativoActual,
            TipoOperacion TipoOperacion,
            bool EsPorDefecto,
            bool Bloqueada
        );

        // ========= STATE =========

        // Identidad única de empresa: RUC
        public Ruc Ruc { get; private set; } = null!;
        // Identidad opaca para integración entre BCs (string basado en RUC)
        public EmpresaId EmpresaId { get; private set; } = null!;
        /// <summary>
        /// Control de concurrencia optimista
        /// </summary>
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
    // Fuente de verdad: entidad Establecimiento
    private readonly Dictionary<Guid, Establecimiento> _estById = new();
    private readonly Dictionary<string, Guid> _estByCodigo = new(StringComparer.OrdinalIgnoreCase);
    private Guid? _principalEstablecimientoId;

        // ----- Series -----
    internal sealed class SerieState
        {
            public Guid Id { get; init; }
            public TipoComprobanteCodigo Tipo { get; set; } = null!;
            public SerieCodigo Serie { get; set; } = null!;
            public Guid EstablecimientoId { get; set; }
            public Correlativo CorrelativoActual { get; set; } = null!;
            public TipoOperacion TipoOperacion { get; set; } = TipoOperacion.Default;
            public bool EsPorDefecto { get; set; }
            public bool Bloqueada { get; set; } // true si ya se usó en emisión
        }

        private readonly Dictionary<Guid, SerieState> _seriesById = new();
        // Índice de unicidad por (Tipo.Codigo, Serie.Codigo)
        private readonly HashSet<string> _indexTipoSerie = new(StringComparer.Ordinal);
        // Serie default por tipo (key: tipo.Codigo)
        private readonly Dictionary<string, Guid> _defaultSerieByTipo = new(StringComparer.Ordinal);

        // ========= CTOR PRIVADO =========
        private ConfiguracionEmpresa() { }

        /// <summary>
        /// Constructor interno para reconstrucción desde persistencia.
        /// Solo debe usarse por el repositorio o infraestructura.
        /// </summary>
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
            Dictionary<Guid, Entities.Establecimiento> estById,
            Dictionary<string, Guid> estByCodigo,
            Guid? principalEstablecimientoId,
            Dictionary<Guid, SerieState> seriesById,
            HashSet<string> indexTipoSerie,
            Dictionary<string, Guid> defaultSerieByTipo
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
            _estById = new Dictionary<Guid, Entities.Establecimiento>(estById);
            _estByCodigo = new Dictionary<string, Guid>(estByCodigo, StringComparer.OrdinalIgnoreCase);
            _principalEstablecimientoId = principalEstablecimientoId;
            _seriesById = new Dictionary<Guid, SerieState>(seriesById);
            _indexTipoSerie = new HashSet<string>(indexTipoSerie, StringComparer.Ordinal);
            _defaultSerieByTipo = new Dictionary<string, Guid>(defaultSerieByTipo, StringComparer.Ordinal);
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
                EmpresaId = EmpresaId.From(ruc.Canonizado), // opaco, canonizado desde RUC
                RazonSocial = razonSocial.Trim(),
                DireccionFiscal = direccionFiscal,
                MonedaBase = monedaBase,
                Ambiente = AmbienteFe.PRUEBA, // siempre inicia en PRUEBA
                Telefono = Telefono.Vacio,
                Emails = new List<Email>(),
                PieDePagina = PieDePagina.FromTextoPlano("Gracias Por su Preferencia"),
                Logo = null
            };

            // Bootstrap: Establecimiento principal + series por defecto
            var estPrincipalId = empresa.RegistrarEstablecimiento("01", "Establecimiento Principal", direccionFiscal);
            empresa.EstablecerComoPrincipal(estPrincipalId);

            // Series por defecto (venta interna)
            // FE01 (Factura), BE01 (Boleta) -> correlativo 1; Default por cada tipo
            empresa.AgregarSerie(
                TipoComprobanteCodigo.Factura,
                SerieCodigo.From("FE01"),
                estPrincipalId,
                Correlativo.From(1),
                TipoOperacion.Default,
                esPorDefecto: true);

            empresa.AgregarSerie(
                TipoComprobanteCodigo.Boleta,
                SerieCodigo.From("BE01"),
                estPrincipalId,
                Correlativo.From(1),
                TipoOperacion.Default,
                esPorDefecto: true);

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
            Ambiente = destino;
                Version++;
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

        public Guid RegistrarEstablecimiento(string codigo, string nombre, DomicilioFiscal direccion, Telefono? telefono = null, Email? email = null)
        {
            if (string.IsNullOrWhiteSpace(codigo)) throw new ArgumentNullException(nameof(codigo));
            if (string.IsNullOrWhiteSpace(nombre)) throw new ArgumentNullException(nameof(nombre));
            if (direccion is null) throw new ArgumentNullException(nameof(direccion));
            if (!direccion.EsPeru)
                throw new ArgumentException("Solo se soporta domicilio fiscal de Perú (PE) para establecimientos.", nameof(direccion));
            if (_estByCodigo.ContainsKey(codigo.Trim()))
                throw new InvalidOperationException($"Ya existe un establecimiento con código \"{codigo}\".");

            var id = Guid.NewGuid();
            var est = new Establecimiento(
                EstablecimientoId.From(id),
                EmpresaId,
                nombre.Trim(),
                codigo.Trim(),
                direccion,
                telefono ?? Telefono.Vacio,
                email
            );
            _estById[id] = est;
            _estByCodigo[est.Codigo] = id;
            Version++;
            return id;
        }

        public void EstablecerComoPrincipal(Guid id)
        {
            if (!_estById.ContainsKey(id))
                throw new KeyNotFoundException("Establecimiento no encontrado.");
            _principalEstablecimientoId = id;
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
            // Actualiza índice
            _estByCodigo.Remove(est.Codigo);
            // Actualiza la entidad
            est.ActualizarDatos(est.Nombre, canon, est.Direccion, est.Telefono, est.Email);
            _estByCodigo[canon] = id;
            Version++;
        }

        public void ActualizarEstablecimiento(Guid id, string nombre, DomicilioFiscal direccion, Telefono? telefono = null, Email? email = null)
        {
            if (!_estById.TryGetValue(id, out var est))
                throw new KeyNotFoundException("Establecimiento no encontrado.");
            if (string.IsNullOrWhiteSpace(nombre)) throw new ArgumentNullException(nameof(nombre));
            if (direccion is null) throw new ArgumentNullException(nameof(direccion));
            if (!direccion.EsPeru)
                throw new ArgumentException("Solo se soporta domicilio fiscal de Perú (PE) para establecimientos.", nameof(direccion));
            est.ActualizarDatos(nombre.Trim(), est.Codigo, direccion, telefono ?? est.Telefono, email ?? est.Email);
            Version++;
        }

    // Si necesitas habilitar/deshabilitar, agrega propiedad en la entidad Establecimiento y método aquí

        public void EliminarEstablecimiento(Guid id)
        {
            if (!_estById.TryGetValue(id, out var est))
                throw new KeyNotFoundException("Establecimiento no encontrado.");
            // No dejar a la empresa sin establecimientos
            if (_estById.Count <= 1)
                throw new InvalidOperationException("La empresa debe conservar al menos un establecimiento.");
            // No permitir si alguna serie del establecimiento está bloqueada (ya usada)
            var seriesDelEst = _seriesById.Values.Where(s => s.EstablecimientoId == id).ToList();
            if (seriesDelEst.Any(s => s.Bloqueada))
                throw new InvalidOperationException("No se puede eliminar: existen series usadas asociadas al establecimiento.");
            // Si es principal, promover automáticamente otro
            if (_principalEstablecimientoId == id)
            {
                var candidato = _estById.Keys.First(eid => eid != id);
                _principalEstablecimientoId = candidato;
            }
            // Limpiar índices de series y series mismas
            foreach (var s in seriesDelEst)
            {
                if (_defaultSerieByTipo.TryGetValue(s.Tipo.Codigo, out var defId) && defId == s.Id)
                    _defaultSerieByTipo.Remove(s.Tipo.Codigo);
                _indexTipoSerie.Remove(IndexKey(s.Tipo, s.Serie));
                _seriesById.Remove(s.Id);
            }
            // Remover establecimiento
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

        private Establecimiento GetEstablecimientoOrThrow(Guid id)
        {
            if (!_estById.TryGetValue(id, out var est))
                throw new KeyNotFoundException("Establecimiento no encontrado.");
            return est;
        }

        // ========= SERIES =========

        /// <summary>
        /// Agrega una serie para un tipo de comprobante.
        /// El <paramref name="tipoOperacion"/> es opcional; por defecto se usa 0101 – Venta interna.
        /// </summary>
        public Guid AgregarSerie(
            TipoComprobanteCodigo tipo,
            SerieCodigo serie,
            Guid establecimientoId,
            Correlativo correlativoInicial,
            TipoOperacion? tipoOperacion = null,
            bool esPorDefecto = false)
        {
            if (tipo is null) throw new ArgumentNullException(nameof(tipo));
            if (serie is null) throw new ArgumentNullException(nameof(serie));

            var est = GetEstablecimientoOrThrow(establecimientoId);
            if (!est.Habilitado)
                throw new InvalidOperationException("No se puede registrar serie en un establecimiento deshabilitado.");

            // Validación de prefijo según tipo (F/B). Lanza si no coincide.
            SerieCodigo.ValidarSegunTipo(serie, tipo);

            var indexKey = IndexKey(tipo, serie);
            if (_indexTipoSerie.Contains(indexKey))
                throw new InvalidOperationException($"La serie \"{serie}\" ya existe para el tipo {tipo.Codigo}.");

            var id = Guid.NewGuid();
            var st = new SerieState
            {
                Id = id,
                Tipo = tipo,
                Serie = serie,
                EstablecimientoId = establecimientoId,
                CorrelativoActual = correlativoInicial,
                TipoOperacion = tipoOperacion ?? TipoOperacion.Default,
                EsPorDefecto = false,
                Bloqueada = false
            };

            _seriesById[id] = st;
            _indexTipoSerie.Add(indexKey);

            if (esPorDefecto) EstablecerDefault(tipo, id, setTrueOnItem: true);

            return id;
        }

        public SerieRead? ObtenerSeriePorId(Guid id)
            => _seriesById.TryGetValue(id, out var st) ? ToRead(st) : null;

        public SerieRead? ObtenerSeriePorDefecto(TipoComprobanteCodigo tipo)
        {
            if (_defaultSerieByTipo.TryGetValue(tipo.Codigo, out var id) && _seriesById.TryGetValue(id, out var st))
                return ToRead(st);
            return null;
        }

        public IReadOnlyList<SerieRead> ListarSeriesPorTipo(TipoComprobanteCodigo tipo)
            => _seriesById.Values.Where(s => s.Tipo == tipo).Select(ToRead).ToList();

        public void ActualizarSerie(
            Guid serieId,
            SerieCodigo? nuevaSerie = null,
            Guid? nuevoEstablecimientoId = null,
            TipoOperacion? nuevoTipoOperacion = null,
            bool? esPorDefecto = null)
        {
            if (!_seriesById.TryGetValue(serieId, out var st))
                throw new KeyNotFoundException("Serie no encontrada.");

            if (st.Bloqueada)
                throw new InvalidOperationException("La serie ya fue usada y no puede actualizarse.");

            // Cambiar establecimiento
            if (nuevoEstablecimientoId.HasValue)
            {
                var est = GetEstablecimientoOrThrow(nuevoEstablecimientoId.Value);
                if (!est.Habilitado)
                    throw new InvalidOperationException("No se puede asignar a un establecimiento deshabilitado.");
                st.EstablecimientoId = est.Id.Value;
            }

            // Cambiar tipo de operación
            if (nuevoTipoOperacion is not null)
                st.TipoOperacion = nuevoTipoOperacion;

            // Cambiar serie (respetando unicidad y prefijo por tipo)
            if (nuevaSerie is not null)
            {
                SerieCodigo.ValidarSegunTipo(nuevaSerie, st.Tipo);

                var newKey = IndexKey(st.Tipo, nuevaSerie);
                if (newKey != IndexKey(st.Tipo, st.Serie) && _indexTipoSerie.Contains(newKey))
                    throw new InvalidOperationException($"Ya existe la serie \"{nuevaSerie}\" para tipo {st.Tipo.Codigo}.");

                // liberar índice viejo y ocupar el nuevo
                _indexTipoSerie.Remove(IndexKey(st.Tipo, st.Serie));
                st.Serie = nuevaSerie;
                _indexTipoSerie.Add(newKey);
            }

            // Default flag
            if (esPorDefecto.HasValue)
            {
                if (esPorDefecto.Value) EstablecerDefault(st.Tipo, st.Id, setTrueOnItem: true);
                else                     EstablecerDefault(st.Tipo, st.Id, setTrueOnItem: false);
            }
            Version++;
        }

        public void BloquearSeriePorUso(Guid serieId)
        {
            if (!_seriesById.TryGetValue(serieId, out var st))
                throw new KeyNotFoundException("Serie no encontrada.");
            st.Bloqueada = true;
        }

        public void EliminarSerie(Guid serieId)
        {
            if (!_seriesById.TryGetValue(serieId, out var st))
                throw new KeyNotFoundException("Serie no encontrada.");

            if (st.Bloqueada)
                throw new InvalidOperationException("No se puede eliminar una serie que ya fue usada.");

            // Quitar default si corresponde
            if (_defaultSerieByTipo.TryGetValue(st.Tipo.Codigo, out var defId) && defId == serieId)
                _defaultSerieByTipo.Remove(st.Tipo.Codigo);

            _indexTipoSerie.Remove(IndexKey(st.Tipo, st.Serie));
            _seriesById.Remove(serieId);
        }

        private static string IndexKey(TipoComprobanteCodigo tipo, SerieCodigo serie)
            => $"{tipo.Codigo}|{serie.Codigo}";

        private void EstablecerDefault(TipoComprobanteCodigo tipo, Guid id, bool setTrueOnItem)
        {
            // desmarcar el anterior
            if (_defaultSerieByTipo.TryGetValue(tipo.Codigo, out var prevId) && _seriesById.TryGetValue(prevId, out var prev))
                prev.EsPorDefecto = false;

            if (setTrueOnItem && _seriesById.TryGetValue(id, out var st))
            {
                st.EsPorDefecto = true;
                _defaultSerieByTipo[tipo.Codigo] = id;
            }
            else
            {
                // quitar default (si quitaste el flag en la misma serie actual)
                if (_defaultSerieByTipo.TryGetValue(tipo.Codigo, out var cur) && cur == id)
                    _defaultSerieByTipo.Remove(tipo.Codigo);
            }
        }

        private SerieRead ToRead(SerieState st)
            => new(
                st.Id,
                EmpresaId.Value,
                st.Serie.Codigo,
                st.Tipo,
                st.EstablecimientoId,
                st.CorrelativoActual,
                st.TipoOperacion,
                st.EsPorDefecto,
                st.Bloqueada
            );

        // ========= DOMAIN EVENTS =========
        private readonly List<SharedKernel.Events.IDomainEvent> _domainEvents = new();
        public IReadOnlyCollection<SharedKernel.Events.IDomainEvent> DomainEvents => _domainEvents;
        private void AddDomainEvent(SharedKernel.Events.IDomainEvent evt) => _domainEvents.Add(evt);
        public void ClearDomainEvents() => _domainEvents.Clear();
    }
}
