using System;
using System.Collections.Generic;
using System.Linq;
using ConfiguracionSistemaBC.Domain.ValueObjects;

namespace ConfiguracionSistemaBC.Domain.Aggregates
{
    /// <summary>
    /// Aggregate raíz que centraliza la configuración de una empresa (tenant).
    /// - Datos legales (RUC, razón social, dirección)
    /// - Moneda base
    /// - Ambiente FE (PRUEBA/PRODUCCION)
    /// - Establecimientos
    /// - Series por tipo de comprobante
    /// - Preferencias opcionales (teléfonos, emails, pie de página, logo)
    /// 
    /// Reglas principales:
    /// - Ambiente: PRUEBA→PRODUCCION permitido una sola vez (no reversible)
    /// - Unicidad: código de establecimiento por empresa; (tipo, serie) único
    /// - Serie solo puede editarse/eliminarse si no está bloqueada por uso
    /// - Tipo de operación por defecto para series: 0101 (Venta interna)
    /// </summary>
    public sealed class ConfiguracionEmpresa
    {
        // ========= PUBLIC READ MODELS =========

        public sealed record EstablecimientoRead(
            Guid Id,
            string Codigo,
            string Nombre,
            DireccionPostal Direccion,
            bool Habilitado
        );

        public sealed record SerieRead(
            Guid Id,
            string Serie,
            TipoComprobanteCodigo Tipo,
            Guid EstablecimientoId,
            Correlativo CorrelativoActual,
            TipoOperacion TipoOperacion,
            bool EsPorDefecto,
            bool Bloqueada
        );

        // ========= STATE =========

        // Identidad multi-tenant
        public Guid TenantId { get; private set; }

        // Datos legales
        public Ruc Ruc { get; private set; } = null!;
        public string RazonSocial { get; private set; } = string.Empty;
        public string? NombreComercial { get; private set; }
        public DireccionPostal DireccionFiscal { get; private set; } = null!;

        // Parámetros base
        public Moneda MonedaBase { get; private set; } = Moneda.PEN;
        public AmbienteFe Ambiente { get; private set; } = AmbienteFe.PRUEBA;

        // Preferencias opcionales
        public Telefono Telefonos { get; private set; } = Telefono.Vacio;
        public List<Email> Emails { get; private set; } = new();
        public PieDePagina PieDePagina { get; private set; } = PieDePagina.Vacio;
        public LogoImagen? Logo { get; private set; }

        // ----- Establecimientos -----
        private sealed class EstablecimientoState
        {
            public Guid Id { get; init; }
            public string Codigo { get; set; } = string.Empty; // único por empresa
            public string Nombre { get; set; } = string.Empty;
            public DireccionPostal Direccion { get; set; } = null!;
            public bool Habilitado { get; set; } = true;
        }

        private readonly Dictionary<Guid, EstablecimientoState> _estById = new();
        private readonly Dictionary<string, Guid> _estByCodigo = new(StringComparer.OrdinalIgnoreCase);

        // ----- Series -----
        private sealed class SerieState
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

        // ========= FACTORY =========

        public static ConfiguracionEmpresa RegistrarNueva(
            Guid tenantId,
            Ruc ruc,
            string razonSocial,
            DireccionPostal direccionFiscal,
            Moneda monedaBase)
        {
            if (tenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(razonSocial)) throw new ArgumentNullException(nameof(razonSocial));

            return new ConfiguracionEmpresa
            {
                TenantId = tenantId,
                Ruc = ruc,
                RazonSocial = razonSocial.Trim(),
                DireccionFiscal = direccionFiscal,
                MonedaBase = monedaBase,
                Ambiente = AmbienteFe.PRUEBA, // siempre inicia en PRUEBA
                Telefonos = Telefono.Vacio,
                Emails = new List<Email>(),
                PieDePagina = PieDePagina.Vacio,
                Logo = null
            };
        }

        // ========= CAMBIOS DE AMBIENTE =========

