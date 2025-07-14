using System;
using System.Threading.Tasks;
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.ValueObjects;

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
    }
}