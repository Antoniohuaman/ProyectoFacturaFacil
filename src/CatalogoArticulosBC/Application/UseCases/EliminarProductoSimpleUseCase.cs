using System;
using System.Threading.Tasks;
using CatalogoArticulosBC.Application.Interfaces;
using CatalogoArticulosBC.Application.DTOs;



namespace CatalogoArticulosBC.Application.UseCases
{
    public class EliminarProductoSimpleUseCase
    {
        private readonly ICatalogoArticulosRepository _repo;
        private readonly IUnitOfWork _uow;

        public EliminarProductoSimpleUseCase(ICatalogoArticulosRepository repo, IUnitOfWork uow)
        {
            _repo = repo;
            _uow = uow;
        }

        public async Task HandleAsync(EliminarProductoSimpleDto dto)
        {
            var producto = await _repo.GetByIdAsync(dto.ProductoId);
            if (producto == null)
                throw new InvalidOperationException("Producto simple no encontrado");

            await _repo.EliminarProductoSimpleAsync(dto.ProductoId);
            await _uow.CommitAsync();
        }
    }
}