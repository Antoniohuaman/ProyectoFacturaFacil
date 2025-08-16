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
        public bool AplicaIsc { get; set; }
        public string? TipoAplicacionIsc { get; set; }
        public decimal? ValorPorcentajeIsc { get; set; }
        public decimal? MontoFijoIsc { get; set; }
        public bool TieneDetraccion { get; set; }
        public string? CodigoDetraccion { get; set; }
        public string? CodigoSunat { get; set; }
        public decimal? BaseImponibleVentas { get; set; }
        public string? CentroCosto { get; set; }
        public decimal? Peso { get; set; }
        public string? Serie { get; set; }
        public string? CodigoBarras { get; set; }
        public string? CodigoFabrica { get; set; }
        public string? CodigoLote { get; set; }
        public string TipoProducto { get; set; } = "Bien";
        public string TipoExistencia { get; set; } = "ProductosTerminados";
        public DateTime? FechaVencimiento { get; set; }
        public List<Guid> AlmacenesAsignados { get; set; } = new List<Guid>();
        public bool AsignarATodosLosAlmacenes { get; set; }
        public Guid? ImagenPrincipalId { get; set; }
    }
}
