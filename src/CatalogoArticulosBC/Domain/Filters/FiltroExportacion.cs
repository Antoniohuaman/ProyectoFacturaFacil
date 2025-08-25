using CatalogoArticulosBC.Domain.ValueObjects;

namespace CatalogoArticulosBC.Domain.Filters
{
    public class FiltroExportacion
    {
        public Categoria? Categoria { get; set; }
        public bool? SoloHabilitados { get; set; }
        // Puedes agregar más propiedades según los criterios de exportación
    }
}
