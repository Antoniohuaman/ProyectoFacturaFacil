using System;

namespace GestionInventarioBC.Application.DTOs.Transferencias
{
    public class CancelarTransferenciaPendienteDto
    {
        public Guid TransferenciaId { get; set; }
    }

    public class CancelarTransferenciaPendienteResultDto
    {
        public bool Ok { get; set; }
    }
}
