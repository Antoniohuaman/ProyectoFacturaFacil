using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConfiguracionSistemaBC.Domain.Aggregates;
using ConfiguracionSistemaBC.Domain.Repositories;
using SharedKernel.Application.Interfaces;              // ITenantContext
using SharedKernel.ValueObjects;                        // EmpresaId, DomicilioFiscal, Moneda, Telefono, Email
using ConfiguracionSistemaBC.Domain.ValueObjects;       // AmbienteFe (no se usa aquí), LogoImagen, PieDePagina

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Actualiza configuración de empresa (sin cambiar el RUC).
    /// Permite actualizar: Razón Social, Nombre Comercial, Dirección Fiscal, Teléfono, Emails, Pie de Página,
    /// MostrarImagenEnComprobanteImpresa, Moneda Base y (opcionalmente) el Logo.
    /// </summary>
    public sealed class ActualizarConfiguracionEmpresaUseCase
    {
        private readonly IConfiguracionEmpresaRepository _repo;
        private readonly IUnitOfWork _uow;
        private readonly ITenantContext _tenant;

        public ActualizarConfiguracionEmpresaUseCase(
            IConfiguracionEmpresaRepository repo,
            IUnitOfWork uow,
            ITenantContext tenantContext)
        {
            _repo   = repo   ?? throw new ArgumentNullException(nameof(repo));
            _uow    = uow    ?? throw new ArgumentNullException(nameof(uow));
            _tenant = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        }

        public async Task<ActualizarConfiguracionEmpresaOutputDto> HandleAsync(
            ActualizarConfiguracionEmpresaInputDto input,
            CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));

            // Empresa destino (input o contexto)
            var empresaId = !string.IsNullOrWhiteSpace(input.EmpresaId)
                ? EmpresaId.From(input.EmpresaId!.Trim())
                : _tenant.EmpresaId ?? throw new InvalidOperationException("No hay EmpresaId en el contexto.");

            var empresa = await _repo.GetByEmpresaIdAsync(empresaId, ct)
                ?? throw new KeyNotFoundException("No se encontró la configuración de la empresa.");

            var versionOriginal = empresa.Version;

            // ===== DATOS LEGALES (sin cambiar RUC) =====
            var quiereActualizarRazon   = !string.IsNullOrWhiteSpace(input.RazonSocial);
            var quiereActualizarNombreC = input.NombreComercial != null; // permitir null para limpiar
            var quiereActualizarDir     = input.DireccionFiscal != null;

            if (quiereActualizarRazon || quiereActualizarNombreC || quiereActualizarDir)
            {
                var nuevaRazon = quiereActualizarRazon ? input.RazonSocial!.Trim() : empresa.RazonSocial;
                var nuevoNC    = quiereActualizarNombreC ? (string.IsNullOrWhiteSpace(input.NombreComercial) ? null : input.NombreComercial!.Trim()) : empresa.NombreComercial;
                var nuevaDir   = quiereActualizarDir ? MapDomicilioFiscal(input.DireccionFiscal!) : empresa.DireccionFiscal;

                // El RUC se mantiene igual (NO se cambia)
                empresa.ActualizarDatosLegales(empresa.Ruc, nuevaRazon, nuevaDir, nuevoNC);
            }

            // ===== MONEDA BASE =====
            if (!string.IsNullOrWhiteSpace(input.MonedaCodigo))
            {
                var moneda = MapMoneda(input.MonedaCodigo!);
                empresa.CambiarMonedaBase(moneda);
            }

            // ===== PREFERENCIAS =====
            if (!string.IsNullOrWhiteSpace(input.Telefono))
                empresa.ReemplazarTelefono(Telefono.FromTexto(input.Telefono!.Trim()));

            if (input.Emails is not null)
            {
                var emails = input.Emails
                                  .Where(s => !string.IsNullOrWhiteSpace(s))
                                  .Select(s => Email.Create(s.Trim()))
                                  .ToList();
                empresa.ReemplazarEmails(emails);
            }

            if (input.PieDePagina is not null)
            {
                // Si viene cadena vacía => PieDePagina.Vacio
                var pie = string.IsNullOrWhiteSpace(input.PieDePagina)
                    ? PieDePagina.Vacio
                    : PieDePagina.FromTextoPlano(input.PieDePagina!.Trim());
                empresa.ActualizarPieDePagina(pie);
            }

            if (input.MostrarImagenEnComprobanteImpresa.HasValue)
                empresa.ConfigurarMostrarImagenEnComprobanteImpresa(input.MostrarImagenEnComprobanteImpresa.Value);

            // ===== LOGO (opcional) =====
            if (input.EliminarLogo == true)
            {
                empresa.EstablecerLogo(null);
            }
            else if (input.LogoBytes is not null && input.LogoBytes.Length > 0)
            {
                // TODO: Ajustar a tu fábrica real de LogoImagen (no se expuso la API exacta en el dominio).
                // Ejemplo si existiera:
                // var logo = LogoImagen.FromUpload(input.LogoBytes, input.LogoFileName ?? "logo.bin", input.LogoContentType);
                // empresa.EstablecerLogo(logo);
            }

            // Persistencia (concurrencia optimista)
            await _repo.UpdateIfVersionMatchAsync(empresa, versionOriginal, ct);
            await _uow.SaveChangesAsync(ct);

            // Salida
            var principal = empresa.ObtenerEstablecimientoPrincipal(); // snapshot útil para UI
            return new ActualizarConfiguracionEmpresaOutputDto
            {
                EmpresaId = empresa.EmpresaId.Value,
                Ruc = empresa.Ruc.Canonizado,
                RazonSocial = empresa.RazonSocial,
                NombreComercial = empresa.NombreComercial,
                MonedaBaseCodigo = empresa.MonedaBase.Codigo,
                Ambiente = empresa.Ambiente.ToString(),
                Telefono = empresa.Telefono?.ToString() ?? string.Empty,
                Emails = empresa.Emails?.Select(e => e.Value).ToArray() ?? Array.Empty<string>(),
                PieDePagina = empresa.PieDePagina?.ToString() ?? string.Empty,
                MostrarImagenEnComprobanteImpresa = empresa.MostrarImagenEnComprobanteImpresa,
                // Por invariantes del dominio, DomicilioFiscal nunca es nulo en empresa
                DireccionFiscal = new ActualizarConfiguracionEmpresaOutputDto.DireccionFiscalOut
                {
                    PaisCodigo = "PE",
                    Ubigeo = empresa.DireccionFiscal!.Ubigeo!,
                    Direccion = empresa.DireccionFiscal!.Linea!
                },
                // El nombre 'principal' es solo un nombre de variable, no una lógica de principal/secundario
                EstablecimientoPrincipal = principal is null ? null
                    : new ActualizarConfiguracionEmpresaOutputDto.EstablecimientoOut
                    {
                        Id = principal.Id,
                        Codigo = principal.Codigo,
                        Nombre = principal.Nombre,
                        Direccion = principal.Direccion.Linea!, // Por contrato, siempre tiene dirección
                        Ubigeo = principal.Direccion.Ubigeo!    // Por contrato, siempre tiene ubigeo
                    }
            };
        }

        // ================= Helpers =================

        private static DomicilioFiscal MapDomicilioFiscal(ActualizarConfiguracionEmpresaInputDto.DireccionFiscalDto dto)
        {
            // Tu VO ya provee FromPeru(linea, ubigeo, ...)
            return DomicilioFiscal.FromPeru(
                linea: (dto.Direccion ?? string.Empty).Trim(),
                ubigeo: (dto.Ubigeo ?? string.Empty).Trim(),
                departamento: null,
                provincia: null,
                distrito: null,
                addressTypeCode: null
            );
        }

        private static Moneda MapMoneda(string codigoIso4217)
        {
            var c = (codigoIso4217 ?? "PEN").Trim().ToUpperInvariant();
            return c switch
            {
                "PEN" => Moneda.PEN(),
                "USD" => Moneda.USD(),
                _     => Moneda.Create(c)
            };
        }
    }
}