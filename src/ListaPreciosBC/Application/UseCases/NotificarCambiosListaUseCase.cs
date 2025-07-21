using System;
using System.Threading.Tasks;
using ListaPreciosBC.Application.Interfaces;
using ListaPreciosBC.Application.DTOs;
using ListaPreciosBC.Domain.Events;

namespace ListaPreciosBC.Application.UseCases
{
    /// <summary>
    /// Caso de uso: Notificar cambios importantes en listas de precios al equipo comercial.
    /// Reacciona a eventos de ListaPrecioCreada o ListaPrecioInhabilitada.
    /// </summary>
    public class NotificarCambiosListaUseCase
    {
        private readonly INotificacionService _notificacionService;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IReintentosService _reintentosService;

        public NotificarCambiosListaUseCase(
            INotificacionService notificacionService,
            IUsuarioRepository usuarioRepository,
            IReintentosService reintentosService)
        {
            _notificacionService = notificacionService;
            _usuarioRepository = usuarioRepository;
            _reintentosService = reintentosService;
        }

        /// <summary>
        /// Ejecuta la notificación al recibir un evento relevante.
        /// </summary>
        public async Task HandleAsync(ListaPrecioEventoDto evento)
        {
            // Recupera usuarios con permiso VER_PRECIOS
            var usuarios = await _usuarioRepository.GetUsuariosConPermisoAsync("VER_PRECIOS");
            if (usuarios.Count == 0) return;

            // Construye el mensaje
            var mensaje = new MensajeNotificacionDto
            {
                ListaNombre = evento.ListaNombre,
                TipoEvento = evento.TipoEvento,
                Usuario = evento.Usuario,
                Fecha = evento.Fecha
            };

            // Intenta enviar la notificación
            try
            {
                await _notificacionService.EnviarCorreoAsync(usuarios, mensaje);
            }
            catch (Exception ex)
            {
                // Registra en cola de reintentos
                await _reintentosService.RegistrarReintentoAsync(evento, ex.Message);
            }
        }
    }
}