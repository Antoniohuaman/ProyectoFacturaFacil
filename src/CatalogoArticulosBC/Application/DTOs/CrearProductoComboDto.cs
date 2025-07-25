using System;
using System.Collections.Generic;
using System.Linq;

namespace CatalogoArticulosBC.Application.DTOs
{
    public class CrearProductoComboDto
    {
        public string SkuCombo { get; }
        public string NombreCombo { get; }
        public IReadOnlyCollection<ComponenteComboDto> Componentes { get; }
        public decimal PrecioCombo { get; }
        public string UnidadMedida { get; }
        public string AfectacionIGV { get; }
        public string Estado { get; }

        public CrearProductoComboDto(
            string skuCombo,
            string nombreCombo,
            IReadOnlyCollection<ComponenteComboDto> componentes,
            decimal precioCombo,
            string unidadMedida,
            string afectacionIGV,
            string estado)
        {
            if (string.IsNullOrWhiteSpace(skuCombo))
                throw new ArgumentException("El SKU del combo es obligatorio.", nameof(skuCombo));
            if (string.IsNullOrWhiteSpace(nombreCombo))
                throw new ArgumentException("El nombre del combo es obligatorio.", nameof(nombreCombo));
            if (componentes == null || componentes.Count == 0)
                throw new ArgumentException("Debe especificar al menos un componente.", nameof(componentes));
            if (precioCombo < 0)
                throw new ArgumentException("El precio del combo no puede ser negativo.", nameof(precioCombo));
            if (string.IsNullOrWhiteSpace(unidadMedida))
                throw new ArgumentException("La unidad de medida es obligatoria.", nameof(unidadMedida));
            if (string.IsNullOrWhiteSpace(afectacionIGV))
                throw new ArgumentException("La afectación IGV es obligatoria.", nameof(afectacionIGV));
            if (string.IsNullOrWhiteSpace(estado))
                throw new ArgumentException("El estado es obligatorio.", nameof(estado));
            // Validación opcional: no permitir componentes duplicados
            if (componentes.Select(c => c.ProductoServicioId).Distinct().Count() != componentes.Count)
                throw new ArgumentException("No se permiten componentes repetidos en el combo.", nameof(componentes));

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
            if (productoServicioId == Guid.Empty)
                throw new ArgumentException("El ID del producto/servicio es obligatorio.", nameof(productoServicioId));
            if (cantidad <= 0)
                throw new ArgumentException("La cantidad debe ser mayor a cero.", nameof(cantidad));

            ProductoServicioId = productoServicioId;
            Cantidad = cantidad;
        }
    }
}