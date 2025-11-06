using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConfiguracionSistemaBC.Domain.Aggregates;
using ConfiguracionSistemaBC.Domain.Repositories;
using ConfiguracionSistemaBC.Application.Interfaces;      // IUnitOfWork
using ConfiguracionSistemaBC.Domain.ValueObjects;   // AmbienteFe, LogoImagen, PieDePagina, Ruc (domain)
using SharedKernel.Application.Interfaces;          // ITenantContext
using SharedKernel.ValueObjects;                    // DomicilioFiscal, Email, Moneda, Telefono

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Crea la configuración inicial de una empresa (multiempresa/multitenant).
    /// - Valida que no exista otra configuración con el mismo RUC.
    /// - Crea el aggregate ConfiguracionEmpresa con bootstrap:
    ///   Establecimiento principal (01), Formas de pago y Unidades de medida.
    /// - Aplica (si vienen) preferencias: teléfono, emails, pie de página, logo.
    /// - Opcional: cambia el ambiente a PRODUCCIÓN (si lo pides y la transición es válida).
    /// </summary>
    public sealed class RegistrarConfiguracionEmpresaUseCase
    {
        private readonly IConfiguracionEmpresaRepository _repo;
        private readonly IUnitOfWork _uow;
        private readonly ITenantContext? _tenant; // En alta inicial puede venir null en algunos flujos

        public RegistrarConfiguracionEmpresaUseCase(
            IConfiguracionEmpresaRepository repo,
            IUnitOfWork uow,
            ITenantContext? tenantContext = null)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _uow  = uow  ?? throw new ArgumentNullException(nameof(uow));
            _tenant = tenantContext; // puede no usarse en el alta
        }

        public async Task<RegistrarConfiguracionEmpresaOutputDto> HandleAsync(
            RegistrarConfiguracionEmpresaInputDto input,
            CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            if (string.IsNullOrWhiteSpace(input.Ruc))
                throw new ArgumentNullException(nameof(input.Ruc), "RUC es obligatorio.");
            if (string.IsNullOrWhiteSpace(input.RazonSocial))
                throw new ArgumentNullException(nameof(input.RazonSocial), "Razón social es obligatoria.");

            // ---- Mapear DTO → Value Objects (dominio / shared kernel)
            var ruc = MapRuc(input.Ruc);
            var direccionFiscal = MapDomicilioFiscal(input.DireccionFiscal);
            var monedaBase = MapMoneda(input.MonedaCodigo ?? "PEN");

            // ---- Idempotencia básica por RUC
            var yaExiste = await _repo.FindByRucAsync(ruc, ct);
            if (yaExiste is not null)
                throw new InvalidOperationException($"Ya existe una configuración registrada para el RUC {ruc}.");

            // ---- Crear aggregate con valores base (arranca en PRUEBA)
            var empresa = ConfiguracionEmpresa.RegistrarNueva(
                ruc,
                input.RazonSocial.Trim(),
                direccionFiscal,
                monedaBase);

            // Nombre comercial (si viene)
            if (!string.IsNullOrWhiteSpace(input.NombreComercial))
            {
                empresa.ActualizarDatosLegales(
                    ruc,
                    input.RazonSocial.Trim(),
                    direccionFiscal,
                    input.NombreComercial!.Trim());
            }

            // Preferencias opcionales
            if (!string.IsNullOrWhiteSpace(input.Telefono))
                empresa.ReemplazarTelefono(MapTelefono(input.Telefono!));

            if (input.Emails is not null && input.Emails.Length > 0)
                empresa.ReemplazarEmails(input.Emails.Where(s => !string.IsNullOrWhiteSpace(s))
                                                     .Select(MapEmail)
                                                     .ToList());

            if (!string.IsNullOrWhiteSpace(input.PieDePagina))
                empresa.ActualizarPieDePagina(PieDePagina.FromTextoPlano(input.PieDePagina!.Trim()));

            // LogoImagen requiere metadatos adicionales, ajustar según el DTO y lógica de dominio
            // if (input.LogoBytes is not null && input.LogoBytes.Length > 0)
            //     empresa.EstablecerLogo(LogoImagen.FromUpload(...));
            // TODO: Ajustar lógica para LogoImagen según los datos disponibles en el DTO

            // Personalizar (opcional) el establecimiento principal
            var estPrincipal = empresa.ObtenerEstablecimientoPrincipal();
            if (estPrincipal is not null)
            {
                if (!string.IsNullOrWhiteSpace(input.EstablecimientoCodigo) &&
                    !estPrincipal.Codigo.Equals(input.EstablecimientoCodigo.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    empresa.RecodificarEstablecimiento(estPrincipal.Id, input.EstablecimientoCodigo.Trim());
                    estPrincipal = empresa.ObtenerEstablecimientoPrincipal();
                }

                if (!string.IsNullOrWhiteSpace(input.EstablecimientoNombre) &&
                    !estPrincipal!.Nombre.Equals(input.EstablecimientoNombre.Trim(), StringComparison.Ordinal))
                {
                    empresa.ActualizarEstablecimiento(estPrincipal.Id, input.EstablecimientoNombre.Trim(), direccionFiscal);
                    estPrincipal = empresa.ObtenerEstablecimientoPrincipal();
                }
            }

            // Cambiar ambiente inicial si lo piden (PRUEBA → PRODUCCION)
            var ambienteDeseado = ParseAmbienteOrNull(input.Ambiente);
            if (ambienteDeseado is not null && ambienteDeseado != AmbienteFe.PRUEBA)
            {
                // el aggregate ya valida la transición mediante AmbienteFe.ValidarTransicion
                empresa.CambiarAmbiente(ambienteDeseado);
            }

            // Persistencia
            await _repo.AddAsync(empresa, ct);
            await _uow.CommitAsync(ct);

            // Salida
            var principal = empresa.ObtenerEstablecimientoPrincipal();
            return new RegistrarConfiguracionEmpresaOutputDto
            {
                EmpresaId = empresa.EmpresaId.Value,
                Ruc = input.Ruc.Trim(),
                RazonSocial = empresa.RazonSocial,
                NombreComercial = empresa.NombreComercial,
                Ambiente = empresa.Ambiente.ToString(),
                MonedaBaseCodigo = empresa.MonedaBase.Codigo,
                DireccionFiscal = new RegistrarConfiguracionEmpresaOutputDto.DireccionFiscalOut
                {
                    PaisCodigo = "PE",
                    Ubigeo = input.DireccionFiscal?.Ubigeo ?? "",
                    Direccion = input.DireccionFiscal?.Direccion ?? ""
                },
                EstablecimientoPrincipal = principal is null
                    ? null
                    : new RegistrarConfiguracionEmpresaOutputDto.EstablecimientoOut
                    {
                        Id = principal.Id,
                        Codigo = principal.Codigo,
                        Nombre = principal.Nombre,
                        Direccion = principal.Direccion?.Linea ?? string.Empty,
                        Ubigeo = principal.Direccion?.Ubigeo ?? string.Empty
                    },
                FormasDePagoPreCreadas = empresa.ListarFormasDePago().Count,
                UnidadesDeMedidaPreCreadas = empresa.ListarUnidadesDeMedida().Count
            };
        }

        // -------------------- Mapeos/helpers internos --------------------

        private static Ruc MapRuc(string raw) => Ruc.From(raw.Trim());

        private static DomicilioFiscal MapDomicilioFiscal(RegistrarConfiguracionEmpresaInputDto.DireccionFiscalDto? dto)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto), "La dirección fiscal es obligatoria.");
            // Modelo Perú (Catálogo SUNAT): Usamos helper del VO.
            // Si tu VO expone otra fábrica, ajusta aquí.
            return DomicilioFiscal.FromPeru(
                linea: dto.Direccion?.Trim() ?? "",
                ubigeo: dto.Ubigeo?.Trim() ?? "",
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
                _     => Moneda.Create(c)  // tu VO admite más divisas
            };
        }

    private static Telefono MapTelefono(string raw) => Telefono.FromTexto(raw.Trim());

    private static Email MapEmail(string raw) => Email.Create(raw.Trim());

        private static AmbienteFe? ParseAmbienteOrNull(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var v = raw.Trim().ToUpperInvariant();
            return v switch
            {
                "PRUEBA"      => AmbienteFe.PRUEBA,
                "PRODUCCION"  => AmbienteFe.PRODUCCION,
                _             => null
            };
        }
    }
}
