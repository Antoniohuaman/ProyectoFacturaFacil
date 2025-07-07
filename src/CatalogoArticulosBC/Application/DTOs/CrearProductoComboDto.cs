// src/CatalogoArticulosBC/Application/DTOs/CrearProductoComboDto.cs
namespace CatalogoArticulosBC.Application.DTOs
{
    public sealed class CrearProductoComboDto
    {
        public CrearProductoComboDto(
            string skuCombo,
            string nombreCombo,
            IReadOnlyCollection<(Guid productoId,int cantidad)> componentes,
            decimal precioCombo,
            string unidadMedida,
            string afectacionIgv)
        {
            SkuCombo       = skuCombo;
            NombreCombo    = nombreCombo;
            Componentes    = componentes;
            PrecioCombo    = precioCombo;
            UnidadMedida   = unidadMedida;
            AfectacionIgv  = afectacionIgv;
        }

        public string SkuCombo { get; }
        public string NombreCombo { get; }
        public IReadOnlyCollection<(Guid productoId,int cantidad)> Componentes { get; }
        public decimal PrecioCombo { get; }
        public string UnidadMedida { get; }
        public string AfectacionIgv { get; }
    }
}
