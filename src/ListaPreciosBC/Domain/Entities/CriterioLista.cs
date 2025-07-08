using System;
using ListaPreciosBC.Domain.ValueObjects;
namespace ListaPreciosBC.Domain.Entities
{
    public class CriterioLista
    {
        public Guid? ClienteId { get; private set; }
        public string? CanalVenta { get; private set; }
        public RangoVolumen? RangoVolumen { get; private set; }
        public PeriodoVigencia? PeriodoVigencia { get; private set; }

        public CriterioLista(Guid? clienteId, string? canalVenta, RangoVolumen? rangoVolumen, PeriodoVigencia? periodoVigencia)
        {
            ClienteId = clienteId;
            CanalVenta = canalVenta;
            RangoVolumen = rangoVolumen;
            PeriodoVigencia = periodoVigencia;
        }

        // Método cumpleCriterio(...) se puede agregar aquí
    }
}