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
    /// Aplica cambios sobre la configuración de una empresa (datos legales y preferencias).
    /// No altera establecimientos, series ni ambiente (tienen sus propios casos de uso).
    /// </summary>
    public sealed class ActualizarConfiguracionEmpresaUseCase
    {
        private readonly IConfiguracionEmpresaRepository _repo;
        private readonly IUnitOfWork _uow;

        public ActualizarConfiguracionEmpresaUseCase(
            IConfiguracionEmpresaRepository repo,
            IUnitOfWork uow)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
        }

        public async Task<DTOs.ActualizarConfiguracionEmpresaOutputDto> Handle(
            DTOs.ActualizarConfiguracionEmpresaInputDto input,
            CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            if (string.IsNullOrWhiteSpace(input.Ruc))
                throw new ArgumentException("RUC obligatorio.", nameof(input.Ruc));

            // 1) Cargar agregado por RUC actual
                var rucActual = Ruc.From(input.Ruc);
                var agg = await _repo.FindByRucAsync(rucActual, ct).ConfigureAwait(false);
            if (agg is null)
                throw new KeyNotFoundException("Configuración de empresa no encontrada para el RUC especificado.");

            // 2) Datos legales (solo si se solicita cambio en cualquiera de sus campos)
            var requiereDatosLegales =
                   input.NuevoRuc is not null
                || input.NuevoRazonSocial is not null
                || input.NuevaDireccionFiscal is not null
                || input.NuevoNombreComercial is not null;

            if (requiereDatosLegales)
            {
                var ruc = input.NuevoRuc is null ? agg.Ruc : Ruc.From(input.NuevoRuc);
                var razon = input.NuevoRazonSocial ?? agg.RazonSocial;
                var dir = input.NuevaDireccionFiscal ?? agg.DireccionFiscal;
                var nomCom = input.NuevoNombreComercial ?? agg.NombreComercial;

                agg.ActualizarDatosLegales(ruc, razon, dir, nomCom);
            }

            // 3) Preferencias opcionales (se aplican solo si vienen informadas)
            if (input.NuevaMonedaBase is not null)
                agg.CambiarMonedaBase(input.NuevaMonedaBase);

            if (input.NuevoTelefono is not null)
                agg.ReemplazarTelefono(input.NuevoTelefono);

            if (input.NuevosEmails is not null)
                agg.ReemplazarEmails(input.NuevosEmails);

            if (input.NuevoPieDePagina is not null)
                agg.ActualizarPieDePagina(input.NuevoPieDePagina);

            if (input.MostrarImagenEnComprobanteImpresa.HasValue)
                agg.ConfigurarMostrarImagenEnComprobanteImpresa(input.MostrarImagenEnComprobanteImpresa.Value);

            if (input.ReemplazarLogo) // si se solicita reemplazo, incluso para limpiar (null)
                agg.EstablecerLogo(input.NuevoLogo);

            // 4) Persistir
            // Si tu repo requiere "Update", descomenta la línea siguiente y asegúrate que la interfaz lo expose.
            // _repo.Update(agg);
                await _uow.SaveChangesAsync(ct).ConfigureAwait(false);

            // 5) Armar salida (snapshot resumido)
            return new DTOs.ActualizarConfiguracionEmpresaOutputDto(
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
                Version: agg.Version
            );
        }
    }
}