        public void CambiarAmbiente(AmbienteFe destino)
        {
            if (destino is null) throw new ArgumentNullException(nameof(destino));
            AmbienteFe.ValidarTransicion(Ambiente, destino);
            Ambiente = destino;
            // Limpiezas/migraciones de datos de prueba suceden fuera (Application/Infra).
        }

        // ========= DATOS LEGALES =========

        public void ActualizarDatosLegales(Ruc ruc, string razonSocial, DireccionPostal direccionFiscal, string? nombreComercial = null)
        {
            if (string.IsNullOrWhiteSpace(razonSocial)) throw new ArgumentNullException(nameof(razonSocial));
            Ruc = ruc;
            RazonSocial = razonSocial.Trim();
            NombreComercial = string.IsNullOrWhiteSpace(nombreComercial) ? null : nombreComercial.Trim();
            DireccionFiscal = direccionFiscal;
        }

        // ========= PREFERENCIAS OPCIONALES =========

        public void ReemplazarEmails(IEnumerable<Email> emails)
        {
            if (emails is null) throw new ArgumentNullException(nameof(emails));
            Emails = emails.ToList();
        }

        public void ReemplazarTelefonos(Telefono telefonos)
        {
            Telefonos = telefonos ?? Telefono.Vacio;
        }

        public void ActualizarPieDePagina(PieDePagina pie) => PieDePagina = pie ?? PieDePagina.Vacio;

        public void EstablecerLogo(LogoImagen? logo) => Logo = logo;

        public void CambiarMonedaBase(Moneda monedaBase) => MonedaBase = monedaBase ?? Moneda.PEN;

        // ========= ESTABLECIMIENTOS =========

        public Guid RegistrarEstablecimiento(string codigo, string nombre, DireccionPostal direccion)
        {
            if (string.IsNullOrWhiteSpace(codigo)) throw new ArgumentNullException(nameof(codigo));
            if (string.IsNullOrWhiteSpace(nombre)) throw new ArgumentNullException(nameof(nombre));
            if (_estByCodigo.ContainsKey(codigo))
                throw new InvalidOperationException($"Ya existe un establecimiento con código \"{codigo}\".");

            var id = Guid.NewGuid();
            var st = new EstablecimientoState
            {
                Id = id,
                Codigo = codigo.Trim(),
                Nombre = nombre.Trim(),
                Direccion = direccion,
                Habilitado = true
            };

            _estById[id] = st;
            _estByCodigo[st.Codigo] = id;
            return id;
        }

        public void ActualizarEstablecimiento(Guid id, string nombre, DireccionPostal direccion)
        {
            if (!_estById.TryGetValue(id, out var st))
                throw new KeyNotFoundException("Establecimiento no encontrado.");

            if (string.IsNullOrWhiteSpace(nombre)) throw new ArgumentNullException(nameof(nombre));

            st.Nombre = nombre.Trim();
            st.Direccion = direccion;
        }

        public void DeshabilitarEstablecimiento(Guid id)
        {
            if (!_estById.TryGetValue(id, out var st))
                throw new KeyNotFoundException("Establecimiento no encontrado.");
            st.Habilitado = false;
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

        private static EstablecimientoRead ToRead(EstablecimientoState st)
            => new(st.Id, st.Codigo, st.Nombre, st.Direccion, st.Habilitado);

        private EstablecimientoState GetEstablecimientoOrThrow(Guid id)
        {
            if (!_estById.TryGetValue(id, out var st))
                throw new KeyNotFoundException("Establecimiento no encontrado.");
            return st;
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
                st.EstablecimientoId = est.Id;
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

        private static SerieRead ToRead(SerieState st)
            => new(
                st.Id,
                st.Serie.Codigo,
                st.Tipo,
                st.EstablecimientoId,
                st.CorrelativoActual,
                st.TipoOperacion,
                st.EsPorDefecto,
                st.Bloqueada
            );
    }
}