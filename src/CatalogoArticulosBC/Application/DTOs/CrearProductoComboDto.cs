using System;
using System.Collections.Generic;

namespace CatalogoArticulosBC.Application.DTOs
{
    public class CrearProductoComboDto
    {
        public string SkuCombo { get; }
        public string NombreCombo { get; }
        public List<ComponenteComboDto> Componentes { get; }
        public decimal PrecioCombo { get; }
        public string UnidadMedida { get; }
        public string AfectacionIGV { get; }
        public string Estado { get; }

        public CrearProductoComboDto(
            string skuCombo,
            string nombreCombo,
            List<ComponenteComboDto> componentes,
            decimal precioCombo,
            string unidadMedida,
            string afectacionIGV,
            string estado)
        {
            SkuCombo = skuCombo;
            NombreCombo = nombreCombo;
            Componentes = componentes;
            PrecioCombo = precioCombo;
            UnidadMedida = unidadMedida;
            AfectacionIGV = afectacionIGV;
            Estado = estado;
        }
    }

    public class ComponenteComboDto
    {
        public Guid ProductoServicioId { get; }
        public int Cantidad { get; }

        public ComponenteComboDto(Guid productoServicioId, int cantidad)
        {
            ProductoServicioId = productoServicioId;
            Cantidad = cantidad;
        }
    }
}