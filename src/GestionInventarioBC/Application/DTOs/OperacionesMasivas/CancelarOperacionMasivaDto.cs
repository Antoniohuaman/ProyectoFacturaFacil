using System;

namespace GestionInventarioBC.Application.DTOs.OperacionesMasivas
{
    public class CancelarOperacionMasivaDto
    {
        public Guid OperacionId { get; set; }
    }

    public class CancelarOperacionMasivaResultDto
    {
        public Guid OperacionId { get; set; }
        public bool Cancelada { get; set; }
    }
}
