namespace CatalogoArticulosBC.Domain.Specifications
{
    /// <summary>
    /// Servicio puente a persistencia para verificar unicidad de SKU.
    /// </summary>
    public interface IValidadorUnicidadSku
    {
        bool EsUnico(string sku);
    }
}