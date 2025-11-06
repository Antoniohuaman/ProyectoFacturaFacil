using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.Repositories;
using GestionClientesBC.Domain.Events;
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;
using GestionClientesBC.Application.Interfaces; // IUnitOfWork

namespace GestionClientesBC.Application.Clientes.Deshabilitar
{
    public interface IDeshabilitarClienteUseCase
    {
        Task<DeshabilitarClienteOutputDto> Handle(DeshabilitarClienteInputDto input, CancellationToken ct = default);
    }

    /// <summary>
    /// Deshabilita un cliente del tenant/empresa actual. Sin condiciones adicionales.
    /// </summary>
    public sealed class DeshabilitarClienteUseCase : IDeshabilitarClienteUseCase
    {
        private readonly IClienteRepository _repo;
        private readonly IUnitOfWork _uow;
        private readonly ITenantContext _tenant;

        public DeshabilitarClienteUseCase(IClienteRepository repo, IUnitOfWork uow, ITenantContext tenant)
        {
            _repo   = repo   ?? throw new ArgumentNullException(nameof(repo));
            _uow    = uow    ?? throw new ArgumentNullException(nameof(uow));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        }

        public async Task<DeshabilitarClienteOutputDto> Handle(DeshabilitarClienteInputDto input, CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            if (input.ClienteId == Guid.Empty)
                throw new BusinessRuleException("ClienteId no puede ser vacío.");

            var empresaId = _tenant.EmpresaId;
            if (empresaId is null || empresaId.IsEmpty)
                throw new BusinessRuleException("No se pudo resolver la Empresa actual.");

            // 1) Cargar agregado
            var cliente = await _repo.GetByIdAsync(empresaId, input.ClienteId);
            if (cliente is null)
                throw NotFoundException.For<Cliente>(input.ClienteId);

            // 2) Validar pertenencia a la empresa actual
            if (!cliente.EmpresaId.EsMismaEmpresaQue(empresaId))
                throw NotFoundException.For<Cliente>(input.ClienteId);

            // 3) Normalizar fecha a UTC
            var fecha = input.FechaDeshabilitacion ?? DateTime.UtcNow;
            var fechaUtc = fecha.Kind == DateTimeKind.Utc ? fecha : fecha.ToUniversalTime();

            // 4) Deshabilitar (el agregado registra evento)
            cliente.Deshabilitar(input.Motivo, fechaUtc);

            // 5) Persistir
            await _repo.UpdateAsync(cliente);
            await _uow.CommitAsync(ct);

            // 6) Tomar OccurredOn del evento si está disponible
            var evento = cliente.DomainEvents.OfType<ClienteDeshabilitado>().LastOrDefault();
            var fechaEventoUtc = evento?.OccurredOn ?? fechaUtc;

            // 7) Salida
            return new DeshabilitarClienteOutputDto
            {
                ClienteId = cliente.ClienteId,
                EmpresaId = empresaId.Value,
                Deshabilitado = true,
                EstadoCodigo = cliente.Estado?.Codigo ?? string.Empty, // "INH"
                FechaDeshabilitacionUtc = cliente.FechaDeshabilitacion ?? fechaEventoUtc,
                MotivoDeshabilitacion = cliente.MotivoDeshabilitacion,
                TipoDocumento = cliente.Documento.Tipo.ToString(),
                NumeroDocumento = cliente.Documento.Numero
            };
        }
    }
}
