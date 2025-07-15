using System;
using System.Threading.Tasks;
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.ValueObjects;
using GestionClientesBC.Domain.Entities;

namespace GestionClientesBC.Application.Interfaces
{
    public interface IGestionClientesRepository
    {
        Task<Cliente?> GetByIdAsync(Guid clienteId); // <-- Agrega este método
        Task<Cliente?> GetByDocumentoIdentidadAsync(DocumentoIdentidad docId);
        Task AddAsync(Cliente cliente); // <-- Renombra para consistencia
        Task UpdateAsync(Cliente cliente); // <-- Renombra para consistencia
                                           // Agrega otros métodos según tus necesidades
                                           // En IGestionClientesRepository.cs
        Task DeleteAsync(Guid clienteId);

        // Métodos necesarios para ConsultarClienteUseCase
        Task<ICollection<ContactoCliente>> ObtenerContactosPorClienteIdAsync(Guid clienteId);
        Task<ICollection<AdjuntoCliente>> ObtenerAdjuntosPorClienteIdAsync(Guid clienteId);
        Task<ICollection<OperacionCliente>> ObtenerOperacionesPorClienteIdAsync(Guid clienteId, DateTime desde);
        Task<ICollection<OperacionCliente>> ObtenerOperacionesPorClienteIdAsync(
        Guid clienteId,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        TipoOperacion? tipoOperacion,
        int? page,
        int? pageSize);
        // Métodos necesarios para RegistrarOperacionClienteUseCase
        Task RegistrarOperacionClienteAsync(Guid clienteId, OperacionCliente operacion);
                                                     
    }
}