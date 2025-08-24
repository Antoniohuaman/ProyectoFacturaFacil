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
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public string UnidadMedida { get; set; } = null!;
    public string AfectacionImpuesto { get; set; } = null!;
    public string Categoria { get; set; } = null!;
    public string Moneda { get; set; } = null!;
    public string? Marca { get; set; }
    public string? CodigoSunat { get; set; }
    public string? CentroCosto { get; set; }
    public decimal? Peso { get; set; }
    public string? CodigoBarras { get; set; }
    public string? CodigoFabrica { get; set; }
    public TipoProducto TipoProducto { get; set; }
    public TipoExistencia TipoExistencia { get; set; }
    public decimal? PrecioVenta { get; set; }
    public List<Guid> AlmacenesAsignados { get; set; } = new();
    public bool AsignarATodosLosAlmacenes { get; set; }
    public Guid? ImagenPrincipalId { get; set; }
    public string UsuarioId { get; set; } = null!;
    }
}