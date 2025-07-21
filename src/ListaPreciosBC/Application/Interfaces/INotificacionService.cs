using System.Collections.Generic;
using System.Threading.Tasks;
using ListaPreciosBC.Application.DTOs;

namespace ListaPreciosBC.Application.Interfaces
{
    public interface INotificacionService
    {
        Task EnviarCorreoAsync(List<string> destinatarios, MensajeNotificacionDto mensaje);
    }
}