using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.Entities;
using GestionClientesBC.Domain.Events;
using GestionClientesBC.Domain.Repositories;
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;
using GestionClientesBC.Application.Interfaces; // IUnitOfWork

namespace GestionClientesBC.Application.Clientes.Adjuntos.Ingresar
{
    public interface IIngresarAdjuntoClienteUseCase
    {
        Task<IngresarAdjuntoClienteOutputDto> Handle(IngresarAdjuntoClienteInputDto input, CancellationToken ct = default);
    }

    /// <summary>
    /// Registra un adjunto en un cliente del tenant/empresa actual.
    /// </summary>
    public sealed class IngresarAdjuntoClienteUseCase : IIngresarAdjuntoClienteUseCase
    {
        private readonly IClienteRepository _repo;
        private readonly IUnitOfWork _uow;
        private readonly ITenantContext _tenant;

        public IngresarAdjuntoClienteUseCase(IClienteRepository repo, IUnitOfWork uow, ITenantContext tenant)
        {
            _repo   = repo   ?? throw new ArgumentNullException(nameof(repo));
            _uow    = uow    ?? throw new ArgumentNullException(nameof(uow));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        }

        public async Task<IngresarAdjuntoClienteOutputDto> Handle(IngresarAdjuntoClienteInputDto input, CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            if (input.ClienteId == Guid.Empty)
                throw new BusinessRuleException("ClienteId no puede ser vacío.");

            if (string.IsNullOrWhiteSpace(input.NombreArchivo))
                throw new BusinessRuleException("NombreArchivo es obligatorio.");

            if (string.IsNullOrWhiteSpace(input.Ruta))
                throw new BusinessRuleException("Ruta es obligatoria.");

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

            // 3) Normalizar id y fecha
            var adjuntoId = (input.AdjuntoId.HasValue && input.AdjuntoId.Value != Guid.Empty)
                ? input.AdjuntoId.Value
                : Guid.NewGuid();

            var fecha = input.FechaSubida ?? DateTime.UtcNow;
            var fechaUtc = fecha.Kind == DateTimeKind.Utc ? fecha : fecha.ToUniversalTime();

            // 4) Crear entidad de adjunto y agregar al agregado
            var adjunto = new AdjuntoCliente(adjuntoId, input.NombreArchivo.Trim(), input.Ruta.Trim(), fechaUtc, input.Comentario);
            cliente.AgregarAdjunto(adjunto);

            // 5) Persistir
            await _repo.UpdateAsync(cliente);
            await _uow.CommitAsync(ct);

            // 6) Obtener evento para trazabilidad
            var ev = cliente.DomainEvents.OfType<AdjuntoAgregado>().LastOrDefault();

            // 7) Retorno
            return new IngresarAdjuntoClienteOutputDto
            {
                ClienteId = cliente.ClienteId,
                EmpresaId = empresaId.Value,
                AdjuntoId = adjunto.AdjuntoId,
                NombreArchivo = adjunto.NombreArchivo,
                Ruta = adjunto.Ruta,
                Comentario = adjunto.Comentario,
                FechaSubidaUtc = adjunto.FechaSubida, // ya está en UTC
                TotalAdjuntosCliente = cliente.Adjuntos.Count,
                FechaEventoUtc = ev?.OccurredOn
            };
        }
    }
}
