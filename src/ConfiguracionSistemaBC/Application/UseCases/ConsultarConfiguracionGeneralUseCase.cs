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
    /// Consulta el “snapshot” completo de configuración de una empresa:
    /// perfil/datos legales, preferencias, establecimientos, series, formas de pago y unidades.
    /// </summary>
    public sealed class ConsultarConfiguracionGeneralUseCase
    {
        private readonly IConfiguracionEmpresaRepository _repo;

        public ConsultarConfiguracionGeneralUseCase(IConfiguracionEmpresaRepository repo)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public async Task<DTOs.ConsultarConfiguracionGeneralOutputDto> Handle(
            DTOs.ConsultarConfiguracionGeneralInputDto input,
            CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            if (string.IsNullOrWhiteSpace(input.Ruc))
                throw new ArgumentException("RUC obligatorio.", nameof(input.Ruc));

            var ruc = Ruc.From(input.Ruc);
                var agg = await _repo.FindByRucAsync(ruc, ct).ConfigureAwait(false);
            if (agg is null)
                throw new KeyNotFoundException("Configuración de empresa no encontrada para el RUC especificado.");

            // --- Perfil y preferencias ---
            var establecimientos = agg.ListarEstablecimientos().ToList();
            var principal = agg.ObtenerEstablecimientoPrincipal();

            // Series: el agregado lista por tipo; combinamos tipos conocidos (no alteramos el dominio)
            var series = new List<ConfiguracionEmpresa.SerieRead>();
            series.AddRange(agg.ListarSeriesPorTipo(TipoComprobanteCodigo.Factura));
            series.AddRange(agg.ListarSeriesPorTipo(TipoComprobanteCodigo.Boleta));

            // Formas de pago y unidades
            var formasPago = agg.ListarFormasDePago().ToList();
            var formaPagoDefault = agg.ObtenerFormaDePagoPorDefecto();

            var unidades = agg.ListarUnidadesDeMedida().ToList();
            var unidadDefault = agg.ObtenerUnidadDeMedidaPorDefecto();

            return new DTOs.ConsultarConfiguracionGeneralOutputDto(
                EmpresaId: agg.EmpresaId.Value,
                Ruc: agg.Ruc,
                RazonSocial: agg.RazonSocial,
                NombreComercial: agg.NombreComercial,
                DireccionFiscal: agg.DireccionFiscal,
                MonedaBase: agg.MonedaBase,
                Ambiente: agg.Ambiente,
                Telefono: agg.Telefono,
                Emails: agg.Emails.ToList(),
                PieDePagina: agg.PieDePagina,
                Logo: agg.Logo,
                MostrarImagenEnComprobanteImpresa: agg.MostrarImagenEnComprobanteImpresa,
                EstablecimientoPrincipal: principal,
                Establecimientos: establecimientos,
                Series: series,
                FormasDePago: formasPago,
                FormaDePagoPorDefecto: formaPagoDefault,
                UnidadesDeMedida: unidades,
                UnidadDeMedidaPorDefecto: unidadDefault,
                Version: agg.Version
            );
        }
    }
}
