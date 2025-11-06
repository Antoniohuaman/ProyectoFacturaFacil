using System;
using SharedKernel.Events;
using SharedKernel.ValueObjects;
using ListaPreciosBC.Domain.ValueObjects;

namespace ListaPreciosBC.Domain.Events
{
    public sealed class PlantillaDeColumnasActualizada : DomainEvent
    {
        public EmpresaId EmpresaId { get; }
        public Guid ListaPrecioId { get; }
        public PlantillaColumnasPrecio NuevaPlantilla { get; }
        public int Version { get; }
        public string? Usuario { get; }
        public DateTimeOffset OcurrioEn { get; }

        public PlantillaDeColumnasActualizada(
            EmpresaId EmpresaId,
            Guid ListaPrecioId,
            PlantillaColumnasPrecio NuevaPlantilla,
            int Version,
            string? Usuario,
            DateTimeOffset OcurrioEn)
            : base(occurredOnUtc: OcurrioEn.UtcDateTime)
        {
            this.EmpresaId      = EmpresaId;
            this.ListaPrecioId   = ListaPrecioId;
            this.NuevaPlantilla  = NuevaPlantilla;
            this.Version         = Version;
            this.Usuario         = Usuario;
            this.OcurrioEn       = OcurrioEn;
        }
    }
}
