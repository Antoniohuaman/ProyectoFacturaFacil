using System;
using SharedKernel.Events;
using ListaPreciosBC.Domain.ValueObjects;

namespace ListaPreciosBC.Domain.Events
{
    public sealed class PlantillaDeColumnasActualizada : DomainEvent
    {
        public Guid ListaPrecioId { get; }
        public PlantillaColumnasPrecio NuevaPlantilla { get; }
        public int Version { get; }
        public string? Usuario { get; }
        public DateTimeOffset OcurrioEn { get; }

        public PlantillaDeColumnasActualizada(
            Guid ListaPrecioId,
            PlantillaColumnasPrecio NuevaPlantilla,
            int Version,
            string? Usuario,
            DateTimeOffset OcurrioEn)
            : base(occurredOnUtc: OcurrioEn.UtcDateTime)
        {
            this.ListaPrecioId   = ListaPrecioId;
            this.NuevaPlantilla  = NuevaPlantilla;
            this.Version         = Version;
            this.Usuario         = Usuario;
            this.OcurrioEn       = OcurrioEn;
        }
    }
}
