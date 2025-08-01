using System;
using System.Collections.Generic;
using CatalogoArticulosBC.Domain.ValueObjects;
using CatalogoArticulosBC.Domain.Entities;
using CatalogoArticulosBC.Domain.Aggregates;



namespace CatalogoArticulosBC.Application.DTOs
{
    public class EditarProductoSimpleDto
    {
        public Guid ProductoId { get; set; }
        public string NuevoNombre { get; set; } = default!;
        public string NuevaDescripcion { get; set; } = default!;
        public string NuevaUnidadMedida { get; set; } = default!;
        public string NuevaAfectacionIGV { get; set; } = default!;
        public string NuevaCategoria { get; set; } = default!;         // Obligatorio
        public string? NuevaMarca { get; set; }                        // Opcional
        public string? NuevoCodigoSunat { get; set; }
        public decimal? NuevaBaseImponibleVentas { get; set; }
        public string? NuevoCentroCosto { get; set; }
        public decimal? NuevoPresupuesto { get; set; }
        public decimal? NuevoPeso { get; set; }
        public TipoProducto NuevoTipoProducto { get; set; }
        public decimal NuevoPrecio { get; set; }
        public Guid? NuevaImagenPrincipalId { get; set; }
        public decimal NuevoPrecioVentaSinIGV { get; set; }
        public decimal NuevoPrecioVentaConIGV { get; set; }
        public bool NuevoEsAfectoICBPER { get; set; }
        public bool NuevoTieneDetraccion { get; set; }
        public string? NuevoCodigoDetraccion { get; set; }
        public string? NuevoCodigoBarras { get; set; }
        public string? NuevoCodigoFabrica { get; set; }
        public string? NuevoCodigoLote { get; set; }
        public string? NuevaSerie { get; set; }
        public List<Guid>? NuevosAlmacenesAsignados { get; set; }
        public bool NuevoAsignarATodosLosAlmacenes { get; set; }
        public Moneda NuevaMoneda { get; set; }
        public DateTime? NuevaFechaVencimiento { get; set; }
        public TipoExistencia NuevoTipoExistencia { get; set; }
        public string UsuarioId { get; set; } = default!;
    }
}