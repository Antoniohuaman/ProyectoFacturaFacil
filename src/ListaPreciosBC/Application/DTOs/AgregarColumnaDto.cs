using System;
using ListaPreciosBC.Domain.ValueObjects;

namespace ListaPreciosBC.Application.DTOs
{
    /// <summary>
    /// DTO de entrada para agregar una columna a la lista de precios.
    /// </summary>
    public class AgregarColumnaDto
    {
    public Guid ListaPrecioId { get; set; }
    public byte NumeroColumna { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public ModoValorizacionColumna Modo { get; set; } = ModoValorizacionColumna.Fijo;
    public bool EsBase { get; set; }
    public bool Visible { get; set; }
    public byte Orden { get; set; }
    public string? Usuario { get; set; }
    public DateTimeOffset? Cuando { get; set; }
    }
}
