using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConfiguracionSistemaBC.Domain.Aggregates;
using ConfiguracionSistemaBC.Domain.Repositories;
using ConfiguracionSistemaBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Caso de uso: Registrar la configuración inicial de un tenant.
    /// - Crea el agregado ConfiguracionEmpresa con Ambiente=PRUEBA (por defecto).
    /// - Registra datos legales, moneda base, preferencias, un establecimiento principal
    ///   y (opcional) series iniciales asociadas a ese establecimiento.
    /// - Si ya existe configuración para el TenantId, lanza excepción (no es “guardar/actualizar”).
    /// </summary>
    public sealed class RegistrarConfiguracionEmpresaUseCase
    {
        // ========= INPUT (SIN "COMMAND") =========
        public sealed record Params(
            Guid TenantId,
            DatosLegalesParams DatosLegales,
            MonedaParams MonedaBase,
            PreferenciasParams? Preferencias,
            EstablecimientoParams EstablecimientoPrincipal,    // requerido en el registro
            IEnumerable<SerieParams>? SeriesIniciales          // opcional
        );

        public sealed record DatosLegalesParams(
            string Ruc,
            string RazonSocial,
            string DireccionLinea,
            string Ubigeo,
            string Departamento,
            string Provincia,
            string Distrito,
            string? NombreComercial = null,
            string PaisIso = "PE",
            string AddressTypeCode = "0000"
        );

        public sealed record MonedaParams(string CodigoOAlias); // "PEN","USD","S/.","US$", etc.

        public sealed record PreferenciasParams(
            string? Telefonos,                      // campo único, admite “/”, “,”, etc.
            IEnumerable<string>? EmailsVisibles,    // se guardan con EsVisible=true
            IEnumerable<string>? EmailsOcultos,     // se guardan con EsVisible=false
            string? PieDePaginaHtml,                // opcional, HTML saneado por el VO
            LogoParams? Logo                        // opcional
        );

        public sealed record LogoParams(
            string FileName,
            string ContentType, // "image/png" | "image/jpeg"
            long BytesLength,
            int AnchoPx,
            int AltoPx
        );

        public sealed record EstablecimientoParams(
            string Codigo,               // único por empresa
            string Nombre,
            string DireccionLinea,
            string Ubigeo,
            string Departamento,
            string Provincia,
            string Distrito,
            string PaisIso = "PE",
            string AddressTypeCode = "0000"
        );

        public sealed record SerieParams(
            string TipoComprobante,        // "01","03","FACTURA","BOLETA","F","B"
            string Serie,                  // "F001","B001",...
            string EstablecimientoCodigo,  // debe corresponder al registrado en este caso de uso
            string CorrelativoInicial,     // "1".."99999999"
            string? TipoOperacion = null,  // null => 0101 (Venta interna)
            bool EsPorDefecto = false
        );

        // ========= OUTPUT =========
        public sealed record Result(
            Guid TenantId,
            string Ruc,
            string RazonSocial,
            string MonedaBaseCodigo,
            string Ambiente, // "PRUEBA"
            IReadOnlyList<ConfiguracionEmpresa.EstablecimientoRead> Establecimientos,
            IReadOnlyList<ConfiguracionEmpresa.SerieRead> Series
        );

        private readonly IConfiguracionEmpresaRepository _repo;
        private readonly IUnitOfWork _uow;

        public RegistrarConfiguracionEmpresaUseCase(
            IConfiguracionEmpresaRepository repo,
            IUnitOfWork uow)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
        }

        public async Task<Result> ExecuteAsync(Params p, CancellationToken ct = default)
        {
            if (p is null) throw new ArgumentNullException(nameof(p));
            if (p.TenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.", nameof(p));

            // 1) Verificar que NO exista configuración previa
            var existente = await _repo.GetByTenantIdAsync(p.TenantId, ct);
            if (existente is not null)
                throw new InvalidOperationException("La configuración para este tenant ya fue registrada.");

            // 2) Construcción de VOs requeridos
            var ruc = Ruc.FromString(p.DatosLegales.Ruc);
            var direccionFiscal = DireccionPostal.From(
                p.DatosLegales.DireccionLinea,
                p.DatosLegales.Ubigeo,
                p.DatosLegales.Departamento,
                p.DatosLegales.Provincia,
                p.DatosLegales.Distrito,
                p.DatosLegales.PaisIso,
                p.DatosLegales.AddressTypeCode
            );
            var moneda = Moneda.Create(p.MonedaBase.CodigoOAlias);

            // 3) Crear agregado (Ambiente=PRUEBA por defecto dentro del aggregate)
            var agg = ConfiguracionEmpresa.RegistrarNueva(
                p.TenantId,
                ruc,
                p.DatosLegales.RazonSocial,
                direccionFiscal,
                moneda
            );

            // 4) Preferencias (opcionales)
            if (p.Preferencias is not null)
            {
                var pref = p.Preferencias;

                // Teléfonos
                agg.ReemplazarTelefonos(Telefono.FromTexto(pref.Telefonos));

                // Emails
                var emails = new List<EmailEmpresa>();
                if (pref.EmailsVisibles is not null)
                {
                    foreach (var ev in pref.EmailsVisibles)
                        if (EmailEmpresa.TryFrom(ev, out var em, esVisible: true)) emails.Add(em!);
                        else throw new ArgumentOutOfRangeException(nameof(pref.EmailsVisibles), $"Email inválido: {ev}");
                }
                if (pref.EmailsOcultos is not null)
                {
                    foreach (var eo in pref.EmailsOcultos)
                        if (EmailEmpresa.TryFrom(eo, out var em, esVisible: false)) emails.Add(em!);
                        else throw new ArgumentOutOfRangeException(nameof(pref.EmailsOcultos), $"Email inválido: {eo}");
                }
                agg.ReemplazarEmails(emails);

                // Pie de página
                agg.ActualizarPieDePagina(PieDePagina.FromHtml(pref.PieDePaginaHtml));

                // Logo
                if (pref.Logo is null) agg.EstablecerLogo(null);
                else
                {
                    var l = pref.Logo;
                    var logo = LogoImagen.FromUpload(l.FileName, l.ContentType, l.BytesLength, l.AnchoPx, l.AltoPx);
                    agg.EstablecerLogo(logo);
                }
            }

            // 5) Establecimiento principal (requerido en registro)
            var e = p.EstablecimientoPrincipal;
            var dirEst = DireccionPostal.From(
                e.DireccionLinea, e.Ubigeo, e.Departamento, e.Provincia, e.Distrito, e.PaisIso, e.AddressTypeCode
            );
            var estId = agg.RegistrarEstablecimiento(e.Codigo, e.Nombre, dirEst);

            // 6) Series iniciales (opcionales) — asociadas al código del establecimiento creado
            if (p.SeriesIniciales is not null)
            {
                foreach (var s in p.SeriesIniciales)
                {
                    // Debe apuntar al código del establecimiento registrado en este caso de uso
                    if (!string.Equals(s.EstablecimientoCodigo, e.Codigo, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException(
                            $"La serie \"{s.Serie}\" referencia el establecimiento \"{s.EstablecimientoCodigo}\", " +
                            $"pero en este registro solo se creó/usa \"{e.Codigo}\"."
                        );

                    var tipo = TipoComprobanteCodigo.From(s.TipoComprobante);
                    var serie = SerieCodigo.ForTipo(s.Serie, tipo);
                    var cor = Correlativo.FromString(s.CorrelativoInicial);
                    var tipoOp = s.TipoOperacion is null ? null : TipoOperacion.From(s.TipoOperacion);

                    agg.AgregarSerie(tipo, serie, estId, cor, tipoOp, esPorDefecto: s.EsPorDefecto);
                }
            }

            // 7) Persistir
            await _repo.AddAsync(agg, ct);
            await _uow.SaveChangesAsync(ct);

            // 8) Salida
            var ests = agg.ListarEstablecimientos();
            var series = new List<ConfiguracionEmpresa.SerieRead>();
            foreach (var t in TipoComprobanteCodigo.All)
                series.AddRange(agg.ListarSeriesPorTipo(t));

            return new Result(
                TenantId: agg.TenantId,
                Ruc: agg.Ruc.Numero,
                RazonSocial: agg.RazonSocial,
                MonedaBaseCodigo: agg.MonedaBase.Codigo,
                Ambiente: agg.Ambiente.Value, // "PRUEBA"
                Establecimientos: ests,
                Series: series
            );
        }
    }
}