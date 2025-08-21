using System;
using ListaPreciosBC.Domain.ValueObjects;

namespace ListaPreciosBC.Domain.Events
{
    // Marca de evento de dominio simple. Si ya tienes una interfaz en tu SharedKernel, úsala.
    public interface IDomainEvent { }

    /// <summary>
    /// Se emite cada vez que la plantilla de columnas cambia en el agregado ListaPrecio.
    /// Incluye versión, usuario y fecha/hora para auditoría/proyecciones.
    /// </summary>
    public sealed record PlantillaDeColumnasActualizada(
        Guid ListaPrecioId,
        PlantillaColumnasPrecio NuevaPlantilla,
        int Version,
        string? Usuario,
        DateTimeOffset OcurrioEn
    ) : IDomainEvent;
}