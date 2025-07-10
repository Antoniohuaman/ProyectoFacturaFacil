using System;
using System.Threading.Tasks;
using CatalogoArticulosBC.Application.Interfaces;
using CatalogoArticulosBC.Application.DTOs;

namespace CatalogoArticulosBC.Application.UseCases
{
    public class EditarProductoVarianteUseCase
    {
        private readonly ICatalogoArticulosRepository _repo;
        private readonly IUnitOfWork _uow;

        public EditarProductoVarianteUseCase(ICatalogoArticulosRepository repo, IUnitOfWork uow)
        {
            _repo = repo;
            _uow = uow;
        }

        public async Task HandleAsync(EditarProductoVarianteDto dto)
        {
            var variante = await _repo.GetProductoVarianteByIdAsync(dto.ProductoVarianteId);
            if (variante == null)
                throw new InvalidOperationException("Producto variante no encontrado");

            variante.Editar(dto.NuevoSku, dto.NuevosAtributos);

            await _repo.UpdateAsync(variante);
            await _uow.CommitAsync();
        }
    }
}