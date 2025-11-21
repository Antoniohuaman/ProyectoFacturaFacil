using ListaPreciosBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace ListaPreciosBC.Domain.Entities;

/// <summary>
/// Representa los precios asociados a una combinación
/// (columna de precio, unidad de medida) dentro del agregado PrecioProducto.
/// 
/// La exclusividad entre precio fijo y matriz por volumen la controla
/// el agregado, esta entidad solo modela la pareja Columna–Unidad.
/// </summary>
public sealed class PrecioPorUnidadDeMedida
{
    public IdentificadorColumnaPrecio ColumnaId { get; }
    public UnidadDeMedida UnidadDeMedida { get; }

    /// <summary>
    /// Indica si esta combinación (columna, unidad) está habilitada
    /// para registrar precios en el agregado.
    /// </summary>
    public bool EstaHabilitada { get; private set; }

    public PrecioPorUnidadDeMedida(
        IdentificadorColumnaPrecio columnaId,
        UnidadDeMedida unidadDeMedida,
        bool estaHabilitada = true)
    {
        // Las invariantes propias de los VOs (rangos, formatos, etc.)
        // se validan en sus propias factories; aquí asumimos que llegan válidos.
        ColumnaId = columnaId;
        UnidadDeMedida = unidadDeMedida;
        EstaHabilitada = estaHabilitada;
    }

    public void Habilitar()
    {
        EstaHabilitada = true;
    }

    public void Deshabilitar()
    {
        EstaHabilitada = false;
    }

    public override string ToString()
    {
        return $"{ColumnaId} - {UnidadDeMedida.Codigo} (Habilitada: {EstaHabilitada})";
    }
}
