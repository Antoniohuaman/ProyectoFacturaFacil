using System;
using System.Threading.Tasks;
using ListaPreciosBC.Domain.Aggregates;
using ListaPreciosBC.Domain.ValueObjects;
using ListaPreciosBC.Domain.Entities;

namespace ListaPreciosBC.Application.Interfaces
{
    public interface IListaPrecioRepository
    {
        Task<ListaPrecio?> GetByIdAsync(Guid listaPrecioId);
        Task<ListaPrecio?> GetVigentePorCriterioAsync(TipoLista tipoLista, CriterioLista criterio, Guid productoId, DateTime fecha);
        Task AddAsync(ListaPrecio listaPrecio);
        Task UpdateAsync(ListaPrecio listaPrecio);
        // Otros métodos según necesidades (ej: búsqueda por producto, etc.)
    }
}