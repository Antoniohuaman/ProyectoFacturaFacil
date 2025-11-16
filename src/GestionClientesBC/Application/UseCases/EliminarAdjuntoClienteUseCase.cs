using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GestionClientesBC.Application.Interfaces; // IUnitOfWork
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.Entities;
using GestionClientesBC.Domain.Events;
using GestionClientesBC.Domain.Repositories;
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;

namespace GestionClientesBC.Application.Clientes.Adjuntos.Eliminar
{
    public interface IEliminarAdjuntoClienteUseCase
    {
        Task<EliminarAdjuntoClienteOutputDto> Handle(EliminarAdjuntoClienteInputDto input, CancellationToken ct = default);
    }

    /// <summary>
    /// Elimina un adjunto previamente cargado en la ficha del cliente.
    /// </summary>
    public sealed class EliminarAdjuntoClienteUseCase : IEliminarAdjuntoClienteUseCase
    {
        private readonly IClienteRepository _repo;
        private readonly IUnitOfWork _uow;
        private readonly ITenantContext _tenant;

        public EliminarAdjuntoClienteUseCase(IClienteRepository repo, IUnitOfWork uow, ITenantContext tenant)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        }

        public async Task<EliminarAdjuntoClienteOutputDto> Handle(EliminarAdjuntoClienteInputDto input, CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            if (input.ClienteId == Guid.Empty)
                throw new BusinessRuleException("ClienteId no puede ser vacío.");
            if (input.AdjuntoId == Guid.Empty)
                throw new BusinessRuleException("AdjuntoId no puede ser vacío.");

            var empresaId = _tenant.EmpresaId;
            if (empresaId is null || empresaId.IsEmpty)
                throw new BusinessRuleException("No se pudo resolver la Empresa actual.");

            var cliente = await _repo.GetByIdAsync(empresaId, input.ClienteId);
            if (cliente is null)
                throw NotFoundException.For<Cliente>(input.ClienteId);

            if (!cliente.EmpresaId.EsMismaEmpresaQue(empresaId))
                throw NotFoundException.For<Cliente>(input.ClienteId);

            var adjunto = cliente.Adjuntos.FirstOrDefault(a => a.AdjuntoId == input.AdjuntoId);
            if (adjunto is null)
                throw NotFoundException.For<AdjuntoCliente>(input.AdjuntoId);

            var expectedVersion = input.ExpectedVersion ?? cliente.Version;
            cliente.EliminarAdjunto(input.AdjuntoId);

            await _repo.UpdateAsync(cliente, expectedVersion);
            await _uow.CommitAsync(ct);

            var evento = cliente.DomainEvents
                .OfType<AdjuntoEliminado>()
                .LastOrDefault(e => e.AdjuntoId == input.AdjuntoId);

            return new EliminarAdjuntoClienteOutputDto
            {
                ClienteId = cliente.ClienteId,
                EmpresaId = empresaId.Value,
                AdjuntoId = input.AdjuntoId,
                TotalAdjuntos = cliente.Adjuntos.Count,
                FechaEventoUtc = evento?.OccurredOn ?? cliente.FechaUltimaModificacion,
                Version = cliente.Version
            };
        }
    }
}
