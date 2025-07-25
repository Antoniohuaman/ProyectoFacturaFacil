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

        // Nuevo método avanzado para agregar multimedia
        public async Task<Guid> AgregarAsync(
            Guid productoServicioId,
            string tipoAdjunto,
            string nombreArchivo,
            string ruta,
            string comentario,
            long tamano)
        {
            var item = await _repo.GetByIdAsync(productoServicioId)
                       ?? throw new InvalidOperationException("Ítem no encontrado.");

            var multimediaId = Guid.NewGuid();
            item.AgregarMultimediaAvanzada(multimediaId, tipoAdjunto, nombreArchivo, ruta, comentario, tamano);

            await _repo.UpdateAsync(item);
            await _uow.CommitAsync();
            return multimediaId;
        }

        public async Task EliminarAsync(Guid productoServicioId, Guid multimediaId)
        {
            var item = await _repo.GetByIdAsync(productoServicioId)
                       ?? throw new InvalidOperationException("Ítem no encontrado.");

            item.EliminarMultimediaAvanzada(multimediaId);
            await _repo.UpdateAsync(item);
            await _uow.CommitAsync();
        }
    }
}