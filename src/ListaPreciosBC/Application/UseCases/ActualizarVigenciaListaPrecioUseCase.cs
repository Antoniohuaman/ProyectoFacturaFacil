using System;
using System.Threading.Tasks;
using ListaPreciosBC.Application.Interfaces;
using ListaPreciosBC.Domain.ValueObjects;

namespace ListaPreciosBC.Application.UseCases
{
    public class ActualizarVigenciaListaPrecioUseCase
    {
        private readonly IListaPrecioRepository _repo;
        private readonly IUnitOfWork _uow;

        public ActualizarVigenciaListaPrecioUseCase(IListaPrecioRepository repo, IUnitOfWork uow)
        {
            _repo = repo;
            _uow = uow;
        }

        public async Task HandleAsync(Guid listaPrecioId, PeriodoVigencia nuevaVigencia)
        {
            var lista = await _repo.GetByIdAsync(listaPrecioId)
                ?? throw new Exception("Lista de precios no encontrada.");

            lista.ActualizarVigencia(nuevaVigencia);

            await _repo.UpdateAsync(lista);
            await _uow.CommitAsync();
        }
    }
}