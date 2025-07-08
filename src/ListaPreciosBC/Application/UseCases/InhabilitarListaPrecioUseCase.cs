using System;
using System.Threading.Tasks;
using ListaPreciosBC.Application.Interfaces;

namespace ListaPreciosBC.Application.UseCases
{
    public class InhabilitarListaPrecioUseCase
    {
        private readonly IListaPrecioRepository _repo;
        private readonly IUnitOfWork _uow;

        public InhabilitarListaPrecioUseCase(IListaPrecioRepository repo, IUnitOfWork uow)
        {
            _repo = repo;
            _uow = uow;
        }

        public async Task HandleAsync(Guid listaPrecioId, string usuarioId)
        {
            var lista = await _repo.GetByIdAsync(listaPrecioId)
                ?? throw new Exception("Lista de precios no encontrada.");

            lista.Inhabilitar(usuarioId);

            await _repo.UpdateAsync(lista);
            await _uow.CommitAsync();
        }
    }
}