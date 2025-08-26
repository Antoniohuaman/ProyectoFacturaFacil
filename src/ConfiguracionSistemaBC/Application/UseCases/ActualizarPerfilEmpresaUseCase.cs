using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConfiguracionSistemaBC.Domain.Aggregates;
using ConfiguracionSistemaBC.Domain.Repositories;
using SharedKernel.ValueObjects;
using ConfiguracionSistemaBC.Domain.ValueObjects;
namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Caso de uso para actualizar el “perfil”/“preferencias” de la empresa:
    /// - Teléfonos (0..3) en un solo campo de entrada (separadores varios)
    /// - Emails visibles/ocultos (validación VO)
    /// - Pie de página (HTML o texto plano) — opcional
    /// - Logo (subir / quitar) — opcional
    ///
    /// NOTA:
    /// - No cambia identidad legal (RUC/razón social) ni ambiente ni moneda aquí.
    /// - Si un campo viene en null => NO se modifica.
    /// - Si viene en string vacío => se “limpia” (p. ej., sin teléfonos / sin pie de página).
    /// </summary>
    public sealed class ActualizarPerfilEmpresaUseCase
    {
        private readonly IConfiguracionEmpresaRepository _repo;
        private readonly IUnitOfWork _uow;

        public ActualizarPerfilEmpresaUseCase(IConfiguracionEmpresaRepository repo, IUnitOfWork uow)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _uow  = uow  ?? throw new ArgumentNullException(nameof(uow));
        }

        // ------------------------- Parámetros -------------------------

        public sealed record Params(
            Guid TenantId,
            PreferenciasParams Preferencias
        );

        public sealed record PreferenciasParams(
            // Telefonos: null = no cambiar; "" (o solo espacios) = dejar vacío
            string? Telefonos,
            // Emails: null = no cambiar; secuencia vacía = deja vacío
            IEnumerable<string>? EmailsVisibles,
            IEnumerable<string>? EmailsOcultos,
            // Pie de página: se acepta HTML o TextoPlano (si ambos vienen, prioriza HTML).
            string? PieDePaginaHtml,
            string? PieDePaginaTextoPlano,
            // Logo
            LogoParams? Logo,
            bool QuitarLogo = false
        );

        public sealed record LogoParams(
            string FileName,
            string ContentType,
            long   BytesLength,
            int    AnchoPx,
            int    AltoPx
        );

        // ------------------------- Resultado -------------------------

        public sealed record Result(
            Guid   TenantId,
            string Ambiente,          // "PRUEBA" | "PRODUCCION"
            string MonedaBaseCodigo,  // "PEN"/"USD"
            string Telefonos,         // texto unificado para mostrar (puede ser "")
            string[] EmailsVisibles,
            string[] EmailsOcultos,
            string  PieDePaginaHtml,  // puede ser ""
            bool   TieneLogo
        );

        // ------------------------- Ejecución -------------------------

        public async Task<Result> ExecuteAsync(Params p, CancellationToken ct = default)
        {
            if (p is null) throw new ArgumentNullException(nameof(p));
            if (p.TenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.", nameof(p.TenantId));
            if (p.Preferencias is null) throw new ArgumentNullException(nameof(p.Preferencias));

            // 1) Cargar agregado
            var agg = await _repo.GetByTenantIdAsync(p.TenantId, ct)
                      ?? throw new InvalidOperationException("La configuración de empresa no existe para este tenant.");

            // 2) Telefonos
            if (p.Preferencias.Telefonos is not null)
            {
                var tel = string.IsNullOrWhiteSpace(p.Preferencias.Telefonos)
                    ? Telefono.Vacio
                    : Telefono.FromTexto(p.Preferencias.Telefonos);
                agg.ReemplazarTelefonos(tel);
            }

            // 3) Emails (visibles/ocultos)
            if (p.Preferencias.EmailsVisibles is not null || p.Preferencias.EmailsOcultos is not null)
            {
                var list = new List<EmailEmpresa>(capacity: 8);

                if (p.Preferencias.EmailsVisibles is not null)
                {
                    foreach (var s in p.Preferencias.EmailsVisibles)
                    {
                        if (string.IsNullOrWhiteSpace(s)) continue; // ignorar vacíos
                        list.Add(EmailEmpresa.From(s, esVisible: true));   // valida VO
                    }
                }

                if (p.Preferencias.EmailsOcultos is not null)
                {
                    foreach (var s in p.Preferencias.EmailsOcultos)
                    {
                        if (string.IsNullOrWhiteSpace(s)) continue;
                        list.Add(EmailEmpresa.From(s, esVisible: false));
                    }
                }

                // Si ambas colecciones fueron provistas pero vacías -> deja sin emails
                agg.ReemplazarEmails(list);
            }

            // 4) Pie de página
            if (p.Preferencias.PieDePaginaHtml is not null || p.Preferencias.PieDePaginaTextoPlano is not null)
            {
                PieDePagina pie = PieDePagina.Vacio;

                if (p.Preferencias.PieDePaginaHtml is not null)
                {
                    // string.Empty => limpia; texto => sanitiza
                    pie = PieDePagina.FromHtml(p.Preferencias.PieDePaginaHtml);
                }
                else if (p.Preferencias.PieDePaginaTextoPlano is not null)
                {
                    pie = PieDePagina.FromTextoPlano(p.Preferencias.PieDePaginaTextoPlano);
                }

                agg.ActualizarPieDePagina(pie);
            }

            // 5) Logo
            if (p.Preferencias.QuitarLogo)
            {
                agg.EstablecerLogo(null);
            }
            else if (p.Preferencias.Logo is not null)
            {
                var lp = p.Preferencias.Logo;
                var logo = LogoImagen.FromUpload(lp.FileName, lp.ContentType, lp.BytesLength, lp.AnchoPx, lp.AltoPx);
                agg.EstablecerLogo(logo);
            }

            // 6) Persistir
            await _repo.UpdateAsync(agg, ct);
            await _uow.SaveChangesAsync(ct);

            // 7) Armar resultado
            var emailsVis = agg.Emails.Where(e => e.EsVisible).Select(e => e.Direccion).ToArray();
            var emailsOc  = agg.Emails.Where(e => !e.EsVisible).Select(e => e.Direccion).ToArray();

            return new Result(
                TenantId:         agg.TenantId,
                Ambiente:         agg.Ambiente.Value,
                MonedaBaseCodigo: agg.MonedaBase.Codigo,
                Telefonos:        agg.Telefonos.UnirParaMostrar(),
                EmailsVisibles:   emailsVis,
                EmailsOcultos:    emailsOc,
                PieDePaginaHtml:  agg.PieDePagina.Html,
                TieneLogo:        agg.Logo is not null
            );
        }
    }
}