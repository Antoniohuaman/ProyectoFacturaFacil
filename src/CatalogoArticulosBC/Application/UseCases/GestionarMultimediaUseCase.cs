// src/CatalogoArticulosBC/Application/UseCases/GestionarMultimediaUseCase.cs
using System;
using System.Threading.Tasks;
using CatalogoArticulosBC.Application.Interfaces;
using CatalogoArticulosBC.Domain.Aggregates;

namespace CatalogoArticulosBC.Application.UseCases
{
    /// <summary>
    /// Agrega o elimina recursos multimedia de un ítem.
    /// </summary>
    public class GestionarMultimediaUseCase
    {
        private readonly ICatalogoArticulosRepository _repo;
        private readonly IUnitOfWork _uow;

        public GestionarMultimediaUseCase(ICatalogoArticulosRepository repo, IUnitOfWork uow)
        {
            _repo = repo;
            _uow  = uow;
        }

        public async Task<Guid> AgregarAsync(Guid productoServicioId, string tipoAdjunto, byte[] contenido)
        {
            var item = await _repo.GetByIdAsync(productoServicioId)
                       ?? throw new InvalidOperationException("Ítem no encontrado.");       // :contentReference[oaicite:5]{index=5}

            var multimediaId = Guid.NewGuid();
            item.AgregarMultimedia(multimediaId, tipoAdjunto, contenido);

            await _repo.UpdateAsync(item);
            await _uow.CommitAsync();
            return multimediaId;
        }

        public async Task EliminarAsync(Guid productoServicioId, Guid multimediaId)
        {
            var item = await _repo.GetByIdAsync(productoServicioId)
                       ?? throw new InvalidOperationException("Ítem no encontrado.");

            item.EliminarMultimedia(multimediaId);
            await _repo.UpdateAsync(item);
            await _uow.CommitAsync();
        }
    }
}
