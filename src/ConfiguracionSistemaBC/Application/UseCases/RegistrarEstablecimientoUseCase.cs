using System;
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
    /// Caso de uso: registrar un nuevo Establecimiento en la configuración de empresa (tenant).
    /// Reglas:
    /// - Debe existir la configuración del tenant.
    /// - El código del establecimiento debe ser único por empresa (lo valida el aggregate).
    /// - El establecimiento inicia habilitado.
    /// - La dirección se valida según las reglas de <see cref="DireccionPostal"/>.
    /// </summary>
    public sealed class RegistrarEstablecimientoUseCase
    {
        private readonly IConfiguracionEmpresaRepository _repo;
        private readonly IUnitOfWork _uow;

        public RegistrarEstablecimientoUseCase(
            IConfiguracionEmpresaRepository repo,
            IUnitOfWork uow)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _uow  = uow  ?? throw new ArgumentNullException(nameof(uow));
        }

        // -------------------- Contratos de entrada/salida --------------------

        /// <summary>Parámetros del caso de uso.</summary>
        public sealed record Params(
            Guid TenantId,
            EstablecimientoParams Establecimiento
        );

        /// <summary>Parámetros del establecimiento a registrar.</summary>
        public sealed record EstablecimientoParams(
            string Codigo,
            string Nombre,
            string DireccionLinea,
            string Ubigeo,
            string Departamento,
            string Provincia,
            string Distrito,
            string? PaisIso = "PE",
            string? AddressTypeCode = "0000"
        );

        /// <summary>Resultado con los datos del establecimiento recién creado.</summary>
        public sealed record Result(
            Guid EstablecimientoId,
            string Codigo,
            string Nombre,
            string Ubigeo,
            string Departamento,
            string Provincia,
            string Distrito,
            string DireccionLinea,
            string PaisIso,
            string AddressTypeCode
        );

        // -------------------- Ejecución --------------------

        public async Task<Result> ExecuteAsync(Params input, CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            if (input.TenantId == Guid.Empty)
                throw new ArgumentException("TenantId inválido.", nameof(input));

            if (input.Establecimiento is null)
                throw new ArgumentNullException(nameof(input.Establecimiento));

            var e = input.Establecimiento;

            if (string.IsNullOrWhiteSpace(e.Codigo))
                throw new ArgumentNullException(nameof(e.Codigo), "El código del establecimiento es obligatorio.");
            if (string.IsNullOrWhiteSpace(e.Nombre))
                throw new ArgumentNullException(nameof(e.Nombre), "El nombre del establecimiento es obligatorio.");

            // 1) Cargar aggregate existente del tenant
            var agg = await _repo.GetByTenantIdAsync(input.TenantId, ct);
            if (agg is null)
                throw new InvalidOperationException("La configuración de empresa para este tenant aún no existe.");

            // 2) Construir VO de dirección (solo Perú soportado)
            var direccion = DireccionPostal.FromPeru(
                linea: e.DireccionLinea,
                ubigeo: e.Ubigeo,
                departamento: e.Departamento,
                provincia: e.Provincia,
                distrito: e.Distrito,
                addressTypeCode: e.AddressTypeCode ?? "0000"
            );

            // 3) Registrar en aggregate (valida unicidad de código y establecimiento habilitado)
            var nuevoId = agg.RegistrarEstablecimiento(
                codigo: e.Codigo.Trim(),
                nombre: e.Nombre.Trim(),
                direccion: direccion
            );

            // 4) Persistir
            await _uow.SaveChangesAsync(ct);

            // 5) Mapear a Result desde el estado actual del aggregate
            var creado = agg.ListarEstablecimientos().First(x => x.Id == nuevoId);
            var d = creado.Direccion;

            return new Result(
                EstablecimientoId: creado.Id,
                Codigo: creado.Codigo,
                Nombre: creado.Nombre,
                Ubigeo: d.Ubigeo,
                Departamento: d.Departamento,
                Provincia: d.Provincia,
                Distrito: d.Distrito,
                DireccionLinea: d.Linea,
                PaisIso: d.PaisCodigoIso,
                AddressTypeCode: d.AddressTypeCode
            );
        }
    }
}