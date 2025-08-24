namespace CatalogoArticulosBC.Domain.Repositories
{
    public class FiltroExportacion
    {
        public Guid? CategoriaId { get; set; }
        public bool? SoloHabilitados { get; set; }
        // Puedes agregar más propiedades según los criterios de exportación
    }
}
