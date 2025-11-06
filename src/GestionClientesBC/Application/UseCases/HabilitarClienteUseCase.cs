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

namespace GestionClientesBC.Application.Clientes.Habilitar
{
    public interface IHabilitarClienteUseCase
    {
        Task<HabilitarClienteOutputDto> Handle(HabilitarClienteInputDto input, CancellationToken ct = default);
    }

    /// <summary>
    /// Habilita un cliente de la empresa (tenant) actual.
    /// </summary>
    public sealed class HabilitarClienteUseCase : IHabilitarClienteUseCase
    {
        private readonly IClienteRepository _repo;
        private readonly IUnitOfWork _uow;
        private readonly ITenantContext _tenant;

        public HabilitarClienteUseCase(IClienteRepository repo, IUnitOfWork uow, ITenantContext tenant)
        {
            _repo   = repo   ?? throw new ArgumentNullException(nameof(repo));
            _uow    = uow    ?? throw new ArgumentNullException(nameof(uow));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        }

        public async Task<HabilitarClienteOutputDto> Handle(HabilitarClienteInputDto input, CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            if (input.ClienteId == Guid.Empty)
                throw new BusinessRuleException("ClienteId no puede ser vacío.");

            var empresaId = _tenant.EmpresaId;
            if (empresaId is null || empresaId.IsEmpty)
                throw new BusinessRuleException("No se pudo resolver la Empresa actual.");


            // 1) Cargar agregado (firma nueva: empresaId, clienteId)
            var cliente = await _repo.GetByIdAsync(empresaId, input.ClienteId);
            if (cliente is null)
                throw NotFoundException.For<Cliente>(input.ClienteId);

            // 2) Validar pertenencia a empresa actual
            if (!cliente.EmpresaId.EsMismaEmpresaQue(empresaId))
                throw NotFoundException.For<Cliente>(input.ClienteId);

            // 3) Habilitar (regla de negocio: lanza si ya estaba habilitado)
            cliente.Habilitar();

            // 4) Persistir
            await _repo.UpdateAsync(cliente);
            await _uow.CommitAsync(ct);

            // 5) Obtener fecha del evento si existe
            var evento = cliente.DomainEvents.OfType<ClienteHabilitado>().LastOrDefault();
            var fechaEventoUtc = evento?.OccurredOn ?? DateTime.UtcNow;

            // 6) Salida
            return new HabilitarClienteOutputDto
            {
                ClienteId = cliente.ClienteId,
                EmpresaId = empresaId.Value,
                Habilitado = true,
                EstadoCodigo = cliente.Estado?.Codigo ?? string.Empty, // "HAB"
                FechaHabilitacionUtc = fechaEventoUtc,
                TipoDocumento = cliente.Documento.Tipo.ToString(),
                NumeroDocumento = cliente.Documento.Numero
            };
        }
    }
}
