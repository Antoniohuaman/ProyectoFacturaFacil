using System;

namespace GestionInventarioBC.Application.DTOs.Transferencias
{
    public class ConfirmarRecepcionTransferenciaDto
    {
        public Guid TransferenciaId { get; set; }
    }

    public class ConfirmarRecepcionTransferenciaResultDto
    {
        public bool Ok { get; set; }
    }
}
