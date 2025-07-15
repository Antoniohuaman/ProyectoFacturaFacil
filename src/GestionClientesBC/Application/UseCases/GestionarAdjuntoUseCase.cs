using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GestionClientesBC.Application.Interfaces;
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.Entities;
using GestionClientesBC.Domain.Events;
using GestionClientesBC.Domain.Exceptions;

namespace GestionClientesBC.Application.UseCases
{
    public class GestionarAdjuntoUseCase
    {
        private readonly IGestionClientesRepository _repo;
        private readonly IUnitOfWork _uow;
        private readonly IAlmacenamientoArchivos _almacenamiento;

        public GestionarAdjuntoUseCase(
            IGestionClientesRepository repo,
            IUnitOfWork uow,
            IAlmacenamientoArchivos almacenamiento)
        {
            _repo = repo;
            _uow = uow;
            _almacenamiento = almacenamiento;
        }

        public async Task<Guid> AgregarAdjuntoAsync(
            Guid clienteId,
            string nombreArchivo,
            byte[] archivo,
            string? comentario,
            IUserContext userContext)
        {
            var cliente = await _repo.GetByIdAsync(clienteId);
            if (cliente == null)
                throw new InvalidOperationException("Cliente no encontrado.");

            ValidarArchivo(nombreArchivo, archivo);

            string rutaArchivo;
            try
            {
                rutaArchivo = await _almacenamiento.GuardarArchivoAsync(nombreArchivo, archivo);
            }
            catch (Exception ex)
            {
                throw new Exception("No se pudo guardar el archivo: " + ex.Message);
            }

            var adjunto = new AdjuntoCliente(
                Guid.NewGuid(),
                nombreArchivo,
                rutaArchivo,
                DateTime.UtcNow,
                comentario
            );
            cliente.AgregarAdjunto(adjunto);

            cliente.RegistrarEvento(new AdjuntoAgregado(clienteId, adjunto));

            await _repo.UpdateAsync(cliente);
            await _uow.CommitAsync();

            return adjunto.AdjuntoId;
        }

        public async Task EliminarAdjuntoAsync(Guid clienteId, Guid adjuntoId, IUserContext userContext)
        {
            var cliente = await _repo.GetByIdAsync(clienteId);
            if (cliente == null)
                throw new InvalidOperationException("Cliente no encontrado.");

            var adjunto = cliente.Adjuntos.FirstOrDefault(a => a.AdjuntoId == adjuntoId && a.Activo);
            if (adjunto == null)
                throw new Exception("Adjunto no encontrado.");

            cliente.EliminarAdjunto(adjuntoId);

            cliente.RegistrarEvento(new ClienteModificado(clienteId, DateTime.UtcNow));

            await _repo.UpdateAsync(cliente);
            await _uow.CommitAsync();
        }

        private void ValidarArchivo(string nombreArchivo, byte[] archivo)
        {
            var extension = Path.GetExtension(nombreArchivo).ToLowerInvariant();
            var permitidas = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" };
            if (!permitidas.Contains(extension))
                throw new AdjuntoInvalidoException("Tipo de archivo no permitido. Solo PDF/JPG/PNG/DOC/DOCX.");

            if (archivo == null || archivo.Length == 0)
                throw new AdjuntoInvalidoException("Archivo vacío.");

            if (archivo.Length > 10 * 1024 * 1024)
                throw new AdjuntoInvalidoException("El archivo excede el tamaño máximo permitido (10MB).");
        }
    }
}