using System;
using System.Collections.Generic;

namespace CatalogoArticulosBC.Application.UseCases
{
    /// <summary>
    /// DTO para crear un ProductoSimple.
    /// Contiene todos los campos necesarios para instanciar el agregado.
    /// </summary>
    public class CrearProductoSimpleDto
    {
        public string Sku { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string? Descripcion { get; set; }
        public string UnidadMedida { get; set; } = null!;
        public string AfectacionIgvCodigo { get; set; } = null!;
        public string Categoria { get; set; } = null!;
        public string? Marca { get; set; }
        public decimal? PrecioVenta { get; set; }
    // ISC, Detracción eliminados
        public string? CodigoSunat { get; set; }
    // BaseImponibleVentas eliminado
        public string? CentroCosto { get; set; }
        public decimal? Peso { get; set; }
    // Serie eliminado
        public string? CodigoBarras { get; set; }
        public string? CodigoFabrica { get; set; }
    // CodigoLote eliminado
        public string TipoProducto { get; set; } = "Bien";
        public string TipoExistencia { get; set; } = "ProductosTerminados";
    // FechaVencimiento eliminada
        public List<Guid> AlmacenesAsignados { get; set; } = new List<Guid>();
        public bool AsignarATodosLosAlmacenes { get; set; }
        public Guid? ImagenPrincipalId { get; set; }
    }
}
