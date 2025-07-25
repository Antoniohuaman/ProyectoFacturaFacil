// src/CatalogoArticulosBC/Application/DTOs/CrearProductoSimpleDto.cs
using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.ValueObjects;

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
            decimal baseImponibleVentas,
            string centroCosto,
            decimal presupuesto,
            decimal peso,
            TipoProducto tipo,
            decimal precio)
        {
            Sku = sku;
            Nombre = nombre;
            Descripcion = descripcion;
            UnidadMedida = unidadMedida;
            AfectacionIgv = afectacionIgv;
            CodigoSunat = codigoSunat;
            BaseImponibleVentas = baseImponibleVentas;
            CentroCosto = centroCosto;
            Presupuesto = presupuesto;
            Peso = peso;
            Tipo = tipo;
            Precio = precio;
        }

        public string Sku { get; set; }
        public string Nombre { get; }
        public string Descripcion { get; }
        public string UnidadMedida { get; }
        public string AfectacionIgv { get; }
        public string CodigoSunat { get; }
        public decimal BaseImponibleVentas { get; }
        public string CentroCosto { get; }
        public decimal Presupuesto { get; }
        public decimal Peso { get; }
        public TipoProducto Tipo { get; }
        public decimal Precio { get; set; }
    }
}
