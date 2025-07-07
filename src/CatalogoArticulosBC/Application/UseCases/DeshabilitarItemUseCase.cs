// src/CatalogoArticulosBC/Application/UseCases/DeshabilitarItemUseCase.cs
using System;
using System.Threading.Tasks;
using CatalogoArticulosBC.Application.Interfaces;

namespace CatalogoArticulosBC.Application.UseCases
{
    /// <summary>
    /// Marca un ítem como inactivo (soft‐delete).
    /// </summary>
    public class DeshabilitarItemUseCase
    {
        private readonly ICatalogoArticulosRepository _repo;
        private readonly IUnitOfWork _uow;

        public DeshabilitarItemUseCase(ICatalogoArticulosRepository repo, IUnitOfWork uow)
        {
            _repo = repo;
            _uow  = uow;
        }

        public async Task HandleAsync(Guid productoServicioId, string motivo)
        {
            var item = await _repo.GetByIdAsync(productoServicioId)
                       ?? throw new InvalidOperationException("Ítem no encontrado.");       // :contentReference[oaicite:4]{index=4}

            item.Deshabilitar(motivo);
            await _repo.UpdateAsync(item);
            await _uow.CommitAsync();
        }
    }
}
