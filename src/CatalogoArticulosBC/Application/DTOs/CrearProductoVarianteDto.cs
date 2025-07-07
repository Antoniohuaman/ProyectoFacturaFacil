// src/CatalogoArticulosBC/Application/DTOs/CrearProductoVarianteDto.cs
namespace CatalogoArticulosBC.Application.DTOs
{
    public sealed class CrearProductoVarianteDto
    {
        public CrearProductoVarianteDto(
            string skuVariante,
            Guid   productoPadreId,
            IReadOnlyCollection<KeyValuePair<string,string>> atributos)
        {
            SkuVariante    = skuVariante;
            ProductoPadreId = productoPadreId;
            Atributos      = atributos;
        }

        public string SkuVariante { get; }
        public Guid ProductoPadreId { get; }
        public IReadOnlyCollection<KeyValuePair<string,string>> Atributos { get; }
    }
}
