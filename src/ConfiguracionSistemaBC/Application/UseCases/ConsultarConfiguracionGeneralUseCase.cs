using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConfiguracionSistemaBC.Domain.Aggregates;
using ConfiguracionSistemaBC.Domain.Repositories;
using SharedKernel.Application.Interfaces;      // ITenantContext
using SharedKernel.ValueObjects;               // EmpresaId

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Consulta la configuración general de la empresa (multiempresa/multi-tenant).
    /// Lee el aggregate ConfiguracionEmpresa y proyecta un snapshot para la UI.
    /// </summary>
    public sealed class ConsultarConfiguracionGeneralUseCase
    {
        private readonly IConfiguracionEmpresaRepository _repo;
        private readonly ITenantContext _tenant;

        public ConsultarConfiguracionGeneralUseCase(
            IConfiguracionEmpresaRepository repo,
            ITenantContext tenantContext)
        {
            _repo   = repo   ?? throw new ArgumentNullException(nameof(repo));
            _tenant = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        }

        public async Task<ConsultarConfiguracionGeneralOutputDto> HandleAsync(
            ConsultarConfiguracionGeneralInputDto input,
            CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));

            // Empresa objetivo: input.EmpresaId (si viene) o contexto
            var empresaId = !string.IsNullOrWhiteSpace(input.EmpresaId)
                ? EmpresaId.From(input.EmpresaId!.Trim())
                : _tenant.EmpresaId ?? throw new InvalidOperationException("No hay EmpresaId en el contexto.");

            var empresa = await _repo.GetByEmpresaIdAsync(empresaId, ct)
                ?? throw new KeyNotFoundException("No se encontró la configuración de la empresa.");

            // ---- Identidad / legales / base ----
            var output = new ConsultarConfiguracionGeneralOutputDto
            {
                EmpresaId = empresa.EmpresaId.Value,
                Ruc = empresa.Ruc.Canonizado,                           // usas el canonizado
                RazonSocial = empresa.RazonSocial,
                NombreComercial = empresa.NombreComercial,

                Ambiente = empresa.Ambiente.ToString(),
                MonedaBaseCodigo = empresa.MonedaBase.Codigo,

                DireccionFiscal = new ConsultarConfiguracionGeneralOutputDto.DireccionFiscalOut
                {
                    PaisCodigo = "PE",
                    Ubigeo = empresa.DireccionFiscal.Ubigeo ?? string.Empty,
                    Direccion = empresa.DireccionFiscal.Linea ?? string.Empty
                },

                // Preferencias
                Telefono = empresa.Telefono?.ToString(),
                Emails = empresa.Emails?.Select(e => e.ToString()).ToArray() ?? Array.Empty<string>(),
                PieDePagina = empresa.PieDePagina?.ToString(),
                MostrarImagenEnComprobanteImpresa = empresa.MostrarImagenEnComprobanteImpresa,
                TieneLogo = empresa.Logo is not null
            };

            // ---- Establecimientos ----
            if (input.IncluirEstablecimientos)
            {
                var principal = empresa.ObtenerEstablecimientoPrincipal();

                var ests = empresa.ListarEstablecimientos()
                                  .Select(e => new ConsultarConfiguracionGeneralOutputDto.EstablecimientoOut
                                  {
                                      Id = e.Id,
                                      Codigo = e.Codigo,
                                      Nombre = e.Nombre,
                                      Direccion = e.Direccion?.Linea ?? string.Empty,
                                      Ubigeo = e.Direccion?.Ubigeo ?? string.Empty,
                                      Habilitado = e.Habilitado,
                                      EsPrincipal = (principal is not null && principal.Id == e.Id)
                                  })
                                  .OrderBy(e => e.Codigo, StringComparer.OrdinalIgnoreCase)
                                  .ToArray();

                output = output with
                {
                    Establecimientos = ests,
                    EstablecimientoPrincipalId = principal?.Id
                };
            }

            // ---- Catálogos locales (formas de pago / unidades) ----
            if (input.IncluirCatalogos)
            {
                // Formas de pago
                var fps = empresa.ListarFormasDePago();
                if (!input.IncluirOcultos)
                    fps = fps.Where(f => f.Visible).ToList();

                var fpDefault = empresa.ObtenerFormaDePagoPorDefecto();

                var fpsOut = fps
                    .OrderBy(f => f.Orden)
                    .Select(f => new ConsultarConfiguracionGeneralOutputDto.FormaPagoOut
                    {
                        Id = f.Id,
                        PaymentMeansCode = f.Valor.PaymentMeansCode,
                        MetodoCodigo = f.Valor.MetodoCodigo,
                        Nombre = f.Nombre,
                        Visible = f.Visible,
                        EsPorDefecto = f.EsPorDefecto,
                        EsSistema = f.EsSistema,
                        Orden = f.Orden
                    })
                    .ToArray();

                // Unidades de medida
                var ums = empresa.ListarUnidadesDeMedida();
                if (!input.IncluirOcultos)
                    ums = ums.Where(u => u.Visible).ToList();

                var umDefault = empresa.ObtenerUnidadDeMedidaPorDefecto();

                var umsOut = ums
                    .OrderBy(u => u.Orden)
                    .Select(u => new ConsultarConfiguracionGeneralOutputDto.UnidadMedidaOut
                    {
                        Id = u.Id,
                        Codigo = u.Unidad.Codigo,
                        Nombre = u.Nombre,
                        Visible = u.Visible,
                        EsPorDefecto = u.EsPorDefecto,
                        EsSistema = u.EsSistema,
                        Orden = u.Orden
                    })
                    .ToArray();

                output = output with
                {
                    FormasDePago = fpsOut,
                    FormaPagoDefaultId = fpDefault?.Id,
                    UnidadesDeMedida = umsOut,
                    UnidadDeMedidaDefaultId = umDefault?.Id
                };
            }

            return output;
        }
    }
}
