// src/CatalogoArticulosBC/Application/DTOs/CrearProductoSimpleDto.cs
namespace CatalogoArticulosBC.Application.DTOs
{
    public sealed class CrearProductoSimpleDto
    {
        public CrearProductoSimpleDto(
            string sku,
            string nombre,
            string descripcion,
            string unidadMedida,
            string afectacionIgv,
            string codigoSunat,
            string cuentaContable,
            string centroCosto,
            decimal presupuesto,
            decimal peso)
        {
            Sku              = sku;
            Nombre           = nombre;
            Descripcion      = descripcion;
            UnidadMedida     = unidadMedida;
            AfectacionIgv    = afectacionIgv;
            CodigoSunat      = codigoSunat;
            CuentaContable   = cuentaContable;
            CentroCosto      = centroCosto;
            Presupuesto      = presupuesto;
            Peso             = peso;
        }

        public string Sku { get; }
        public string Nombre { get; }
        public string Descripcion { get; }
        public string UnidadMedida { get; }
        public string AfectacionIgv { get; }
        public string CodigoSunat { get; }
        public string CuentaContable { get; }
        public string CentroCosto { get; }
        public decimal Presupuesto { get; }
        public decimal Peso { get; }
    }
}
