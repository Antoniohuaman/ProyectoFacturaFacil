// src/CatalogoArticulosBC/Application/UseCases/EditarItemUseCase.cs
using System;
using System.Threading.Tasks;
using CatalogoArticulosBC.Application.Interfaces;
using CatalogoArticulosBC.Domain.ValueObjects;


namespace CatalogoArticulosBC.Application.UseCases
{
    /// <summary>
    /// Actualiza atributos básicos de un ítem (Simple, Variante o Combo).
    /// </summary>
    public class EditarItemUseCase
    {
        private readonly ICatalogoArticulosRepository _repo;
        private readonly IUnitOfWork _uow;

        public EditarItemUseCase(ICatalogoArticulosRepository repo, IUnitOfWork uow)
        {
            _repo = repo;
            _uow  = uow;
        }

        public async Task HandleAsync(
            Guid   productoServicioId,
            string nuevaDescripcion,
            decimal nuevoPeso)
        {
            // 1. Obtener ítem
            var item = await _repo.GetByIdAsync(productoServicioId)
                       ?? throw new InvalidOperationException("Ítem no encontrado.");          // :contentReference[oaicite:3]{index=3}
            if (!item.Activo)
                throw new InvalidOperationException("No se puede editar ítem inactivo.");   // RN-CA-001

            // 2. Aplicar cambios
            item.ActualizarDescripcion(nuevaDescripcion);
            item.ActualizarPeso(new Peso(nuevoPeso));

            // 3. Persistir
            await _repo.UpdateAsync(item);
            await _uow.CommitAsync();
        }
    }
}
