using System;
using System.Threading.Tasks;
using ListaPreciosBC.Application.DTOs;
using ListaPreciosBC.Application.Interfaces;
using ListaPreciosBC.Domain.Entities;
using ListaPreciosBC.Domain.Events;
using ListaPreciosBC.Domain.ValueObjects;
using ListaPreciosBC.Domain.Aggregates;

namespace ListaPreciosBC.Application.UseCases
{
    public class ModificarPrecioEspecificoUseCase
    {
        private readonly IListaPrecioRepository _repo;
        private readonly IEventBus _eventBus;

        public ModificarPrecioEspecificoUseCase(
            IListaPrecioRepository repo,
            IEventBus eventBus)
        {
            _repo = repo;
            _eventBus = eventBus;
        }

        public async Task EjecutarAsync(ModificarPrecioEspecificoDto dto)
        {
            // Buscar precio específico
            var precio = await _repo.ObtenerPrecioEspecificoPorIdAsync(dto.PrecioEspecificoId);
            if (precio == null)
                throw new EntityNotFoundException("PrecioEspecifico no encontrado.");

            // Buscar lista y validar vigente
            var lista = await _repo.GetByIdAsync(precio.ListaPrecioId);
            if (lista == null || !lista.Activa || lista.Vigencia.FechaFin < DateTime.Today)
                throw new InvalidOperationException("La lista no está vigente.");

            // Convertir string a Moneda?
            Moneda? nuevaMoneda = null;
            if (!string.IsNullOrEmpty(dto.NuevaMoneda))
            {
                if (Enum.TryParse<Moneda>(dto.NuevaMoneda, out var monedaParsed))
                    nuevaMoneda = monedaParsed;
                else
                    throw new ArgumentException("Moneda inválida");
            }

            // Modificar y obtener campos modificados
            var camposModificados = precio.Modificar(
                dto.NuevoValor,
                nuevaMoneda,
                dto.NuevaFechaInicio,
                dto.NuevaFechaFin
            );

            if (camposModificados == null || camposModificados.Count == 0)
                throw new InvalidOperationException("No hay cambios para aplicar.");

            // Registrar historial
            var historial = new HistorialPrecio(
                Guid.NewGuid(),
                precio.PrecioEspecificoId,
                dto.UsuarioId,
                DateTime.UtcNow,
                camposModificados,
                dto.Motivo
            );
            await _repo.AgregarHistorialAsync(historial);

            // Actualizar precio
            await _repo.ActualizarPrecioEspecificoAsync(precio);

            // Publicar evento
            var evento = new PrecioEspecificoModificado(
                precio.PrecioEspecificoId,
                camposModificados,
                dto.UsuarioId,
                DateTime.UtcNow,
                dto.Motivo
            );
            await _eventBus.PublishAsync(evento);
        }
    }
}