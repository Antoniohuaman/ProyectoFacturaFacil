using System.Threading.Tasks;
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.ValueObjects;

namespace GestionClientesBC.Application.Interfaces
{
    public interface IGestionClientesRepository
    {
        Task<Cliente?> GetByDocumentoIdentidadAsync(DocumentoIdentidad docId);
        Task AddClienteAsync(Cliente cliente);
        Task UpdateClienteAsync(Cliente cliente);
        // Agrega otros métodos según tus necesidades
    }
}