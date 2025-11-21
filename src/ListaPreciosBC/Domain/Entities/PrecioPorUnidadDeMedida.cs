using System;
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

    public ValorPrecio? PrecioFijo { get; private set; }
    public PeriodoVigencia? Vigencia { get; private set; }
    public MatrizVolumen? MatrizVolumen { get; private set; }

    public bool TienePrecioFijo => PrecioFijo is not null && Vigencia is not null;
    public bool TieneMatrizVolumen => MatrizVolumen is not null;
    public bool EstaVacia => !TienePrecioFijo && !TieneMatrizVolumen;

    public PrecioPorUnidadDeMedida(
        IdentificadorColumnaPrecio columnaId,
        UnidadDeMedida unidadDeMedida,
        bool estaHabilitada = true)
    {
        // Las invariantes propias de los VOs (rangos, formatos, etc.)
        // se validan en sus propias factories; aquí asumimos que llegan válidos.
        ColumnaId = columnaId ?? throw new ArgumentNullException(nameof(columnaId));
        UnidadDeMedida = unidadDeMedida ?? throw new ArgumentNullException(nameof(unidadDeMedida));
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

    public void EstablecerPrecioFijo(ValorPrecio valor, PeriodoVigencia vigencia)
    {
        PrecioFijo = valor ?? throw new ArgumentNullException(nameof(valor));
        Vigencia = vigencia ?? throw new ArgumentNullException(nameof(vigencia));
        MatrizVolumen = null;
    }

    public void EliminarPrecioFijo()
    {
        PrecioFijo = null;
        Vigencia = null;
    }

    public void EstablecerMatrizVolumen(MatrizVolumen matriz)
    {
        MatrizVolumen = matriz ?? throw new ArgumentNullException(nameof(matriz));
        PrecioFijo = null;
        Vigencia = null;
    }

    public void EliminarMatrizVolumen()
    {
        MatrizVolumen = null;
    }

    public override string ToString()
    {
        return $"{ColumnaId} - {UnidadDeMedida.Codigo} (Habilitada: {EstaHabilitada})";
    }
}
