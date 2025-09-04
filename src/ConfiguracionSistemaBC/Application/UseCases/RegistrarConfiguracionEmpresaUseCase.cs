using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConfiguracionSistemaBC.Domain.Aggregates;
using ConfiguracionSistemaBC.Domain.Repositories; // <- tus interfaces
using SharedKernel.ValueObjects;

namespace ConfiguracionSistemaBC.Application.UseCases
{
    public sealed class RegistrarConfiguracionEmpresaUseCase
    {
        private readonly IConfiguracionEmpresaRepository _repo;
        private readonly IUnitOfWork _uow;

        public RegistrarConfiguracionEmpresaUseCase(
            IConfiguracionEmpresaRepository repo,
            IUnitOfWork uow)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _uow  = uow  ?? throw new ArgumentNullException(nameof(uow));
        }

        public async Task<RegistrarConfiguracionEmpresaOutputDto> Handle(
            RegistrarConfiguracionEmpresaInputDto input,
            CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));

            // 1) Crear el agregado (bootstrap: establecimiento principal, series, formas de pago, unidades).
            var empresa = ConfiguracionEmpresa.RegistrarNueva(
                input.Ruc,
                input.RazonSocial,
                input.DireccionFiscal,
                input.MonedaBase ?? Moneda.PEN()
            );

            // 2) Preferencias iniciales (opcionales)
            if (!string.IsNullOrWhiteSpace(input.NombreComercial))
                empresa.ActualizarDatosLegales(input.Ruc, input.RazonSocial, input.DireccionFiscal, input.NombreComercial);

            if (input.Telefono is not null) empresa.ReemplazarTelefono(input.Telefono);
            if (input.Emails is not null) empresa.ReemplazarEmails(input.Emails);
            if (input.PieDePagina is not null) empresa.ActualizarPieDePagina(input.PieDePagina);
            if (input.Logo is not null) empresa.EstablecerLogo(input.Logo);
            if (input.MostrarImagenEnComprobanteImpresa.HasValue)
                empresa.ConfigurarMostrarImagenEnComprobanteImpresa(input.MostrarImagenEnComprobanteImpresa.Value);

            // 3) Visibilidad inicial (opcional) — respetando que no se oculta el ítem por defecto
            if (input.FormasDePagoVisibles is not null)
            {
                var visibles = new HashSet<string>(input.FormasDePagoVisibles.Select(s => s.Trim().ToUpperInvariant()), StringComparer.Ordinal);
                var actuales = empresa.ListarFormasDePago();
                var porDefecto = empresa.ObtenerFormaDePagoPorDefecto();

                foreach (var fp in actuales)
                {
                    var target = visibles.Contains(fp.Nombre.Trim().ToUpperInvariant());
                    if (!target && porDefecto is not null && porDefecto.Id == fp.Id) continue;
                    if (fp.Visible != target) empresa.ActualizarFormaDePago(fp.Id, visible: target);
                }
            }

            if (input.UnidadesDeMedidaVisibles is not null)
            {
                var visibles = new HashSet<string>(input.UnidadesDeMedidaVisibles.Select(s => s.Trim().ToUpperInvariant()), StringComparer.Ordinal);
                var actuales = empresa.ListarUnidadesDeMedida();
                var porDefecto = empresa.ObtenerUnidadDeMedidaPorDefecto();

                foreach (var um in actuales)
                {
                    var target = visibles.Contains(um.Unidad.Codigo.Trim().ToUpperInvariant());
                    if (!target && porDefecto is not null && porDefecto.Id == um.Id) continue;
                    if (um.Visible != target) empresa.ActualizarUnidadDeMedida(um.Id, visible: target);
                }
            }

            // 4) Defaults explícitos (si se envían). Si no, queda Contado/NIU del bootstrap.
            if (!string.IsNullOrWhiteSpace(input.FormaDePagoPorDefectoNombre))
            {
                var targetName = input.FormaDePagoPorDefectoNombre!.Trim();
                var match = empresa.ListarFormasDePago()
                    .FirstOrDefault(x => string.Equals(x.Nombre, targetName, StringComparison.OrdinalIgnoreCase));
                if (match is not null) empresa.EstablecerFormaPagoPorDefecto(match.Id);
            }

            if (!string.IsNullOrWhiteSpace(input.UnidadDeMedidaPorDefectoCodigo))
            {
                var code = input.UnidadDeMedidaPorDefectoCodigo!.Trim().ToUpperInvariant();
                var match = empresa.ListarUnidadesDeMedida()
                    .FirstOrDefault(x => string.Equals(x.Unidad.Codigo, code, StringComparison.Ordinal));
                if (match is not null) empresa.EstablecerUnidadDeMedidaPorDefecto(match.Id);
            }

            // 5) Persistencia con tus repositorios
            await _repo.AddAsync(empresa, ct);          // <- usa el método que exponga tu repo
            await _uow.SaveChangesAsync(ct);            // <- commit transaccional

            // 6) Salida
            return BuildOutput(empresa);
        }

        private static RegistrarConfiguracionEmpresaOutputDto BuildOutput(ConfiguracionEmpresa empresa)
        {
            var estPrincipal = empresa.ObtenerEstablecimientoPrincipal();
            var fpDef = empresa.ObtenerFormaDePagoPorDefecto();
            var umDef = empresa.ObtenerUnidadDeMedidaPorDefecto();

            return new RegistrarConfiguracionEmpresaOutputDto(
                empresa.EmpresaId,
                empresa.Ruc,
                empresa.RazonSocial,
                empresa.NombreComercial,
                empresa.DireccionFiscal,
                empresa.MonedaBase,
                estPrincipal,
                fpDef,
                umDef,
                empresa.ListarFormasDePago(),
                empresa.ListarUnidadesDeMedida()
            );
        }
    }
}
