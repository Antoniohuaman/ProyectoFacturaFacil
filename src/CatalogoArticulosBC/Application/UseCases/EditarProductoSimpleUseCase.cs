using System;
using System.Threading.Tasks;
using CatalogoArticulosBC.Application.DTOs;
using CatalogoArticulosBC.Application.Interfaces;

namespace CatalogoArticulosBC.Application.UseCases
{
    public class EditarProductoSimpleUseCase
    {
        private readonly ICatalogoArticulosRepository _repo;
        private readonly IUnitOfWork _uow;

        public EditarProductoSimpleUseCase(ICatalogoArticulosRepository repo, IUnitOfWork uow)
        {
            _repo = repo;
            _uow = uow;
        }

        public async Task HandleAsync(EditarProductoSimpleDto dto)
        {
            var producto = await _repo.GetByIdAsync(dto.ProductoId)
                ?? throw new Exception("Producto no encontrado.");

            producto.EditarDatos(dto.NuevoNombre, dto.NuevaDescripcion, dto.NuevoPrecio);

            await _repo.UpdateAsync(producto);
            await _uow.CommitAsync();
        }
    }
}