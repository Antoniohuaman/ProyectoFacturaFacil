using System;
using System.Threading.Tasks;
using ListaPreciosBC.Application.Interfaces;
using ListaPreciosBC.Domain.Aggregates;
using ListaPreciosBC.Domain.ValueObjects;
using ListaPreciosBC.Domain.Entities;

namespace ListaPreciosBC.Application.UseCases
{
    public class ObtenerListaPrecioVigenteUseCase
    {
        private readonly IListaPrecioRepository _repo;

        public ObtenerListaPrecioVigenteUseCase(IListaPrecioRepository repo)
        {
            _repo = repo;
        }

        public async Task<ListaPrecio?> HandleAsync(
            TipoLista tipoLista,
            CriterioLista criterio,
            Guid productoId,
            DateTime fecha)
        {
            return await _repo.GetVigentePorCriterioAsync(tipoLista, criterio, productoId, fecha);
        }
    }
}