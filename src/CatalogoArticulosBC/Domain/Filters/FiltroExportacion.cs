using CatalogoArticulosBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace CatalogoArticulosBC.Domain.Filters
{
    public class FiltroExportacion
    {
        public CategoriaId? CategoriaId { get; set; }
        public bool? SoloHabilitados { get; set; }
        // Puedes agregar más propiedades según los criterios de exportación
    }
}
