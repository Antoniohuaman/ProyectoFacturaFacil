using System;
using System.Threading;
using System.Threading.Tasks;
using ConfiguracionSistemaBC.Domain.Aggregates;
using ConfiguracionSistemaBC.Domain.Repositories;
using ConfiguracionSistemaBC.Application.Interfaces;   // IUnitOfWork
using SharedKernel.Application.Interfaces;          // ITenantContext
using SharedKernel.ValueObjects;                    // DomicilioFiscal, EmpresaId

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Registra un nuevo establecimiento en la empresa del contexto actual (multiempresa).
    /// - Requiere EmpresaId en ITenantContext.
    /// - Usa concurrencia optimista (versión del aggregate).
    /// - Permite marcar como principal.
    /// </summary>
    public sealed class RegistrarEstablecimientoUseCase
    {
        private readonly IConfiguracionEmpresaRepository _repo;
        private readonly IUnitOfWork _uow;
        private readonly ITenantContext _tenant;

        public RegistrarEstablecimientoUseCase(
            IConfiguracionEmpresaRepository repo,
            IUnitOfWork uow,
            ITenantContext tenantContext)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _uow  = uow  ?? throw new ArgumentNullException(nameof(uow));
            _tenant = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        }

        public async Task<RegistrarEstablecimientoOutputDto> HandleAsync(
            RegistrarEstablecimientoInputDto input,
            CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            if (string.IsNullOrWhiteSpace(input.Codigo))
                throw new ArgumentNullException(nameof(input.Codigo), "El código es obligatorio.");
            if (string.IsNullOrWhiteSpace(input.Nombre))
                throw new ArgumentNullException(nameof(input.Nombre), "El nombre es obligatorio.");
            if (input.Direccion is null)
                throw new ArgumentNullException(nameof(input.Direccion), "La dirección es obligatoria.");
            if (!string.Equals(input.Direccion.PaisCodigo?.Trim(), "PE", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Actualmente solo se soporta país PE para establecimientos.", nameof(input.Direccion.PaisCodigo));

            // Empresa del contexto
            var empresaId = _tenant.EmpresaId
                ?? throw new InvalidOperationException("No hay EmpresaId en el contexto del tenant.");

            // Cargar aggregate
            var empresa = await _repo.GetByEmpresaIdAsync(empresaId, ct)
                ?? throw new KeyNotFoundException("No se encontró la configuración de la empresa.");

            // Concurrencia
            var expectedVersion = empresa.Version;

            // Mapear dirección a VO
            var dom = DomicilioFiscal.FromPeru(
                linea: input.Direccion.Direccion?.Trim() ?? string.Empty,
                ubigeo: input.Direccion.Ubigeo?.Trim() ?? string.Empty,
                departamento: null,
                provincia: null,
                distrito: null,
                addressTypeCode: null
            );

            // Crear
            var nuevoId = empresa.RegistrarEstablecimiento(
                input.Codigo.Trim(),
                input.Nombre.Trim(),
                dom
            );

            // Eliminado: ya no se permite marcar como principal desde el registro de establecimiento.

            // Persistir (optimista)
            var ok = await _repo.UpdateIfVersionMatchAsync(empresa, expectedVersion, ct);
            if (!ok)
                throw new InvalidOperationException("No se pudo guardar los cambios por conflicto de concurrencia. Intente nuevamente.");

            await _uow.CommitAsync(ct);

            // Salida
            var principal = empresa.ObtenerEstablecimientoPrincipal();
            var esPrincipal = principal is not null && principal.Id == nuevoId;

            return new RegistrarEstablecimientoOutputDto
            {
                EmpresaId = empresa.EmpresaId.Value,
                EstablecimientoId = nuevoId,
                Codigo = input.Codigo.Trim(),
                Nombre = input.Nombre.Trim(),
                Direccion = dom.Linea ?? string.Empty,
                Ubigeo = dom.Ubigeo ?? string.Empty,
                EsPrincipal = esPrincipal
            };
        }
    }
}
