using System;
using System.Threading;
using System.Threading.Tasks;
using ConfiguracionSistemaBC.Domain.Aggregates;
using ConfiguracionSistemaBC.Domain.Repositories;
using ConfiguracionSistemaBC.Domain.ValueObjects;    // AmbienteFe
using SharedKernel.Application.Interfaces;           // ITenantContext
using SharedKernel.ValueObjects;                     // EmpresaId

namespace ConfiguracionSistemaBC.Application.Ports
{
    /// <summary>
    /// Puerto de aplicación para borrar (purgar) documentos emitidos en ambiente PRUEBA.
    /// Su implementación real vive en Infra/Adapters del BC de emisión electrónica.
    /// </summary>
    public interface IDocumentosElectronicosPurgeService
    {
        Task PurgeTestDocumentsAsync(EmpresaId empresaId, CancellationToken ct = default);
    }
}

namespace ConfiguracionSistemaBC.Application.UseCases
{
    using ConfiguracionSistemaBC.Application.Ports;

    /// <summary>
    /// Cambia el ambiente de la empresa del contexto.
    /// - Si el destino es PRODUCCION y se solicita purga, elimina los documentos emitidos en PRUEBA.
    /// - Idempotencia suave: si el ambiente ya es el destino, no actualiza ni purga.
    /// </summary>
    public sealed class CambiarAmbienteEmpresaUseCase
    {
        private readonly IConfiguracionEmpresaRepository _repo;
        private readonly IUnitOfWork _uow;
        private readonly ITenantContext _tenant;
        private readonly IDocumentosElectronicosPurgeService? _purge;

        public CambiarAmbienteEmpresaUseCase(
            IConfiguracionEmpresaRepository repo,
            IUnitOfWork uow,
            ITenantContext tenantContext,
            IDocumentosElectronicosPurgeService? purgeService = null)
        {
            _repo   = repo ?? throw new ArgumentNullException(nameof(repo));
            _uow    = uow  ?? throw new ArgumentNullException(nameof(uow));
            _tenant = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
            _purge  = purgeService; // opcional: si se exige purga y es null, se lanza
        }

        public async Task<CambiarAmbienteEmpresaOutputDto> HandleAsync(
            CambiarAmbienteEmpresaInputDto input,
            CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));

            var empresaId = _tenant.EmpresaId
                ?? throw new InvalidOperationException("No hay EmpresaId en el contexto del tenant.");

            var empresa = await _repo.GetByEmpresaIdAsync(empresaId, ct)
                ?? throw new KeyNotFoundException("No se encontró la configuración de la empresa.");

            var destino = ParseAmbiente(input.Destino);
            var anterior = empresa.Ambiente;

            // Idempotencia suave: nada que hacer
            if (destino == anterior)
            {
                return new CambiarAmbienteEmpresaOutputDto
                {
                    EmpresaId = empresa.EmpresaId.Value,
                    AmbienteAnterior = anterior.ToString(),
                    AmbienteActual = anterior.ToString(),
                    PurgaEjecutada = false,
                    FechaCambioUtc = DateTime.UtcNow
                };
            }

            // Guardamos versión para concurrencia
            var expectedVersion = empresa.Version;

            // Delega la validación al dominio (AmbienteFe.ValidarTransicion)
            empresa.CambiarAmbiente(destino);

            // Persistimos con concurrencia optimista
            var ok = await _repo.UpdateIfVersionMatchAsync(empresa, expectedVersion, ct);
            if (!ok)
                throw new InvalidOperationException("Conflicto de concurrencia al guardar el cambio de ambiente.");

            await _uow.SaveChangesAsync(ct);

            var purga = false;
            if (destino == AmbienteFe.PRODUCCION && input.BorrarDocumentosEmitidosEnPrueba)
            {
                if (_purge is null)
                    throw new InvalidOperationException("Se solicitó purga de documentos, pero no hay servicio configurado.");

                await _purge.PurgeTestDocumentsAsync(empresa.EmpresaId, ct);
                purga = true;
            }

            return new CambiarAmbienteEmpresaOutputDto
            {
                EmpresaId = empresa.EmpresaId.Value,
                AmbienteAnterior = anterior.ToString(),
                AmbienteActual = destino.ToString(),
                PurgaEjecutada = purga,
                FechaCambioUtc = DateTime.UtcNow
            };
        }

        private static AmbienteFe ParseAmbiente(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                throw new ArgumentNullException(nameof(raw), "Ambiente destino es obligatorio.");
            var v = raw.Trim().ToUpperInvariant();
            return v switch
            {
                "PRUEBA"     => AmbienteFe.PRUEBA,
                "PRODUCCION" => AmbienteFe.PRODUCCION,
                _ => throw new ArgumentException("Ambiente destino inválido. Use PRUEBA o PRODUCCION.", nameof(raw))
            };
        }
    }
}
